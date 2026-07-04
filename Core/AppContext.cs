using ISO11820.Data;
using ISO11820.Models;
using Microsoft.Extensions.Configuration;

namespace ISO11820.Core;

/// <summary>
/// 全局应用上下文 — 单例，持有所有核心对象
/// </summary>
public class GlobalContext
{
    private static readonly Lazy<GlobalContext> _instance = new(() => new GlobalContext());
    public static GlobalContext Instance => _instance.Value;

    public IConfiguration Configuration { get; }
    public DbHelper Db { get; }
    public SimulationConfig Simulation { get; }
    public string BaseDirectory { get; }
    public string TestDataDirectory { get; }
    public string ReportDirectory { get; }
    public string DbPath { get; }

    /// <summary>当前登录的操作员用户名</summary>
    public string? CurrentOperator { get; set; }

    /// <summary>当前登录的操作员角色</summary>
    public string? CurrentUserType { get; set; }

    private GlobalContext()
    {
        // 读取配置
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        Configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("Config\\appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // 绑定仿真配置
        Simulation = new SimulationConfig();
        Configuration.GetSection("Simulation").Bind(Simulation);

        // 路径配置
        BaseDirectory = Configuration["FileStorage:BaseDirectory"] ?? "D:\\ISO11820";
        TestDataDirectory = Configuration["FileStorage:TestDataDirectory"] ?? "D:\\ISO11820\\TestData";
        ReportDirectory = Configuration["Report:OutputDirectory"] ?? "D:\\ISO11820\\Reports";

        // 数据库路径
        var dbRelativePath = Configuration["Database:SqlitePath"] ?? "Data\\ISO11820.db";
        DbPath = Path.Combine(basePath, dbRelativePath);

        // 确保目录存在
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
        Directory.CreateDirectory(TestDataDirectory);
        Directory.CreateDirectory(ReportDirectory);

        // 初始化数据库
        Db = new DbHelper(DbPath);
        Db.InitializeDatabase();
    }
}
