using ISO11820.Models;
using MathNet.Numerics;

namespace ISO11820.Core;

/// <summary>
/// 试验控制器 — 5 状态状态机 + 数据广播 + 终止条件判断
/// 严格按开发文档 2.4、2.6、7.3、7.4 节实现
/// </summary>
public class TestMaster
{
    private readonly SimulationConfig _simConfig;
    private readonly SensorSimulator _simulator;
    private readonly System.Timers.Timer _tickTimer;    // 800ms 定时器
    private readonly System.Timers.Timer _secondTimer;   // 1秒定时器

    // 温度历史数据（用于温漂计算，最多600个点）
    private readonly Queue<double> _tf1History = new();
    private readonly Queue<double> _tf2History = new();

    // 每秒记录的温度数据
    private readonly List<double[]> _recordedData = new();

    // 温漂计算相关
    private double _temperatureDrift;

    // 各通道最大温度追踪
    private double _maxTf1, _maxTf2, _maxTs, _maxTc;
    private int _maxTf1Time, _maxTf2Time, _maxTsTime, _maxTcTime;

    // 记录起始温度（用于计算温升）
    private double _startTf1, _startTf2, _startTs, _startTc;

    // PID 输出电源（恒功率计算）
    private int _pidOutputValue;

    /// <summary>当前试验状态</summary>
    public TestState State { get; private set; } = TestState.Idle;

    /// <summary>记录秒数</summary>
    public int ElapsedSeconds { get; private set; }

    /// <summary>当前样品编号</summary>
    public string? CurrentProductId { get; private set; }

    /// <summary>当前试验ID</summary>
    public string? CurrentTestId { get; private set; }

    /// <summary>试验前质量</summary>
    public double PreWeight { get; private set; }

    /// <summary>环境温度</summary>
    public double AmbTemp { get; private set; }

    /// <summary>环境湿度</summary>
    public double AmbHumi { get; private set; }

    /// <summary>当前操作员</summary>
    public string? CurrentOperator { get; private set; }

    /// <summary>试验时长模式：true=标准60分钟，false=自定义</summary>
    public bool IsStandardMode { get; private set; } = true;

    /// <summary>自定义试验时长（秒）</summary>
    public int TargetDurationSeconds { get; private set; } = 3600;

    /// <summary>5通道当前温度 [TF1, TF2, TS, TC, TCal]</summary>
    public double[] Temperatures => _simulator.GetTemperatures();

    /// <summary>温度漂移 (°C/10min)</summary>
    public double TemperatureDrift => _temperatureDrift;

    /// <summary>已记录的温度数据（用于导出）</summary>
    public List<double[]> RecordedData => _recordedData;

    // 各通道最大值（公开只读）
    public double MaxTf1 => _maxTf1;
    public double MaxTf2 => _maxTf2;
    public double MaxTs => _maxTs;
    public double MaxTc => _maxTc;

    /// <summary>数据广播事件（在后台线程触发）</summary>
    public event EventHandler<DataBroadcastEventArgs>? DataBroadcast;

    public TestMaster(SimulationConfig simConfig)
    {
        _simConfig = simConfig;
        _simulator = new SensorSimulator(simConfig);

        // 800ms 定时器（仿真 tick）
        _tickTimer = new System.Timers.Timer(800);
        _tickTimer.AutoReset = true;
        _tickTimer.Elapsed += OnTick;

        // 1秒定时器（数据广播 + 记录）
        _secondTimer = new System.Timers.Timer(1000);
        _secondTimer.AutoReset = true;
        _secondTimer.Elapsed += OnSecond;
    }

    // ===== 试验流程控制 =====

    /// <summary>设置当前试验信息（新建试验后调用）</summary>
    public void SetTestInfo(string productId, string testId, double preWeight,
                            double ambTemp, double ambHumi, string operatorName,
                            bool isStandard, int targetSeconds = 3600)
    {
        CurrentProductId = productId;
        CurrentTestId = testId;
        PreWeight = preWeight;
        AmbTemp = ambTemp;
        AmbHumi = ambHumi;
        CurrentOperator = operatorName;
        IsStandardMode = isStandard;
        TargetDurationSeconds = isStandard ? 3600 : targetSeconds;

        // 如果炉子已经在高温状态，立即检查是否 Ready
        if (State == TestState.Preparing && _simulator.CheckStartCriteria())
        {
            State = TestState.Ready;
            AddMessage("炉温已稳定，可直接开始记录");
            Broadcast();
        }
    }

