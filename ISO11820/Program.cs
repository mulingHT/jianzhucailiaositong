namespace ISO11820;

static class Program
{
    /// <summary>
    /// ISO 11820 建筑材料不燃性试验仿真系统 — 主入口
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new LoginForm());
    }
}
