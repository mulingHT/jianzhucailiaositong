namespace ISO11820.Models;

/// <summary>
/// 样品实体 — 对应 productmaster 表
/// </summary>
public class ProductMaster
{
    public string ProductId { get; set; } = "";   // 样品编号（主键）
    public string ProductName { get; set; } = "";  // 样品名称
    public string Specific { get; set; } = "";     // 规格型号
    public double Diameter { get; set; }           // 直径（mm）
    public double Height { get; set; }             // 高度（mm）
    public string? Flag { get; set; }              // 备用
}
