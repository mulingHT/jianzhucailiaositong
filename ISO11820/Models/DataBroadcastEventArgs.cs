using ISO11820.Models;

namespace ISO11820.Models;

/// <summary>
/// 数据广播事件参数 — 用于后台线程向 UI 线程推送数据
/// </summary>
public class DataBroadcastEventArgs : EventArgs
{
    /// <summary>5通道温度值 [TF1, TF2, TS, TC, TCal]</summary>
    public double[] Temperatures { get; set; } = new double[5];

    /// <summary>当前试验状态</summary>
    public TestState State { get; set; }

    /// <summary>已记录秒数</summary>
    public int ElapsedSeconds { get; set; }

    /// <summary>温漂值（°C/10min）</summary>
    public double TemperatureDrift { get; set; }

    /// <summary>当前样品编号</summary>
    public string ProductId { get; set; } = "";

    /// <summary>新产生的系统消息列表</summary>
    public List<MasterMessage> Messages { get; set; } = new();
}
