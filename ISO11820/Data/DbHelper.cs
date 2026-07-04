using ISO11820.Models;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace ISO11820.Data;

/// <summary>
/// SQLite 数据库操作封装 — 严格按 DB-数据库设计.md 文档实现
/// </summary>
public class DbHelper
{
    private readonly string _connStr;

    public DbHelper(string dbPath)
    {
        _connStr = $"Data Source={dbPath}";
    }

    // ===== 数据库初始化 =====

    /// <summary>
    /// 首次运行时创建所有表并写入种子数据
    /// </summary>
    public void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();

        // 检查是否已初始化（operators 表存在即视为已初始化）
        var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='operators'";
        if ((long)checkCmd.ExecuteScalar()! > 0)
            return; // 已初始化

        // 创建 operators 表（无主键约束）
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""operators"" (
                ""userid""    TEXT NOT NULL,
                ""username""  TEXT NOT NULL,
                ""pwd""       TEXT NOT NULL,
                ""usertype""  TEXT NOT NULL
            )");

        // 创建 apparatus 表
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""apparatus"" (
                ""apparatusid""   INTEGER NOT NULL CONSTRAINT ""PK_apparatus"" PRIMARY KEY,
                ""innernumber""   TEXT NOT NULL,
                ""apparatusname"" TEXT NOT NULL,
                ""checkdatef""    date NOT NULL,
                ""checkdatet""    date NOT NULL,
                ""pidport""       TEXT NOT NULL,
                ""powerport""     TEXT NOT NULL,
                ""constpower""    INTEGER NULL
            )");

        // 创建 productmaster 表
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""productmaster"" (
                ""productid""   TEXT NOT NULL CONSTRAINT ""PK_productmaster"" PRIMARY KEY,
                ""productname"" TEXT NOT NULL,
                ""specific""    TEXT NOT NULL,
                ""diameter""    REAL NOT NULL,
                ""height""      REAL NOT NULL,
                ""flag""        TEXT NULL
            )");

        // 创建 testmaster 表
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""testmaster"" (
                ""productid""        TEXT NOT NULL,
                ""testid""           TEXT NOT NULL,
                ""testdate""         date NOT NULL,
                ""ambtemp""          REAL NOT NULL,
                ""ambhumi""          REAL NOT NULL,
                ""according""        TEXT NOT NULL,
                ""operator""         TEXT NOT NULL,
                ""apparatusid""      TEXT NOT NULL,
                ""apparatusname""    TEXT NOT NULL,
                ""apparatuschkdate"" date NOT NULL,
                ""rptno""            TEXT NOT NULL,
                ""preweight""        REAL NOT NULL,
                ""postweight""       REAL NOT NULL,
                ""lostweight""       REAL NOT NULL,
                ""lostweight_per""   REAL NOT NULL,
                ""totaltesttime""    INTEGER NOT NULL,
                ""constpower""       INTEGER NOT NULL,
                ""phenocode""        TEXT NOT NULL,
                ""flametime""        INTEGER NOT NULL,
                ""flameduration""    INTEGER NOT NULL,
                ""maxtf1""           REAL NOT NULL,
                ""maxtf2""           REAL NOT NULL,
                ""maxts""            REAL NOT NULL,
                ""maxtc""            REAL NOT NULL,
                ""maxtf1_time""      INTEGER NOT NULL,
                ""maxtf2_time""      INTEGER NOT NULL,
                ""maxts_time""       INTEGER NOT NULL,
                ""maxtc_time""       INTEGER NOT NULL,
                ""finaltf1""         REAL NOT NULL,
                ""finaltf2""         REAL NOT NULL,
                ""finalts""          REAL NOT NULL,
                ""finaltc""          REAL NOT NULL,
                ""finaltf1_time""    INTEGER NOT NULL,
                ""finaltf2_time""    INTEGER NOT NULL,
                ""finalts_time""     INTEGER NOT NULL,
                ""finaltc_time""     INTEGER NOT NULL,
                ""deltatf1""         REAL NOT NULL,
                ""deltatf2""         REAL NOT NULL,
                ""deltatf""          REAL NOT NULL,
                ""deltats""          REAL NOT NULL,
                ""deltatc""          REAL NOT NULL,
                ""memo""             TEXT NULL,
                ""flag""             TEXT NULL,
                CONSTRAINT ""PK_testmaster"" PRIMARY KEY (""productid"", ""testid""),
                CONSTRAINT ""FK_testmaster_productmaster"" FOREIGN KEY (""productid"") REFERENCES ""productmaster"" (""productid"")
            )");

