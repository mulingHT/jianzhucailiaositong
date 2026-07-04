namespace ISO11820.Models;

/// <summary>
/// 设备实体 — 对应 apparatus 表
/// </summary>
public class Apparatus
{
    public int ApparatusId { get; set; }
    public string InnerNumber { get; set; } = "";       // 内部编号，如 FURNACE-01
    public string ApparatusName { get; set; } = "";      // 设备名称，如 一号试验炉
    public string CheckDateF { get; set; } = "";         // 检定有效期开始
    public string CheckDateT { get; set; } = "";         // 检定有效期结束
    public string PidPort { get; set; } = "";            // PID串口
    public string PowerPort { get; set; } = "";          // 功率串口
    public int? ConstPower { get; set; }                  // 恒功率值
}
