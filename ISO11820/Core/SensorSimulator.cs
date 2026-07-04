using ISO11820.Models;

namespace ISO11820.Core;

/// <summary>
/// 温度仿真引擎 — 严格按开发文档 2.3 节算法实现
/// 每 800ms 执行一次 Update()，生成 5 通道温度数据
/// </summary>
public class SensorSimulator
{
    private readonly SimulationConfig _config;
    private readonly Random _rng = new();

    // 5 通道当前温度
    public double TF1 { get; private set; }  // 炉温1
    public double TF2 { get; private set; }  // 炉温2
    public double TS { get; private set; }   // 表面温
    public double TC { get; private set; }   // 中心温
    public double TCal { get; private set; } // 校准温

    /// <summary>稳定计数器（每 tick > 3 即 IsStable = true）</summary>
    public int StableCounter { get; private set; }

    /// <summary>温度是否稳定</summary>
    public bool IsStable { get; private set; }

    /// <summary>PID 输出值队列（最多600个）</summary>
    public Queue<double> PidOutputQueue { get; } = new();

    /// <summary>恒功率值</summary>
    public int ConstantPower { get; set; }

    /// <summary>是否处于记录阶段（影响 TS/TC 行为）</summary>
    public bool IsRecording { get; set; }

    /// <summary>是否停止加热（降温模式）</summary>
    public bool IsCoolingDown { get; set; }

    public SensorSimulator(SimulationConfig config)
    {
        _config = config;
        // 初始温度
        TF1 = config.InitialFurnaceTemp;
        TF2 = config.InitialFurnaceTemp;
        TS = config.InitialFurnaceTemp * 0.3;
        TC = config.InitialFurnaceTemp * 0.25;
        TCal = config.InitialFurnaceTemp;
    }

    /// <summary>
    /// 每 800ms 调用一次，更新所有温度通道
    /// </summary>
    public void Update()
    {
        double noise() => (_rng.NextDouble() * 2 - 1) * _config.TempFluctuation;

        if (IsCoolingDown)
        {
            // 降温阶段
            TF1 -= 0.5 + noise() * 0.1;
            TF2 -= 0.5 + noise() * 0.1;
            if (TF1 < 25) { TF1 = 25; IsCoolingDown = false; }
            if (TF2 < 25) TF2 = 25;
        }
        else if (TF1 < _config.TargetFurnaceTemp - _config.StableThreshold) // < 747°C
        {
            // === 升温阶段 ===
            TF1 += _config.HeatingRatePerSecond * 0.8 + noise();
            TF2 += _config.HeatingRatePerSecond * 0.8 + noise();
            TS = TF1 * 0.3 + noise();
            TC = TF1 * 0.25 + noise();
            TCal = TF1 + noise() * 2;
            StableCounter = 0;
            IsStable = false;
        }
        else
        {
            // === 稳定阶段（TF1 >= 747°C）===
            TF1 = _config.TargetFurnaceTemp + noise();
            TF2 = _config.TargetFurnaceTemp + noise();
            TCal = TF1 + noise() * 2;

            if (IsRecording)
            {
                // 记录阶段：TS/TC 指数接近炉温
                double surfaceTarget = Math.Min(TF1 * 0.95, 800);
                TS += (surfaceTarget - TS) * 0.02 + noise();
                double centerTarget = Math.Min(TF1 * 0.85, 750);
                TC += (centerTarget - TC) * 0.01 + noise();
            }
            else
            {
                // 非记录阶段：TS/TC 低值跟随
                TS = TF1 * 0.3 + noise();
                TC = TF1 * 0.25 + noise();
            }

            // 稳定计数器累加
            StableCounter++;
            if (StableCounter > 3)
                IsStable = true;
        }

        // PID 输出队列（仿真模式：用炉温1模拟 PID 输出）
        PidOutputQueue.Enqueue(TF1);
        if (PidOutputQueue.Count > 600)
            PidOutputQueue.Dequeue();
    }

    /// <summary>
    /// 检查是否可以进入 Ready 状态：745~755°C 且 IsStable
    /// </summary>
    public bool CheckStartCriteria()
    {
        return TF1 >= 745 && TF1 <= 755 && IsStable;
    }

    /// <summary>
    /// 获取当前恒功率值（队列平均值）
    /// </summary>
    public int CalculateConstantPower()
    {
        if (PidOutputQueue.Count == 0) return (int)_config.TargetFurnaceTemp;
        return (int)PidOutputQueue.Average();
    }

    /// <summary>
    /// 获取 5 通道当前温度数组
    /// </summary>
    public double[] GetTemperatures() => new[] { TF1, TF2, TS, TC, TCal };

    /// <summary>
    /// 重置仿真器到初始状态
    /// </summary>
    public void Reset()
    {
        TF1 = _config.TargetFurnaceTemp;
        TF2 = _config.TargetFurnaceTemp;
        TS = _config.TargetFurnaceTemp * 0.3;
        TC = _config.TargetFurnaceTemp * 0.25;
        TCal = _config.TargetFurnaceTemp;
        StableCounter = 0;
        IsStable = false;
        IsRecording = false;
        IsCoolingDown = false;
        PidOutputQueue.Clear();
        ConstantPower = 0;
    }

    /// <summary>
    /// 完全冷却：回到室温
    /// </summary>
    public void CoolToRoomTemp()
    {
        TF1 = 25;
        TF2 = 25;
        TS = TF1 * 0.3;
        TC = TF1 * 0.25;
        TCal = 25;
        StableCounter = 0;
        IsStable = false;
        IsRecording = false;
        IsCoolingDown = false;
        PidOutputQueue.Clear();
        ConstantPower = 0;
    }
}