        // 创建 testmaster 索引
        ExecuteNonQuery(conn, @"CREATE INDEX IF NOT EXISTS ""IX_Testmaster_Testdate"" ON ""testmaster"" (""testdate"")");
        ExecuteNonQuery(conn, @"CREATE INDEX IF NOT EXISTS ""IX_Testmaster_Operator"" ON ""testmaster"" (""operator"")");
        ExecuteNonQuery(conn, @"CREATE INDEX IF NOT EXISTS ""IX_Testmaster_Testdate_Productid"" ON ""testmaster"" (""testdate"", ""productid"")");

        // 创建 sensors 表
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""sensors"" (
                ""sensorid""    INTEGER NOT NULL CONSTRAINT ""PK_sensors"" PRIMARY KEY,
                ""sensorname""  TEXT NOT NULL,
                ""dispname""    TEXT NOT NULL,
                ""sensorgroup"" TEXT NOT NULL,
                ""unit""        TEXT NOT NULL,
                ""discription"" TEXT NOT NULL,
                ""flag""        TEXT NOT NULL,
                ""signalzero""  REAL NOT NULL,
                ""signalspan""  REAL NOT NULL,
                ""outputzero""  REAL NOT NULL,
                ""outputspan""  REAL NOT NULL,
                ""outputvalue"" REAL NOT NULL,
                ""inputvalue""  REAL NOT NULL,
                ""signaltype""  INTEGER NOT NULL
            )");

        // 创建 CalibrationRecords 表
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""CalibrationRecords"" (
                ""Id""                 TEXT NOT NULL CONSTRAINT ""PK_CalibrationRecords"" PRIMARY KEY,
                ""CalibrationDate""    TEXT NOT NULL,
                ""CalibrationType""    TEXT NOT NULL,
                ""ApparatusId""        INTEGER NOT NULL,
                ""Operator""           TEXT NOT NULL,
                ""TemperatureData""    TEXT NOT NULL,
                ""UniformityResult""   REAL NULL,
                ""MaxDeviation""       REAL NULL,
                ""AverageTemperature"" REAL NULL,
                ""PassedCriteria""     INTEGER NOT NULL,
                ""Remarks""            TEXT NOT NULL,
                ""CreatedAt""          TEXT NOT NULL,
                ""TempA1"" REAL NULL, ""TempA2"" REAL NULL, ""TempA3"" REAL NULL,
                ""TempB1"" REAL NULL, ""TempB2"" REAL NULL, ""TempB3"" REAL NULL,
                ""TempC1"" REAL NULL, ""TempC2"" REAL NULL, ""TempC3"" REAL NULL,
                ""TAvg""        REAL NULL,
                ""TAvgAxis1""   REAL NULL, ""TAvgAxis2"" REAL NULL, ""TAvgAxis3"" REAL NULL,
                ""TAvgLevela""  REAL NULL, ""TAvgLevelb"" REAL NULL, ""TAvgLevelc"" REAL NULL,
                ""TDevAxis1""   REAL NULL, ""TDevAxis2"" REAL NULL, ""TDevAxis3"" REAL NULL,
                ""TDevLevela""  REAL NULL, ""TDevLevelb"" REAL NULL, ""TDevLevelc"" REAL NULL,
                ""TAvgDevAxis"" REAL NULL, ""TAvgDevLevel"" REAL NULL,
                ""CenterTempData"" TEXT NULL,
                ""Memo""           TEXT NULL
            )");

        ExecuteNonQuery(conn, @"CREATE INDEX IF NOT EXISTS ""IX_CalibrationRecord_Date"" ON ""CalibrationRecords"" (""CalibrationDate"")");
        ExecuteNonQuery(conn, @"CREATE INDEX IF NOT EXISTS ""IX_CalibrationRecord_Operator"" ON ""CalibrationRecords"" (""Operator"")");

