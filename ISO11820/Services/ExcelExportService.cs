using ISO11820.Models;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;

namespace ISO11820.Services;

/// <summary>
/// Excel 导出服务 — 使用 EPPlus
/// 生成 3 个 Sheet：试验信息、温度数据、温度曲线图
/// </summary>
public class ExcelExportService
{
    public string Export(TestMaster test, List<double[]> recordedData, string reportDir)
    {
        Directory.CreateDirectory(reportDir);
        var filePath = Path.Combine(reportDir, $"{test.TestId}_报告.xlsx");

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        // Sheet1: 试验信息
        var sheet1 = package.Workbook.Worksheets.Add("试验信息");
        WriteTestInfo(sheet1, test);

        // Sheet2: 温度数据
        var sheet2 = package.Workbook.Worksheets.Add("温度数据");
        WriteTemperatureData(sheet2, recordedData);

        // Sheet3: 温度曲线图
        var sheet3 = package.Workbook.Worksheets.Add("温度曲线图");
        WriteTemperatureChart(sheet3, recordedData);

        package.SaveAs(new FileInfo(filePath));
        return filePath;
    }

    private void WriteTestInfo(ExcelWorksheet sheet, TestMaster test)
    {
        sheet.Cells["A1"].Value = "ISO 11820 建筑材料不燃性试验报告";
        sheet.Cells["A1"].Style.Font.Size = 16;
        sheet.Cells["A1"].Style.Font.Bold = true;

        int row = 3;
        var info = new (string Label, object Value)[]
        {
            ("试验ID", test.TestId),
            ("样品编号", test.ProductId),
            ("试验日期", test.TestDate),
            ("操作员", test.Operator),
            ("环境温度 (°C)", test.AmbTemp),
            ("环境湿度 (%)", test.AmbHumi),
            ("试验前质量 (g)", test.PreWeight),
            ("试验后质量 (g)", test.PostWeight),
            ("失重量 (g)", test.LostWeight),
            ("失重率 (%)", test.LostWeightPer),
            ("总试验时长 (秒)", test.TotalTestTime),
            ("炉温1温升 (°C)", test.DeltaTf1),
            ("炉温2温升 (°C)", test.DeltaTf2),
            ("表面温升 (°C)", test.DeltaTs),
            ("中心温升 (°C)", test.DeltaTc),
            ("综合温升 (°C)", test.DeltaTf),
        };

        foreach (var (label, value) in info)
        {
            sheet.Cells[$"A{row}"].Value = label;
            sheet.Cells[$"B{row}"].Value = value;
            row++;
        }
        sheet.Column(1).AutoFit();
        sheet.Column(2).AutoFit();
    }

    private void WriteTemperatureData(ExcelWorksheet sheet, List<double[]> data)
    {
        sheet.Cells["A1"].Value = "时间(秒)";
        sheet.Cells["B1"].Value = "炉温1(°C)";
        sheet.Cells["C1"].Value = "炉温2(°C)";
        sheet.Cells["D1"].Value = "表面温(°C)";
        sheet.Cells["E1"].Value = "中心温(°C)";
        sheet.Cells["F1"].Value = "校准温(°C)";

        for (int i = 0; i < data.Count; i++)
        {
            sheet.Cells[$"A{i + 2}"].Value = i;
            sheet.Cells[$"B{i + 2}"].Value = Math.Round(data[i][0], 1);
            sheet.Cells[$"C{i + 2}"].Value = Math.Round(data[i][1], 1);
            sheet.Cells[$"D{i + 2}"].Value = Math.Round(data[i][2], 1);
            sheet.Cells[$"E{i + 2}"].Value = Math.Round(data[i][3], 1);
            sheet.Cells[$"F{i + 2}"].Value = Math.Round(data[i][4], 1);
        }
        sheet.Column(1).AutoFit();
        sheet.Column(2).AutoFit();
        sheet.Column(3).AutoFit();
        sheet.Column(4).AutoFit();
        sheet.Column(5).AutoFit();
        sheet.Column(6).AutoFit();
    }

    private void WriteTemperatureChart(ExcelWorksheet sheet, List<double[]> data)
    {
        // 写入图表数据
        for (int i = 0; i < data.Count; i++)
        {
            sheet.Cells[$"A{i + 1}"].Value = i;              // X轴：时间
            sheet.Cells[$"B{i + 1}"].Value = Math.Round(data[i][0], 1); // TF1
            sheet.Cells[$"C{i + 1}"].Value = Math.Round(data[i][1], 1); // TF2
            sheet.Cells[$"D{i + 1}"].Value = Math.Round(data[i][2], 1); // TS
            sheet.Cells[$"E{i + 1}"].Value = Math.Round(data[i][3], 1); // TC
        }

        int count = Math.Max(data.Count, 1);

        // 创建折线图
        var chart = (ExcelLineChart)sheet.Drawings.AddChart("温度曲线", eChartType.Line);
        chart.Title.Text = "温度曲线图";
        chart.XAxis.Title.Text = "时间 (秒)";
        chart.YAxis.Title.Text = "温度 (°C)";
        chart.YAxis.MinValue = 0;
        chart.YAxis.MaxValue = 800;

        // 添加 4 条折线
        var series1 = chart.Series.Add(sheet.Cells[1, 2, count, 2], sheet.Cells[1, 1, count, 1]);
        series1.Header = "炉温1";
        var series2 = chart.Series.Add(sheet.Cells[1, 3, count, 3], sheet.Cells[1, 1, count, 1]);
        series2.Header = "炉温2";
        var series3 = chart.Series.Add(sheet.Cells[1, 4, count, 4], sheet.Cells[1, 1, count, 1]);
        series3.Header = "表面温";
        var series4 = chart.Series.Add(sheet.Cells[1, 5, count, 5], sheet.Cells[1, 1, count, 1]);
        series4.Header = "中心温";

        chart.SetPosition(1, 0, 6, 0);
        chart.SetSize(800, 500);
    }
}
