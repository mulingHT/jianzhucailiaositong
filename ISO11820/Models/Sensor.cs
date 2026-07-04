namespace ISO11820.Models;

/// <summary>
/// 传感器配置实体 — 对应 sensors 表
/// </summary>
public class Sensor
{
    public int SensorId { get; set; }
    public string SensorName { get; set; } = "";    // 传感器代号，如 TF1
    public string DispName { get; set; } = "";      // 显示名，如 炉内温度1
    public string SensorGroup { get; set; } = "";   // 分组标识
    public string Unit { get; set; } = "℃";
    public string Discription { get; set; } = "";   // 描述
    public string Flag { get; set; } = "启用";
    public double SignalZero { get; set; }
    public double SignalSpan { get; set; }
    public double OutputZero { get; set; }
    public double OutputSpan { get; set; }
    public double OutputValue { get; set; }          // 当前温度值（运行时更新）
    public double InputValue { get; set; }           // 当前输入值（运行时更新）
    public int SignalType { get; set; } = 4;         // 4=数字量（仿真用）
}
