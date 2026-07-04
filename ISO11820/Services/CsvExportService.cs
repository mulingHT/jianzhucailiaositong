using ISO11820.Core;

namespace ISO11820.Services;

/// <summary>
/// CSV 导出服务 — 温度时序数据
/// 文件路径：{TestDataDirectory}\{productid}\{testid}\sensor_data.csv
/// </summary>
public class CsvExportService
{
    private readonly string _testDataDir;

    public CsvExportService(string testDataDir)
    {
        _testDataDir = testDataDir;
    }

    /// <summary>
    /// 将记录的温度数据导出为 CSV 文件
    /// </summary>
    public string Export(string productId, string testId, List<double[]> recordedData)
    {
        var dir = Path.Combine(_testDataDir, productId, testId);
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, "sensor_data.csv");

        using var writer = new StreamWriter(filePath);
        writer.WriteLine("Time,Temp1,Temp2,TempSurface,TempCenter,TempCalibration");
        for (int i = 0; i < recordedData.Count; i++)
        {
            var d = recordedData[i];
            writer.WriteLine($"{i},{d[0]:F1},{d[1]:F1},{d[2]:F1},{d[3]:F1},{d[4]:F1}");
        }

        return filePath;
    }
}
