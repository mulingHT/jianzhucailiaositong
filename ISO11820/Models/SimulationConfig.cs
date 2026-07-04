namespace ISO11820.Models;

/// <summary>
/// 仿真配置 — 对应 appsettings.json 中的 Simulation 节
/// </summary>
public class SimulationConfig
{
    public bool EnableSimulation { get; set; } = true;
    public bool SimulateSensors { get; set; } = true;
    public bool SimulatePidController { get; set; } = true;
    public double InitialFurnaceTemp { get; set; } = 720.0;
    public double TargetFurnaceTemp { get; set; } = 750.0;
    public double HeatingRatePerSecond { get; set; } = 40.0;
    public double TempFluctuation { get; set; } = 0.5;
    public double StableThreshold { get; set; } = 3.0;
    public bool SimulateFlame { get; set; } = false;
}
