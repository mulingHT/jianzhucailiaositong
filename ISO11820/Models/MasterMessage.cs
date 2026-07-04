namespace ISO11820.Models;

/// <summary>
/// 系统消息 — 显示在消息日志区域
/// </summary>
public class MasterMessage
{
    public string Time { get; set; } = "";     // 格式 HH:mm:ss
    public string Message { get; set; } = "";   // 消息内容

    public MasterMessage() { }

    public MasterMessage(string time, string message)
    {
        Time = time;
        Message = message;
    }
}
