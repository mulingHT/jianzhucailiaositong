using ISO11820.Models;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace ISO11820.Services;

/// <summary>
/// PDF 导出服务 — 使用 PDFsharp + OxyPlot
/// 生成试验概要 + 温度曲线图片 + 判定结论
/// </summary>
public class PdfExportService
{
    private static bool _fontResolverSet = false;
    private static readonly object _lock = new();

    // 使用 SimHei（黑体）— Windows 自带独立 TTF，支持中文，PDFsharp 可直接嵌入
    private const string FONT = "SimHei";

    public string Export(TestMaster test, List<double[]> recordedData, string reportDir)
    {
        // 全局字体解析器（仅设置一次）
        if (!_fontResolverSet)
        {
            lock (_lock)
            {
                if (!_fontResolverSet)
                {
                    GlobalFontSettings.FontResolver = new SystemFontResolver();
                    _fontResolverSet = true;
                }
            }
        }

        Directory.CreateDirectory(reportDir);
        var filePath = Path.Combine(reportDir, $"{test.TestId}_报告.pdf");

        using var document = new PdfDocument();
        document.Info.Title = $"ISO 11820 试验报告 - {test.TestId}";

        var page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;

        var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont(FONT, 16, XFontStyleEx.Bold);
        var fontNormal = new XFont(FONT, 10, XFontStyleEx.Regular);
        var fontBold = new XFont(FONT, 10, XFontStyleEx.Bold);

        double y = 40;
        double leftX = 50;
        double valueX = 200;

        // 标题
        gfx.DrawString("ISO 11820 建筑材料不燃性试验报告", fontTitle, XBrushes.Black,
            new XRect(XUnit.FromPoint(0), XUnit.FromPoint(y), page.Width, XUnit.FromPoint(30)),
            XStringFormats.TopCenter);
        y += 45;

        // 试验概要
        void DrawRow(string label, string value)
        {
            gfx.DrawString($"{label}:", fontNormal, XBrushes.Black,
                XUnit.FromPoint(leftX), XUnit.FromPoint(y));
            gfx.DrawString(value, fontNormal, XBrushes.Black,
                XUnit.FromPoint(valueX), XUnit.FromPoint(y));
            y += 20;
        }

        DrawRow("试验ID", test.TestId);
        DrawRow("样品编号", test.ProductId);
        DrawRow("试验日期", test.TestDate);
        DrawRow("操作员", test.Operator);
        DrawRow("环境温度", $"{test.AmbTemp:F1} °C");
        DrawRow("环境湿度", $"{test.AmbHumi:F1} %");
        DrawRow("试验前质量", $"{test.PreWeight:F1} g");
        DrawRow("试验后质量", $"{test.PostWeight:F1} g");
        DrawRow("失重量", $"{test.LostWeight:F1} g");
        DrawRow("失重率", $"{test.LostWeightPer:F1} %");
        DrawRow("总试验时长", $"{test.TotalTestTime} 秒");
        y += 8;

        // 温升数据
        gfx.DrawString("温度数据", fontBold, XBrushes.Black,
            XUnit.FromPoint(leftX), XUnit.FromPoint(y));
        y += 22;
        DrawRow("炉温1温升", $"{test.DeltaTf1:F1} °C");
        DrawRow("炉温2温升", $"{test.DeltaTf2:F1} °C");
        DrawRow("表面温升", $"{test.DeltaTs:F1} °C");
        DrawRow("中心温升", $"{test.DeltaTc:F1} °C");
        DrawRow("综合温升(deltaTf)", $"{test.DeltaTf:F1} °C");
        y += 15;

        // 温度曲线图
        if (recordedData.Count > 0)
        {
            var chartBmp = GenerateChartImage(recordedData);
            if (chartBmp != null)
            {
                // 换新页
                gfx.Dispose();
                page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                gfx = XGraphics.FromPdfPage(page);
                y = 40;

                gfx.DrawString("温度曲线图", fontBold, XBrushes.Black,
                    XUnit.FromPoint(leftX), XUnit.FromPoint(y));
                y += 25;

                using var ms = new MemoryStream();
                chartBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                using var xImg = XImage.FromStream(ms);

                double imgW = page.Width.Point - 100;
                double imgH = 280;
                gfx.DrawImage(xImg,
                    XUnit.FromPoint(leftX), XUnit.FromPoint(y),
                    XUnit.FromPoint(imgW), XUnit.FromPoint(imgH));
                y += imgH + 20;

                chartBmp.Dispose();
            }
        }

        // 判定结论
        gfx.DrawString("判定结论", fontBold, XBrushes.Black,
            XUnit.FromPoint(leftX), XUnit.FromPoint(y));
        y += 22;

        bool passed = test.DeltaTf <= 50 && test.LostWeightPer <= 50 && test.FlameDuration < 5;
        string conclusion = passed ? "通过 - 符合不燃性要求" : "不通过";
        var brush = passed ? XBrushes.Green : XBrushes.Red;
        gfx.DrawString(conclusion, fontTitle, brush,
            new XRect(XUnit.FromPoint(leftX), XUnit.FromPoint(y),
                      XUnit.FromPoint(page.Width.Point - 100), XUnit.FromPoint(28)),
            XStringFormats.TopLeft);
        y += 32;

        if (test.FlameDuration > 0)
            gfx.DrawString($"火焰持续时间: {test.FlameDuration} 秒", fontNormal, XBrushes.Black,
                XUnit.FromPoint(leftX), XUnit.FromPoint(y));

        document.Save(filePath);
        gfx.Dispose();
        return filePath;
    }

    private System.Drawing.Bitmap? GenerateChartImage(List<double[]> data)
    {
        try
        {
            var model = new PlotModel { Title = "温度曲线" };
            model.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                Title = "温度 (°C)",
                Minimum = 0,
                Maximum = 800
            });
            model.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                Title = "时间 (秒)"
            });

            var colors = new[] { OxyColors.Red, OxyColors.Blue, OxyColors.Green, OxyColors.Orange };
            var names = new[] { "炉温1", "炉温2", "表面温", "中心温" };

            for (int ch = 0; ch < 4; ch++)
            {
                var series = new LineSeries { Title = names[ch], Color = colors[ch], StrokeThickness = 1 };
                for (int i = 0; i < data.Count; i++)
                    series.Points.Add(new DataPoint(i, data[i][ch]));
                model.Series.Add(series);
            }

            var pngExporter = new PngExporter { Width = 800, Height = 400 };
            using var ms = new MemoryStream();
            pngExporter.Export(model, ms);
            ms.Position = 0;
            return new System.Drawing.Bitmap(ms);
        }
        catch
        {
            return null;
        }
    }
}
