namespace ISO11820.Models;

/// <summary>
/// 试验状态枚举 — 严格按文档 2.4 节定义
/// </summary>
public enum TestState
{
    Idle,        // 空闲
    Preparing,   // 升温中
    Ready,       // 就绪（温度已达745~755且稳定）
    Recording,   // 记录中
    Complete     // 完成
}