    /// <summary>开始升温：Idle/Preparing → Preparing</summary>
    public void StartHeating()
    {
        if (State == TestState.Idle)
        {
            State = TestState.Preparing;
            _tickTimer.Start();
            _secondTimer.Start();
            AddMessage("开始升温，系统升温中");
            Broadcast();
        }
        else if (State == TestState.Preparing)
        {
            // 炉子已在高温状态，为新试验重置
            _simulator.Reset();
            _recordedData.Clear();
            _tf1History.Clear();
            _tf2History.Clear();
            ElapsedSeconds = 0;
            AddMessage("已重置，等待温度稳定");
            Broadcast();
        }
    }

    /// <summary>停止升温：回到 Idle，开始降温</summary>
    public void StopHeating()
    {
        if (State != TestState.Preparing && State != TestState.Ready && State != TestState.Complete) return;

        _simulator.IsCoolingDown = true;
        State = TestState.Idle;
        _tickTimer.Stop();
        _secondTimer.Stop();
        ElapsedSeconds = 0;
        AddMessage("停止升温，系统冷却中");
        Broadcast();
    }

    /// <summary>开始记录：Ready → Recording</summary>
    public void StartRecording()
    {
        if (State != TestState.Ready) return;

        // 计算恒功率值
        _pidOutputValue = _simulator.CalculateConstantPower();

        // 记录起点温度
        _startTf1 = _simulator.TF1;
        _startTf2 = _simulator.TF2;
        _startTs = _simulator.TS;
        _startTc = _simulator.TC;

        // 初始化最大值追踪
        _maxTf1 = _simulator.TF1; _maxTf2 = _simulator.TF2;
        _maxTs = _simulator.TS;   _maxTc = _simulator.TC;
        _maxTf1Time = 0; _maxTf2Time = 0; _maxTsTime = 0; _maxTcTime = 0;

        // 清空记录数据
        _recordedData.Clear();
        _tf1History.Clear();
        _tf2History.Clear();

        State = TestState.Recording;
        ElapsedSeconds = 0;
        _simulator.IsRecording = true;

        AddMessage("开始记录，计时开始");
        Broadcast();
    }

    /// <summary>停止记录：Recording → Complete（或 Preparing）</summary>
    public void StopRecording()
    {
        if (State != TestState.Recording) return;

        // 如果有有效记录（>=30秒），进入 Complete；否则回到 Preparing
        if (ElapsedSeconds >= 30)
        {
            State = TestState.Complete;
            _simulator.IsRecording = false;
            AddMessage("用户手动停止记录");
        }
        else
        {
            State = TestState.Preparing;
            _simulator.IsRecording = false;
            AddMessage("记录时间不足，返回升温状态");
        }
        Broadcast();
    }

    /// <summary>保存完成后调用：清空试验缓存，保持炉温</summary>
    public void MarkSaved()
    {
        _simulator.Reset();  // 重置仿真器到目标温度
        _simulator.IsRecording = false;
        State = TestState.Preparing;
        _recordedData.Clear();
        _tf1History.Clear();
        _tf2History.Clear();
        ElapsedSeconds = 0;
        CurrentProductId = null;
        CurrentTestId = null;
        AddMessage("试验记录已保存，炉温保持中");
        Broadcast();
    }

    // ===== 内部定时器逻辑 =====

    /// <summary>800ms tick：仿真更新 + 状态检查</summary>
    private void OnTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (State == TestState.Idle) return;

        // 更新仿真
        _simulator.Update();