        // ===== 种子数据 =====
        // 操作员
        ExecuteNonQuery(conn, @"
            INSERT INTO operators (userid, username, pwd, usertype)
            SELECT '1', 'admin', '123456', 'admin'
            WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'admin')");
        ExecuteNonQuery(conn, @"
            INSERT INTO operators (userid, username, pwd, usertype)
            SELECT '2', 'experimenter', '123456', 'operator'
            WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'experimenter')");

        // 设备
        ExecuteNonQuery(conn, @"
            INSERT INTO apparatus VALUES (0, 'FURNACE-01', '一号试验炉',
                date('now'), date('now', '+1 year'), 'COM9', 'COM9', 2048)");

        // 传感器（业务使用 0、1、2、3、16 + 备用 4~15）
        var sensorSeedSqls = new[]
        {
            "INSERT INTO sensors VALUES (0,'Sensor0','炉温1','采集','℃','炉温1','启用',0,0,0,1000,0,0,4)",
            "INSERT INTO sensors VALUES (1,'Sensor1','炉温2','采集','℃','炉温2','启用',0,0,0,1000,0,0,4)",
            "INSERT INTO sensors VALUES (2,'Sensor2','表面温度','采集','℃','表面温度','启用',0,0,0,1000,0,0,4)",
            "INSERT INTO sensors VALUES (3,'Sensor3','中心温度','采集','℃','中心温度','启用',0,0,0,1000,0,0,4)",
            "INSERT INTO sensors VALUES (16,'Sensor16','校准温度','校准','℃','校准温度','启用',0,0,0,1000,0,0,4)",
        };
        foreach (var sql in sensorSeedSqls)
            ExecuteNonQuery(conn, sql);

        // 备用通道 4~15
        for (int i = 4; i <= 15; i++)
        {
            ExecuteNonQuery(conn, $@"
                INSERT INTO sensors VALUES ({i},'Sensor{i}','备用通道{i + 1}','备用','℃','备用通道','启用',0,0,0,1000,0,0,4)");
        }
    }

    // ===== 登录验证 =====

    public bool Login(string username, string pwd, out string userid, out string usertype)
    {
        userid = ""; usertype = "";
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT userid, usertype FROM operators WHERE username=$name AND pwd=$pwd";
        cmd.Parameters.AddWithValue("$name", username);
        cmd.Parameters.AddWithValue("$pwd", pwd);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            userid = reader.GetString(0);
            usertype = reader.GetString(1);
            return true;
        }
        return false;
    }

    // ===== 设备查询 =====

    public Apparatus? GetApparatus()
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM apparatus LIMIT 1";
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Apparatus
            {
                ApparatusId = reader.GetInt32(0),
                InnerNumber = reader.GetString(1),
                ApparatusName = reader.GetString(2),
                CheckDateF = reader.GetString(3),
                CheckDateT = reader.GetString(4),
                PidPort = reader.GetString(5),
                PowerPort = reader.GetString(6),
                ConstPower = reader.IsDBNull(7) ? null : reader.GetInt32(7),
            };
        }
        return null;
    }

    // ===== 样品操作 =====

    public ProductMaster? GetProduct(string productId)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM productmaster WHERE productid=$pid";
        cmd.Parameters.AddWithValue("$pid", productId);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new ProductMaster
            {
                ProductId = reader.GetString(0),
                ProductName = reader.GetString(1),
                Specific = reader.GetString(2),
                Diameter = reader.GetDouble(3),
                Height = reader.GetDouble(4),
                Flag = reader.IsDBNull(5) ? null : reader.GetString(5),
            };
        }
        return null;
    }

    public void UpsertProduct(ProductMaster p)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO productmaster (productid, productname, specific, diameter, height, flag)
            VALUES ($pid, $pname, $spec, $dia, $h, $flag)
            ON CONFLICT(productid) DO UPDATE SET
                productname=excluded.productname, specific=excluded.specific,
                diameter=excluded.diameter, height=excluded.height, flag=excluded.flag";
        cmd.Parameters.AddWithValue("$pid", p.ProductId);
        cmd.Parameters.AddWithValue("$pname", p.ProductName);
        cmd.Parameters.AddWithValue("$spec", p.Specific);
        cmd.Parameters.AddWithValue("$dia", p.Diameter);
        cmd.Parameters.AddWithValue("$h", p.Height);
        cmd.Parameters.AddWithValue("$flag", (object?)p.Flag ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    // ===== 试验操作（核心）=====

    /// <summary>新建试验（初始插入，统计字段填0）</summary>
    public void InsertTest(string productId, string testId, string operatorName,
                           double preweight, double ambtemp, double ambhumi)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO testmaster
                (productid, testid, testdate, operator, ambtemp, ambhumi,
                 according, apparatusid, apparatusname, apparatuschkdate, rptno,
                 preweight, postweight, lostweight, lostweight_per,
                 totaltesttime, constpower, phenocode, flametime, flameduration,
                 maxtf1,maxtf2,maxts,maxtc,
                 maxtf1_time,maxtf2_time,maxts_time,maxtc_time,
                 finaltf1,finaltf2,finalts,finaltc,
                 finaltf1_time,finaltf2_time,finalts_time,finaltc_time,
                 deltatf1,deltatf2,deltatf,deltats,deltatc)
            VALUES
                ($pid,$tid,date('now'),$op,$ambtemp,$ambhumi,
                 'ISO 11820:2022','FURNACE-01','一号试验炉',date('now'),$rptno,
                 $prewt,0,0,0,
                 0,0,'',0,0,
                 0,0,0,0,0,0,0,0,
                 0,0,0,0,0,0,0,0,
                 0,0,0,0,0)";
        cmd.Parameters.AddWithValue("$pid", productId);
        cmd.Parameters.AddWithValue("$tid", testId);
        cmd.Parameters.AddWithValue("$op", operatorName);
        cmd.Parameters.AddWithValue("$ambtemp", ambtemp);
        cmd.Parameters.AddWithValue("$ambhumi", ambhumi);
        cmd.Parameters.AddWithValue("$rptno", productId);
        cmd.Parameters.AddWithValue("$prewt", preweight);
        cmd.ExecuteNonQuery();
    }

    /// <summary>试验完成后更新统计字段</summary>
    public void UpdateTestResult(string productId, string testId, double preweight,
                                 double postweight, double lostPer,
                                 double deltaTf, double deltaTs,
                                 int totalTime, string phenocode,
                                 double maxTf1, double maxTf2, double maxTs, double maxTc,
                                 int maxTf1Time, int maxTf2Time, int maxTsTime, int maxTcTime,
                                 double finalTf1, double finalTf2, double finalTs, double finalTc,
                                 int finalTf1Time, int finalTf2Time, int finalTsTime, int finalTcTime,
                                 double deltaTf1, double deltaTf2, double deltaTc,
                                 int flameTime, int flameDuration)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE testmaster SET
                postweight      = $post,
                lostweight      = $lost,
                lostweight_per  = $lostper,
                deltatf         = $dtf,
                deltats         = $dts,
                totaltesttime   = $time,
                phenocode       = $pheno,
                flag            = '10000000',
                maxtf1=$maxtf1, maxtf2=$maxtf2, maxts=$maxts, maxtc=$maxtc,
                maxtf1_time=$maxtf1_time, maxtf2_time=$maxtf2_time,
                maxts_time=$maxts_time, maxtc_time=$maxtc_time,
                finaltf1=$finaltf1, finaltf2=$finaltf2, finalts=$finalts, finaltc=$finaltc,
                finaltf1_time=$finaltf1_time, finaltf2_time=$finaltf2_time,
                finalts_time=$finalts_time, finaltc_time=$finaltc_time,
                deltatf1=$deltatf1, deltatf2=$deltatf2, deltatc=$deltatc,
                flametime=$flametime, flameduration=$flameduration
            WHERE productid=$pid AND testid=$tid";
        cmd.Parameters.AddWithValue("$post", postweight);
        cmd.Parameters.AddWithValue("$lost", preweight - postweight);
        cmd.Parameters.AddWithValue("$lostper", lostPer);
        cmd.Parameters.AddWithValue("$dtf", deltaTf);
        cmd.Parameters.AddWithValue("$dts", deltaTs);
        cmd.Parameters.AddWithValue("$time", totalTime);
        cmd.Parameters.AddWithValue("$pheno", phenocode);
        cmd.Parameters.AddWithValue("$maxtf1", maxTf1);
        cmd.Parameters.AddWithValue("$maxtf2", maxTf2);
        cmd.Parameters.AddWithValue("$maxts", maxTs);
        cmd.Parameters.AddWithValue("$maxtc", maxTc);
        cmd.Parameters.AddWithValue("$maxtf1_time", maxTf1Time);
        cmd.Parameters.AddWithValue("$maxtf2_time", maxTf2Time);
        cmd.Parameters.AddWithValue("$maxts_time", maxTsTime);
        cmd.Parameters.AddWithValue("$maxtc_time", maxTcTime);
        cmd.Parameters.AddWithValue("$finaltf1", finalTf1);
        cmd.Parameters.AddWithValue("$finaltf2", finalTf2);
        cmd.Parameters.AddWithValue("$finalts", finalTs);
        cmd.Parameters.AddWithValue("$finaltc", finalTc);
        cmd.Parameters.AddWithValue("$finaltf1_time", finalTf1Time);
        cmd.Parameters.AddWithValue("$finaltf2_time", finalTf2Time);
        cmd.Parameters.AddWithValue("$finalts_time", finalTsTime);
        cmd.Parameters.AddWithValue("$finaltc_time", finalTcTime);
        cmd.Parameters.AddWithValue("$deltatf1", deltaTf1);
        cmd.Parameters.AddWithValue("$deltatf2", deltaTf2);
        cmd.Parameters.AddWithValue("$deltatc", deltaTc);
        cmd.Parameters.AddWithValue("$flametime", flameTime);
        cmd.Parameters.AddWithValue("$flameduration", flameDuration);
        cmd.Parameters.AddWithValue("$pid", productId);
        cmd.Parameters.AddWithValue("$tid", testId);
        cmd.ExecuteNonQuery();
    }

    public TestMaster? GetTest(string productId, string testId)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM testmaster WHERE productid=$pid AND testid=$tid";
        cmd.Parameters.AddWithValue("$pid", productId);
        cmd.Parameters.AddWithValue("$tid", testId);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return ReadTestMaster(reader);
        return null;
    }

    /// <summary>获取最近一次试验（按 testdate + testid 排序）</summary>
    public TestMaster? GetLatestTest()
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM testmaster ORDER BY testdate DESC, testid DESC LIMIT 1";
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return ReadTestMaster(reader);
        return null;
    }

    /// <summary>检查是否存在已完成但未保存的试验（flag != '10000000' 且 totaltesttime > 0）</summary>
    public bool HasUnfinishedTest()
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM testmaster WHERE totaltesttime > 0 AND (flag IS NULL OR flag != '10000000')";
        return (long)cmd.ExecuteScalar()! > 0;
    }

    /// <summary>查询试验历史列表</summary>
    public List<TestMaster> QueryTests(DateTime from, DateTime to, string productId = "", string operatorName = "")
    {
        var result = new List<TestMaster>();
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        var sql = @"
            SELECT * FROM testmaster
            WHERE testdate BETWEEN $from AND $to";
        if (!string.IsNullOrEmpty(productId))
            sql += " AND productid LIKE '%' || $pid || '%'";
        if (!string.IsNullOrEmpty(operatorName))
            sql += " AND operator=$op";
        sql += " ORDER BY testdate DESC";
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));
        if (!string.IsNullOrEmpty(productId))
            cmd.Parameters.AddWithValue("$pid", productId);
        if (!string.IsNullOrEmpty(operatorName))
            cmd.Parameters.AddWithValue("$op", operatorName);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(ReadTestMaster(reader));
        return result;
    }

    // ===== 校准记录操作 =====

    public void InsertCalibration(CalibrationRecord cr)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO CalibrationRecords
                (Id, CalibrationDate, CalibrationType, ApparatusId, Operator,
                 TemperatureData, UniformityResult, MaxDeviation, AverageTemperature,
                 PassedCriteria, Remarks, CreatedAt,
                 TempA1,TempA2,TempA3,TempB1,TempB2,TempB3,TempC1,TempC2,TempC3,
                 TAvg,TAvgAxis1,TAvgAxis2,TAvgAxis3,TAvgLevela,TAvgLevelb,TAvgLevelc,
                 TDevAxis1,TDevAxis2,TDevAxis3,TDevLevela,TDevLevelb,TDevLevelc,
                 TAvgDevAxis,TAvgDevLevel,CenterTempData,Memo)
            VALUES
                ($id,$date,$type,$appid,$op,
                 $tempdata,$uni,$maxdev,$avgtemp,
                 $pass,$remarks,$created,
                 $ta1,$ta2,$ta3,$tb1,$tb2,$tb3,$tc1,$tc2,$tc3,
                 $tavg,$tavgax1,$tavgax2,$tavgax3,$tavglevela,$tavglevelb,$tavglevelc,
                 $tdevax1,$tdevax2,$tdevax3,$tdevlevela,$tdevlevelb,$tdevlevelc,
                 $tavgdevax,$tavgdevlevel,$ctd,$memo)";
        cmd.Parameters.AddWithValue("$id", cr.Id);
        cmd.Parameters.AddWithValue("$date", cr.CalibrationDate);
        cmd.Parameters.AddWithValue("$type", cr.CalibrationType);
        cmd.Parameters.AddWithValue("$appid", cr.ApparatusId);
        cmd.Parameters.AddWithValue("$op", cr.Operator);
        cmd.Parameters.AddWithValue("$tempdata", cr.TemperatureData);
        cmd.Parameters.AddWithValue("$uni", (object?)cr.UniformityResult ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$maxdev", (object?)cr.MaxDeviation ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$avgtemp", (object?)cr.AverageTemperature ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$pass", cr.PassedCriteria);
        cmd.Parameters.AddWithValue("$remarks", cr.Remarks);
        cmd.Parameters.AddWithValue("$created", cr.CreatedAt);
        cmd.Parameters.AddWithValue("$ta1", (object?)cr.TempA1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ta2", (object?)cr.TempA2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ta3", (object?)cr.TempA3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tb1", (object?)cr.TempB1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tb2", (object?)cr.TempB2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tb3", (object?)cr.TempB3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tc1", (object?)cr.TempC1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tc2", (object?)cr.TempC2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tc3", (object?)cr.TempC3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tavg", (object?)cr.TAvg ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tavgax1", (object?)cr.TAvgAxis1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tavgax2", (object?)cr.TAvgAxis2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tavgax3", (object?)cr.TAvgAxis3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tavglevela", (object?)cr.TAvgLevela ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tavglevelb", (object?)cr.TAvgLevelb ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tavglevelc", (object?)cr.TAvgLevelc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tdevax1", (object?)cr.TDevAxis1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tdevax2", (object?)cr.TDevAxis2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tdevax3", (object?)cr.TDevAxis3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tdevlevela", (object?)cr.TDevLevela ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tdevlevelb", (object?)cr.TDevLevelb ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tdevlevelc", (object?)cr.TDevLevelc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tavgdevax", (object?)cr.TAvgDevAxis ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tavgdevlevel", (object?)cr.TAvgDevLevel ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ctd", (object?)cr.CenterTempData ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$memo", (object?)cr.Memo ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<CalibrationRecord> QueryCalibrations(DateTime from, DateTime to)
    {
        var result = new List<CalibrationRecord>();
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT * FROM CalibrationRecords
            WHERE CalibrationDate BETWEEN $from AND $to
            ORDER BY CalibrationDate DESC";
        cmd.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(ReadCalibrationRecord(reader));
        return result;
    }

    // ===== 辅助方法 =====

    private void ExecuteNonQuery(SqliteConnection conn, string sql)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private TestMaster ReadTestMaster(SqliteDataReader reader)
    {
        return new TestMaster
        {
            ProductId = reader.GetString(0),
            TestId = reader.GetString(1),
            TestDate = reader.GetString(2),
            AmbTemp = reader.GetDouble(3),
            AmbHumi = reader.GetDouble(4),
            According = reader.GetString(5),
            Operator = reader.GetString(6),
            ApparatusId = reader.GetString(7),
            ApparatusName = reader.GetString(8),
            ApparatusChkDate = reader.GetString(9),
            RptNo = reader.GetString(10),
            PreWeight = reader.GetDouble(11),
            PostWeight = reader.GetDouble(12),
            LostWeight = reader.GetDouble(13),
            LostWeightPer = reader.GetDouble(14),
            TotalTestTime = reader.GetInt32(15),
            ConstPower = reader.GetInt32(16),
            PhenoCode = reader.GetString(17),
            FlameTime = reader.GetInt32(18),
            FlameDuration = reader.GetInt32(19),
            MaxTf1 = reader.GetDouble(20),
            MaxTf2 = reader.GetDouble(21),
            MaxTs = reader.GetDouble(22),
            MaxTc = reader.GetDouble(23),
            MaxTf1Time = reader.GetInt32(24),
            MaxTf2Time = reader.GetInt32(25),
            MaxTsTime = reader.GetInt32(26),
            MaxTcTime = reader.GetInt32(27),
            FinalTf1 = reader.GetDouble(28),
            FinalTf2 = reader.GetDouble(29),
            FinalTs = reader.GetDouble(30),
            FinalTc = reader.GetDouble(31),
            FinalTf1Time = reader.GetInt32(32),
            FinalTf2Time = reader.GetInt32(33),
            FinalTsTime = reader.GetInt32(34),
            FinalTcTime = reader.GetInt32(35),
            DeltaTf1 = reader.GetDouble(36),
            DeltaTf2 = reader.GetDouble(37),
            DeltaTf = reader.GetDouble(38),
            DeltaTs = reader.GetDouble(39),
            DeltaTc = reader.GetDouble(40),
            Memo = reader.IsDBNull(41) ? null : reader.GetString(41),
            Flag = reader.IsDBNull(42) ? null : reader.GetString(42),
        };
    }

    private CalibrationRecord ReadCalibrationRecord(SqliteDataReader reader)
    {
        return new CalibrationRecord
        {
            Id = reader.GetString(0),
            CalibrationDate = reader.GetString(1),
            CalibrationType = reader.GetString(2),
            ApparatusId = reader.GetInt32(3),
            Operator = reader.GetString(4),
            TemperatureData = reader.GetString(5),
            UniformityResult = reader.IsDBNull(6) ? null : reader.GetDouble(6),
            MaxDeviation = reader.IsDBNull(7) ? null : reader.GetDouble(7),
            AverageTemperature = reader.IsDBNull(8) ? null : reader.GetDouble(8),
            PassedCriteria = reader.GetInt32(9),
            Remarks = reader.GetString(10),
            CreatedAt = reader.GetString(11),
            TempA1 = reader.IsDBNull(12) ? null : reader.GetDouble(12),
            TempA2 = reader.IsDBNull(13) ? null : reader.GetDouble(13),
            TempA3 = reader.IsDBNull(14) ? null : reader.GetDouble(14),
            TempB1 = reader.IsDBNull(15) ? null : reader.GetDouble(15),
            TempB2 = reader.IsDBNull(16) ? null : reader.GetDouble(16),
            TempB3 = reader.IsDBNull(17) ? null : reader.GetDouble(17),
            TempC1 = reader.IsDBNull(18) ? null : reader.GetDouble(18),
            TempC2 = reader.IsDBNull(19) ? null : reader.GetDouble(19),
            TempC3 = reader.IsDBNull(20) ? null : reader.GetDouble(20),
            TAvg = reader.IsDBNull(21) ? null : reader.GetDouble(21),
            TAvgAxis1 = reader.IsDBNull(22) ? null : reader.GetDouble(22),
            TAvgAxis2 = reader.IsDBNull(23) ? null : reader.GetDouble(23),
            TAvgAxis3 = reader.IsDBNull(24) ? null : reader.GetDouble(24),
            TAvgLevela = reader.IsDBNull(25) ? null : reader.GetDouble(25),
            TAvgLevelb = reader.IsDBNull(26) ? null : reader.GetDouble(26),
            TAvgLevelc = reader.IsDBNull(27) ? null : reader.GetDouble(27),
            TDevAxis1 = reader.IsDBNull(28) ? null : reader.GetDouble(28),
            TDevAxis2 = reader.IsDBNull(29) ? null : reader.GetDouble(29),
            TDevAxis3 = reader.IsDBNull(30) ? null : reader.GetDouble(30),
            TDevLevela = reader.IsDBNull(31) ? null : reader.GetDouble(31),
            TDevLevelb = reader.IsDBNull(32) ? null : reader.GetDouble(32),
            TDevLevelc = reader.IsDBNull(33) ? null : reader.GetDouble(33),
            TAvgDevAxis = reader.IsDBNull(34) ? null : reader.GetDouble(34),
            TAvgDevLevel = reader.IsDBNull(35) ? null : reader.GetDouble(35),
            CenterTempData = reader.IsDBNull(36) ? null : reader.GetString(36),
            Memo = reader.IsDBNull(37) ? null : reader.GetString(37),
        };
    }
}