        // 状态切换检查
        if (State == TestState.Preparing)
        {
            if (_simulator.CheckStartCriteria())
            {
                State = TestState.Ready;
                AddMessage("温度已稳定，可以开始记录");
            }
        }
        else if (State == TestState.Ready)
        {
            // 温度跌出稳定范围则回退
            if (!_simulator.CheckStartCriteria())
            {
                State = TestState.Preparing;
                AddMessage("温度不稳定，返回升温状态");
            }
        }
    }

    /// <summary>1秒 tick：数据记录 + 温漂计算 + 终止检查 + 广播</summary>
    private void OnSecond(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (State == TestState.Idle) return;

        // 温漂数据收集（所有状态都收集）
        _tf1History.Enqueue(_simulator.TF1);
        _tf2History.Enqueue(_simulator.TF2);
        if (_tf1History.Count > 600) _tf1History.Dequeue();
        if (_tf2History.Count > 600) _tf2History.Dequeue();

        // 计算温漂
        CalculateDrift();

        // Recording 状态的秒级逻辑
        if (State == TestState.Recording)
        {
            ElapsedSeconds++;

            // 记录温度数据
            var temps = _simulator.GetTemperatures();
            _recordedData.Add(temps);

            // 更新最大温度
            UpdateMaxTemps(temps, ElapsedSeconds);

            // 终止检查
            CheckTermination();
        }

        // 广播数据
        Broadcast();
    }

    // ===== 温漂计算 =====

    private void CalculateDrift()
    {
        if (_tf1History.Count < 10) { _temperatureDrift = 0; return; }
        var data = _tf1History.ToArray();
        // 对最近的数据做线性回归，斜率 × 600 = °C/10min
        try
        {
            double[] x = Enumerable.Range(0, data.Length).Select(i => (double)i).ToArray();
            var (a, b) = Fit.Line(x, data);
            _temperatureDrift = b * 600; // 斜率 × 600秒 = 10分钟温漂
        }
        catch
        {
            _temperatureDrift = 0;
        }
    }

    // ===== 终止检查 =====

    private void CheckTermination()
    {
        if (State != TestState.Recording) return;

        if (IsStandardMode)
        {
            // 标准 60 分钟模式：每 5 分钟检查一次（30, 35, 40, 45, 50, 55 分钟）
            int minute = ElapsedSeconds / 60;
            if (minute >= 30 && minute <= 55 && minute % 5 == 0 && ElapsedSeconds % 60 == 0)
            {
                // 10 分钟温漂有效且不超过阈值
                if (_tf1History.Count >= 600 && _tf2History.Count >= 600)
                {
                    double driftTf1 = Math.Abs(_temperatureDrift);
                    // 炉温2的漂移：单独计算
                    double driftTf2 = 0;
                    var tf2Data = _tf2History.ToArray();
                    if (tf2Data.Length >= 10)
                    {
                        double[] x = Enumerable.Range(0, tf2Data.Length).Select(i => (double)i).ToArray();
                        try { driftTf2 = Math.Abs(Fit.Line(x, tf2Data).Item2 * 600); } catch { }
                    }

                    double maxDrift = 2.0; // 默认阈值 2°C/10min
                    if (driftTf1 <= maxDrift && driftTf2 <= maxDrift)
                    {
                        // 满足终止条件
                        State = TestState.Complete;
                        _simulator.IsRecording = false;
                        AddMessage("满足终止条件，试验结束");
                        return;
                    }
                }
            }

            // 60 分钟无条件终止
            if (ElapsedSeconds >= 3600)
            {
                State = TestState.Complete;
                _simulator.IsRecording = false;
                AddMessage("记录时间到达 3600 秒，试验自动结束");
                return;
            }
        }
        else
        {
            // 固定时长模式
            if (ElapsedSeconds >= TargetDurationSeconds)
            {
                State = TestState.Complete;
                _simulator.IsRecording = false;
                AddMessage($"记录时间到达 {TargetDurationSeconds} 秒，试验自动结束");
                return;
            }
        }
    }

    // ===== 最大值追踪 =====

    private void UpdateMaxTemps(double[] temps, int second)
    {
        if (temps[0] > _maxTf1) { _maxTf1 = temps[0]; _maxTf1Time = second; }
        if (temps[1] > _maxTf2) { _maxTf2 = temps[1]; _maxTf2Time = second; }
        if (temps[2] > _maxTs) { _maxTs = temps[2]; _maxTsTime = second; }
        if (temps[3] > _maxTc) { _maxTc = temps[3]; _maxTcTime = second; }
    }

    // ===== 试验结果数据 =====

    /// <summary>获取记录起始温度</summary>
    public double[] GetStartTemps() => new[] { _startTf1, _startTf2, _startTs, _startTc };

    /// <summary>获取记录结束温度</summary>
    public double[] GetFinalTemps()
    {
        var temps = _simulator.GetTemperatures();
        return new[] { temps[0], temps[1], temps[2], temps[3] };
    }

    /// <summary>获取恒功率值</summary>
    public int GetConstantPower() => _pidOutputValue;

    // ===== 消息系统 =====

    private readonly List<MasterMessage> _pendingMessages = new();

    private void AddMessage(string msg)
    {
        _pendingMessages.Add(new MasterMessage(
            DateTime.Now.ToString("HH:mm:ss"), msg));
    }

    /// <summary>
    /// 手动添加系统消息（从UI层调用）
    /// </summary>
    public void AddSystemMessage(string msg)
    {
        AddMessage(msg);
    }

    // ===== 数据广播 =====

    private void Broadcast()
    {
        var msgs = new List<MasterMessage>(_pendingMessages);
        _pendingMessages.Clear();

        var args = new DataBroadcastEventArgs
        {
            Temperatures = _simulator.GetTemperatures(),
            State = State,
            ElapsedSeconds = ElapsedSeconds,
            TemperatureDrift = _temperatureDrift,
            ProductId = CurrentProductId ?? "",
            Messages = msgs
        };

        DataBroadcast?.Invoke(this, args);
    }

    /// <summary>手动触发一次广播（用于UI初始化）</summary>
    public void TriggerBroadcast()
    {
        Broadcast();
    }

    // ===== 清理 =====

    public void Dispose()
    {
        _tickTimer.Stop();
        _tickTimer.Dispose();
        _secondTimer.Stop();
        _secondTimer.Dispose();
    }
}
