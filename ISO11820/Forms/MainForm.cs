using ISO11820.Core;
using ISO11820.Data;
using ISO11820.Helpers;
using ISO11820.Models;
using ISO11820.Services;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.WindowsForms;
using TestEntity = ISO11820.Models.TestMaster;

namespace ISO11820.Forms;

/// <summary>
/// 主窗体 — 试验监控、记录查询、设备校准、参数设置
/// </summary>
public partial class MainForm : Form
{
    private readonly ISO11820.Core.TestMaster _testMaster;
    private readonly CsvExportService _csvExport;
    private readonly ExcelExportService _excelExport;
    private readonly PdfExportService _pdfExport;
    private readonly DbHelper _db;

    // 温度显示标签（LED风格）
    private Label lblTF1 = null!, lblTF2 = null!, lblTS = null!, lblTC = null!, lblTCal = null!;
    private Label lblState = null!, lblTimer = null!, lblDrift = null!, lblProductId = null!;

    // 按钮
    private Button btnNewTest = null!, btnStartHeat = null!, btnStopHeat = null!;
    private Button btnStartRecord = null!, btnStopRecord = null!, btnTestRecord = null!;

    // OxyPlot
    private PlotView plotView = null!;
    private PlotModel plotModel = null!;
    private LineSeries seriesTF1 = null!, seriesTF2 = null!, seriesTS = null!, seriesTC = null!;
    private readonly List<double> _plotTime = new();
    private readonly List<double> _plotTF1 = new(), _plotTF2 = new(), _plotTS = new(), _plotTC = new();
    private int _plotDataCount = 0;

    // 消息日志
    private RichTextBox rtbLog = null!;

    // 动画相关
    private System.Windows.Forms.Timer? _ledPulseTimer;
    private float _ledPulsePhase = 0f;
    private bool _isLedPulsing = false;

    // 状态指示点
    private Panel? _statusDotPanel;
    private float _statusPulsePhase = 0f;

    // 升温进度
    private Panel? _heatProgressBar;
    private Label? _heatProgressLabel;

    // TabControl
    private TabControl tabControl = null!;
    private TabPage tabMonitor = null!, tabQuery = null!, tabCalibration = null!, tabSettings = null!;

    // 记录查询控件
    private DataGridView dgvRecords = null!;
    private DateTimePicker dtpFrom = null!, dtpTo = null!;
    private TextBox txtQueryProductId = null!;
    private Button btnQuery = null!, btnExportQuery = null!;

    // 设备校准控件
    private Label lblCalTemp = null!;
    private Button btnRecordCal = null!, btnSaveCal = null!, btnQueryCal = null!;
    private DataGridView dgvCalibrations = null!;

    public MainForm()
    {
        _db = GlobalContext.Instance.Db;
        _testMaster = new ISO11820.Core.TestMaster(GlobalContext.Instance.Simulation);
        _csvExport = new CsvExportService(GlobalContext.Instance.TestDataDirectory);
        _excelExport = new ExcelExportService();
        _pdfExport = new PdfExportService();

        InitializeComponent();
        UpdateButtonStates();

        // 订阅数据广播事件
        _testMaster.DataBroadcast += OnDataBroadcast;

        // 初始化系统消息
        _testMaster.AddSystemMessage($"系统初始化，操作员：{GlobalContext.Instance.CurrentOperator}");
        _testMaster.TriggerBroadcast();

        // 定时刷新按钮状态
        var stateTimer = new System.Windows.Forms.Timer { Interval = 500 };
        stateTimer.Tick += (s, e) => UpdateButtonStates();
        stateTimer.Start();

        // LED 脉冲动画（仅 Recording 状态时使用）
        _ledPulseTimer = new System.Windows.Forms.Timer { Interval = 60 };
        _ledPulseTimer.Tick += LedPulseTick;

        // 窗口淡入动画
        this.Load += (s, e) => ThemeColors.ApplyFadeIn(this, 14, 0.07);
    }

    #region 界面构建

    private void InitializeComponent()
    {
        this.Text = "ISO 11820 建筑材料不燃性试验系统";
        this.Size = new Size(1600, 960);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(1200, 780);
        this.BackColor = ThemeColors.Surface;

        tabControl = new TabControl
        {
            Location = new Point(8, 8),
            Size = new Size(1568, 908),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            DrawMode = TabDrawMode.OwnerDrawFixed,
            ItemSize = new Size(200, 42)
        };
        tabControl.DrawItem += DrawTabItem;

        // Tab 1: 试验监控
        tabMonitor = new TabPage("试验监控");
        BuildMonitorTab();
        tabControl.TabPages.Add(tabMonitor);

        // Tab 2: 记录查询
        tabQuery = new TabPage("记录查询");
        BuildQueryTab();
        tabControl.TabPages.Add(tabQuery);

        // Tab 3: 设备校准
        tabCalibration = new TabPage("设备校准");
        BuildCalibrationTab();
        tabControl.TabPages.Add(tabCalibration);

        // Tab 4: 参数设置
        tabSettings = new TabPage("参数设置");
        BuildSettingsTab();
        tabControl.TabPages.Add(tabSettings);

        this.Controls.Add(tabControl);
        this.FormClosing += MainForm_FormClosing;
    }

    private void DrawTabItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= tabControl.TabPages.Count) return;
        var tabPage = tabControl.TabPages[e.Index];
        var tabRect = tabControl.GetTabRect(e.Index);
        bool isSelected = tabControl.SelectedIndex == e.Index;

        using var backBrush = new SolidBrush(isSelected ? ThemeColors.TabActiveBg : ThemeColors.TabInactiveBg);
        e.Graphics.FillRectangle(backBrush, tabRect);

        using var textBrush = new SolidBrush(isSelected ? ThemeColors.BrandPrimary : ThemeColors.TextSecondary);
        using var font = new Font("Microsoft YaHei", 9.5f, isSelected ? FontStyle.Bold : FontStyle.Regular);
        var textSize = e.Graphics.MeasureString(tabPage.Text, font);
        float textX = tabRect.X + (tabRect.Width - textSize.Width) / 2;
        float textY = tabRect.Y + (tabRect.Height - textSize.Height) / 2;
        e.Graphics.DrawString(tabPage.Text, font, textBrush, textX, textY);

        if (isSelected)
        {
            using var indicatorBrush = new SolidBrush(ThemeColors.TabIndicator);
            e.Graphics.FillRectangle(indicatorBrush, tabRect.X + 4, tabRect.Bottom - 3, tabRect.Width - 8, 3);
        }
    }

    private void BuildMonitorTab()
    {
        tabMonitor.BackColor = ThemeColors.Surface;
        tabMonitor.AutoScroll = true;

        // ===== 左侧面板：信息 & 控制区 =====
        int panelWidth = 540;
        int panelHeight = 840;

        var panelLeft = new Panel
        {
            Location = new Point(6, 6),
            Size = new Size(panelWidth, panelHeight),
            BackColor = ThemeColors.CardBackground,
            BorderStyle = BorderStyle.None
        };
        panelLeft.Paint += (s, e) =>
        {
            using var pen = new Pen(ThemeColors.CardBorder);
            e.Graphics.DrawRectangle(pen, 0, 0, panelLeft.Width - 1, panelLeft.Height - 1);
        };

        int y = 10;

        // ===== 样品 & 状态信息栏 =====
        var infoPanel = new Panel
        {
            Location = new Point(10, y),
            Size = new Size(520, 72),
            BackColor = ThemeColors.InfoPanelBg,
            BorderStyle = BorderStyle.None
        };
        infoPanel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(ThemeColors.CardBorder);
            e.Graphics.DrawRectangle(pen, 0, 0, infoPanel.Width - 1, infoPanel.Height - 1);
        };

        var lblProductTitle = new Label { Text = "当前样品", Location = new Point(14, 8), Size = new Size(65, 22), ForeColor = ThemeColors.TextSecondary, Font = ThemeColors.SmallLabelFont };
        lblProductId = new Label { Text = "-", Location = new Point(84, 8), Size = new Size(220, 22), Font = ThemeColors.MonoFont, ForeColor = ThemeColors.TextPrimary };

        _statusDotPanel = new Panel { Location = new Point(14, 40), Size = new Size(16, 16), BackColor = ThemeColors.InfoPanelBg };
        _statusDotPanel.Paint += (s, e) => ThemeColors.PaintStatusDot(e.Graphics, 2, 2, 12, ThemeColors.StateIdle);
        infoPanel.Controls.Add(_statusDotPanel);

        var lblStateTitle = new Label { Text = "状态", Location = new Point(34, 38), Size = new Size(38, 22), ForeColor = ThemeColors.TextSecondary, Font = ThemeColors.SmallLabelFont };
        lblState = new Label { Text = "空闲", Location = new Point(84, 38), Size = new Size(100, 22), Font = ThemeColors.SubtitleFont, ForeColor = ThemeColors.StateIdle };

        var lblTimerTitle = new Label { Text = "计时", Location = new Point(250, 8), Size = new Size(38, 22), ForeColor = ThemeColors.TextSecondary, Font = ThemeColors.SmallLabelFont };
        lblTimer = new Label { Text = "0 秒", Location = new Point(295, 8), Size = new Size(110, 22), Font = ThemeColors.MonoFont, ForeColor = ThemeColors.StateReady };

        var lblDriftTitle = new Label { Text = "温漂", Location = new Point(250, 38), Size = new Size(38, 22), ForeColor = ThemeColors.TextSecondary, Font = ThemeColors.SmallLabelFont };
        lblDrift = new Label { Text = "0.00 °C/10min", Location = new Point(295, 38), Size = new Size(200, 22), Font = ThemeColors.MonoFont, ForeColor = ThemeColors.TextPrimary };

        infoPanel.Controls.AddRange(new Control[] { lblProductTitle, lblProductId, lblStateTitle, lblState, lblTimerTitle, lblTimer, lblDriftTitle, lblDrift });
        panelLeft.Controls.Add(infoPanel);
        y += 82;

        // ===== 升温进度条 =====
        var progressPanel = new Panel
        {
            Location = new Point(10, y),
            Size = new Size(520, 28),
            BackColor = ThemeColors.InfoPanelBg,
            BorderStyle = BorderStyle.None
        };
        progressPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(ThemeColors.CardBorder);
            e.Graphics.DrawRectangle(pen, 0, 0, progressPanel.Width - 1, progressPanel.Height - 1);
        };
        var progressBg = new Panel
        {
            Location = new Point(10, 10),
            Size = new Size(430, 10),
            BackColor = ThemeColors.ProgressBg,
            BorderStyle = BorderStyle.None
        };
        _heatProgressBar = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(0, 10),
            BackColor = ThemeColors.ProgressFill,
            BorderStyle = BorderStyle.None
        };
        progressBg.Controls.Add(_heatProgressBar);
        _heatProgressLabel = new Label
        {
            Text = "未开始升温",
            Location = new Point(10, 3),
            Size = new Size(500, 22),
            ForeColor = ThemeColors.TextSecondary,
            Font = ThemeColors.SmallLabelFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        progressPanel.Controls.Add(progressBg);
        progressPanel.Controls.Add(_heatProgressLabel);
        panelLeft.Controls.Add(progressPanel);
        y += 36;

        // ===== 温度显示 — 渐变标题 + LED =====
        var tempHeader = ThemeColors.CreateGradientHeader("◆  TEMPERATURE MONITOR", 10, y, 520, 30,
            ThemeColors.GradientStart, ThemeColors.GradientEnd);
        panelLeft.Controls.Add(tempHeader);
        y += 35;

        var tempConfigs = new (string ShortTitle, Label LabelRef)[]
        {
            ("TF1", null!), ("TF2", null!), ("TS", null!), ("TC", null!), ("TCal", null!),
        };
        tempConfigs[0].LabelRef = lblTF1 = CreateTempLabel();
        tempConfigs[1].LabelRef = lblTF2 = CreateTempLabel();
        tempConfigs[2].LabelRef = lblTS = CreateTempLabel();
        tempConfigs[3].LabelRef = lblTC = CreateTempLabel();
        tempConfigs[4].LabelRef = lblTCal = CreateTempLabel();

        int ledGap = 5;
        int ledWidth = (520 - 10 - ledGap * 4) / 5;
        int ledHeight = 74;

        for (int i = 0; i < tempConfigs.Length; i++)
        {
            var cfg = tempConfigs[i];
            int cx = 10 + i * (ledWidth + ledGap);

            var ledCard = new Panel
            {
                Location = new Point(cx, y),
                Size = new Size(ledWidth, ledHeight),
                BackColor = ThemeColors.LedBezel,
                BorderStyle = BorderStyle.None
            };
            ledCard.Paint += (s, e) =>
            {
                using var outerPen = new Pen(Color.FromArgb(20, 20, 36));
                e.Graphics.DrawRectangle(outerPen, 0, 0, ledCard.Width - 1, ledCard.Height - 1);
                using var innerPen = new Pen(Color.FromArgb(50, 50, 70));
                e.Graphics.DrawRectangle(innerPen, 1, 1, ledCard.Width - 3, ledCard.Height - 3);
            };

            var ledTitle = new Label
            {
                Text = cfg.ShortTitle,
                Location = new Point(2, 2),
                Size = new Size(ledWidth - 4, 16),
                ForeColor = ThemeColors.LedTitle,
                Font = new Font("Consolas", 7.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            cfg.LabelRef.Location = new Point(2, 20);
            cfg.LabelRef.Size = new Size(ledWidth - 4, 50);

            ledCard.Controls.Add(ledTitle);
            ledCard.Controls.Add(cfg.LabelRef);
            panelLeft.Controls.Add(ledCard);
        }
        y += ledHeight + 8;

        // ===== 按钮区 =====
        var btnHeader = ThemeColors.CreateGradientHeader("◆  CONTROLS", 10, y, 520, 30,
            ThemeColors.GradientStart, ThemeColors.GradientEnd);
        panelLeft.Controls.Add(btnHeader);
        y += 35;

        int btnW = 122;
        int btnH = 36;
        int btnGapX = 7;
        int btnStartX = 10;

        // 第一行：4 个按钮
        btnNewTest = ThemeColors.StyleButton(new Button { Text = "新建试验", Location = new Point(btnStartX, y), Size = new Size(btnW, btnH) }, ThemeColors.BtnSuccess);
        btnStartHeat = ThemeColors.StyleButton(new Button { Text = "开始升温", Location = new Point(btnStartX + btnW + btnGapX, y), Size = new Size(btnW, btnH) }, ThemeColors.BtnWarning);
        btnStopHeat = ThemeColors.StyleButton(new Button { Text = "停止升温", Location = new Point(btnStartX + 2 * (btnW + btnGapX), y), Size = new Size(btnW, btnH) }, ThemeColors.BtnDanger);
        btnStartRecord = ThemeColors.StyleButton(new Button { Text = "开始记录", Location = new Point(btnStartX + 3 * (btnW + btnGapX), y), Size = new Size(btnW, btnH) }, ThemeColors.BtnPrimary);
        y += btnH + 6;

        // 第二行：2 个按钮居中
        int row2TotalW = 2 * btnW + btnGapX;
        int row2X = 10 + (520 - row2TotalW) / 2;
        btnStopRecord = ThemeColors.StyleButton(new Button { Text = "停止记录", Location = new Point(row2X, y), Size = new Size(btnW, btnH) }, ThemeColors.BtnDanger);
        btnTestRecord = ThemeColors.StyleButton(new Button { Text = "试验记录", Location = new Point(row2X + btnW + btnGapX, y), Size = new Size(btnW, btnH) }, ThemeColors.BtnPurple);
        y += btnH + 10;

        btnNewTest.Click += BtnNewTest_Click;
        btnStartHeat.Click += BtnStartHeat_Click;
        btnStopHeat.Click += BtnStopHeat_Click;
        btnStartRecord.Click += BtnStartRecord_Click;
        btnStopRecord.Click += BtnStopRecord_Click;
        btnTestRecord.Click += BtnTestRecord_Click;

        // 先添加按钮，确保它们不被其他控件覆盖
        foreach (var btn in new[] { btnNewTest, btnStartHeat, btnStopHeat, btnStartRecord, btnStopRecord, btnTestRecord })
        {
            panelLeft.Controls.Add(btn);
            btn.BringToFront();
        }

        // ===== 系统消息日志 =====
        var logHeader = ThemeColors.CreateGradientHeader("◆  SYSTEM LOG", 10, y, 520, 30,
            ThemeColors.GradientStart, ThemeColors.GradientEnd);
        panelLeft.Controls.Add(logHeader);
        y += 34;

        // 计算日志剩余高度
        int logHeight = panelHeight - y - 8;
        if (logHeight < 100) logHeight = 100;

        rtbLog = new RichTextBox
        {
            Location = new Point(12, y),
            Size = new Size(516, logHeight),
            ReadOnly = true,
            BackColor = ThemeColors.LogBg,
            ForeColor = ThemeColors.LogTextNormal,
            Font = ThemeColors.MonoSmallFont,
            BorderStyle = BorderStyle.None,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        panelLeft.Controls.Add(rtbLog);

        tabMonitor.Controls.Add(panelLeft);

        // ===== 右侧：OxyPlot 温度曲线图（带面积填充）=====
        plotModel = new PlotModel
        {
            Title = "实时温度曲线",
            Background = ThemeColors.ChartBg,
            PlotAreaBackground = ThemeColors.ChartPlotAreaBg,
            PlotAreaBorderColor = OxyColor.FromRgb(200, 208, 216),
            TitleColor = ThemeColors.ChartTitle,
            TitleFont = "Microsoft YaHei",
            TitleFontSize = 15,
            TitleFontWeight = FontWeights.Bold,
            IsLegendVisible = true
        };

        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "温度 (°C)",
            Minimum = 0,
            Maximum = 800,
            MajorGridlineColor = ThemeColors.ChartGrid,
            MinorGridlineColor = OxyColors.Transparent,
            TicklineColor = OxyColor.FromRgb(200, 208, 216),
            TitleColor = ThemeColors.ChartAxisText,
            TextColor = ThemeColors.ChartAxisText,
            TitleFontSize = 11,
            FontSize = 9,
            MajorStep = 100
        };
        var xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "时间 (秒)",
            MajorGridlineColor = ThemeColors.ChartGrid,
            MinorGridlineColor = OxyColors.Transparent,
            TicklineColor = OxyColor.FromRgb(200, 208, 216),
            TitleColor = ThemeColors.ChartAxisText,
            TextColor = ThemeColors.ChartAxisText,
            TitleFontSize = 11,
            FontSize = 9
        };
        plotModel.Axes.Add(yAxis);
        plotModel.Axes.Add(xAxis);

        // 面积填充系列（先添加，在折线下方）
        var areaTF1 = new AreaSeries { Title = null, Color = OxyColors.Transparent, Fill = ThemeColors.AreaTF1, StrokeThickness = 0, ConstantY2 = 0 };
        var areaTF2 = new AreaSeries { Title = null, Color = OxyColors.Transparent, Fill = ThemeColors.AreaTF2, StrokeThickness = 0, ConstantY2 = 0 };
        var areaTS = new AreaSeries { Title = null, Color = OxyColors.Transparent, Fill = ThemeColors.AreaTS, StrokeThickness = 0, ConstantY2 = 0 };
        var areaTC = new AreaSeries { Title = null, Color = OxyColors.Transparent, Fill = ThemeColors.AreaTC, StrokeThickness = 0, ConstantY2 = 0 };

        seriesTF1 = new LineSeries { Title = "炉温1", Color = ThemeColors.LineTF1, StrokeThickness = 2.5, MarkerType = MarkerType.None };
        seriesTF2 = new LineSeries { Title = "炉温2", Color = ThemeColors.LineTF2, StrokeThickness = 2.5, MarkerType = MarkerType.None };
        seriesTS = new LineSeries { Title = "表面温", Color = ThemeColors.LineTS, StrokeThickness = 2.5, MarkerType = MarkerType.None };
        seriesTC = new LineSeries { Title = "中心温", Color = ThemeColors.LineTC, StrokeThickness = 2.5, MarkerType = MarkerType.None };

        foreach (var s in new Series[] { areaTF1, areaTF2, areaTS, areaTC, seriesTF1, seriesTF2, seriesTS, seriesTC })
            plotModel.Series.Add(s);

        int plotX = 560;
        int plotW = 1570 - plotX;
        plotView = new PlotView
        {
            Location = new Point(plotX, 6),
            Size = new Size(plotW, panelHeight),
            Model = plotModel,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.White
        };
        tabMonitor.Controls.Add(plotView);
    }

    private Label CreateTempLabel()
    {
        return new Label
        {
            Text = "0.0 °C",
            Font = ThemeColors.LedFont,
            BackColor = ThemeColors.LedBg,
            ForeColor = ThemeColors.LedText,
            TextAlign = ContentAlignment.MiddleCenter,
            BorderStyle = BorderStyle.None,
            Tag = "tempLed"
        };
    }

    private Button CreateButton(string text, int x, int y, Color backColor)
    {
        return ThemeColors.StyleButton(new Button { Text = text, Location = new Point(x, y), Size = new Size(100, 38) }, backColor);
    }

    /// <summary>
    /// LED 脉冲动画 — Recording 状态时温度显示呼吸灯效果
    /// </summary>
    private void LedPulseTick(object? sender, EventArgs e)
    {
        _ledPulsePhase += 0.08f;
        float brightness = 0.7f + 0.3f * (float)Math.Sin(_ledPulsePhase);

        // 脉冲 LED 文字颜色
        Color pulseColor = Color.FromArgb(
            (int)(12 + 3 * brightness),
            (int)(Math.Min(255, 255 * brightness)),
            (int)(65 + 20 * brightness));

        foreach (var lbl in new[] { lblTF1, lblTF2, lblTS, lblTC })
        {
            if (lbl != null)
                lbl.ForeColor = pulseColor;
        }

        // 脉冲状态指示点
        if (_statusDotPanel != null)
        {
            _statusPulsePhase += 0.08f;
            _statusDotPanel.Invalidate();
        }
    }

    private void StatusDot_Paint(object? sender, PaintEventArgs e)
    {
        if (_statusDotPanel?.Tag is Color dotColor)
        {
            float alpha = 1.0f;
            if (_isLedPulsing)
                alpha = 0.7f + 0.3f * (float)Math.Sin(_statusPulsePhase);
            ThemeColors.PaintStatusDot(e.Graphics, 2, 2, 12, dotColor, alpha);
        }
    }

    private void BuildQueryTab()
    {
        tabQuery.BackColor = ThemeColors.Surface;
        var panel = ThemeColors.CreateCardPanel(6, 6, 1540, 866);

        var lblFrom = new Label { Text = "开始日期：", Location = new Point(18, 16), Size = new Size(75, 28), ForeColor = ThemeColors.TextSecondary, Font = ThemeColors.DefaultFont, TextAlign = ContentAlignment.MiddleRight };
        dtpFrom = new DateTimePicker { Location = new Point(98, 16), Size = new Size(130, 28), Value = DateTime.Now.AddMonths(-1), Format = DateTimePickerFormat.Short, Font = ThemeColors.DefaultFont };
        var lblTo = new Label { Text = "结束日期：", Location = new Point(240, 16), Size = new Size(75, 28), ForeColor = ThemeColors.TextSecondary, Font = ThemeColors.DefaultFont, TextAlign = ContentAlignment.MiddleRight };
        dtpTo = new DateTimePicker { Location = new Point(320, 16), Size = new Size(130, 28), Value = DateTime.Now, Format = DateTimePickerFormat.Short, Font = ThemeColors.DefaultFont };
        var lblPid = new Label { Text = "样品编号：", Location = new Point(465, 16), Size = new Size(75, 28), ForeColor = ThemeColors.TextSecondary, Font = ThemeColors.DefaultFont, TextAlign = ContentAlignment.MiddleRight };
        txtQueryProductId = new TextBox { Location = new Point(545, 16), Size = new Size(145, 28), Font = ThemeColors.DefaultFont, BorderStyle = BorderStyle.FixedSingle };

        btnQuery = ThemeColors.StyleButton(new Button { Text = "查询", Location = new Point(705, 14), Size = new Size(95, 32) }, ThemeColors.BtnPrimary);
        btnQuery.Click += BtnQuery_Click;
        btnExportQuery = ThemeColors.StyleButton(new Button { Text = "导出Excel", Location = new Point(810, 14), Size = new Size(105, 32) }, ThemeColors.BtnSuccess);
        btnExportQuery.Click += BtnExportQuery_Click;

        var sep = ThemeColors.CreateSeparator(12, 54, 1516);

        dgvRecords = new DataGridView { Location = new Point(12, 62), Size = new Size(1516, 790) };
        ThemeColors.StyleDataGridView(dgvRecords);
        dgvRecords.DoubleClick += DgvRecords_DoubleClick;

        panel.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, lblPid, txtQueryProductId,
            btnQuery, btnExportQuery, sep, dgvRecords });
        tabQuery.Controls.Add(panel);
    }

    private void BuildCalibrationTab()
    {
        tabCalibration.BackColor = ThemeColors.Surface;
        var panel = ThemeColors.CreateCardPanel(6, 6, 1540, 866);

        var lblCalTitle = new Label { Text = "校准温度：", Location = new Point(18, 16), Size = new Size(80, 28), ForeColor = ThemeColors.TextSecondary, Font = ThemeColors.DefaultFont, TextAlign = ContentAlignment.MiddleRight };
        lblCalTemp = new Label { Text = "0.0 °C", Location = new Point(105, 16), Size = new Size(120, 28), Font = ThemeColors.MonoFont, ForeColor = ThemeColors.BrandPrimary, BackColor = Color.FromArgb(248, 250, 252), TextAlign = ContentAlignment.MiddleCenter, BorderStyle = BorderStyle.FixedSingle };

        btnRecordCal = ThemeColors.StyleButton(new Button { Text = "记录校准点", Location = new Point(250, 14), Size = new Size(110, 32) }, ThemeColors.BtnSuccess);
        btnRecordCal.Click += BtnRecordCal_Click;
        btnSaveCal = ThemeColors.StyleButton(new Button { Text = "保存校准记录", Location = new Point(370, 14), Size = new Size(115, 32) }, ThemeColors.BtnPrimary);
        btnSaveCal.Click += BtnSaveCal_Click;
        btnQueryCal = ThemeColors.StyleButton(new Button { Text = "查询历史", Location = new Point(500, 14), Size = new Size(95, 32) }, ThemeColors.BtnPurple);
        btnQueryCal.Click += BtnQueryCal_Click;

        var sep = ThemeColors.CreateSeparator(12, 54, 1516);

        dgvCalibrations = new DataGridView { Location = new Point(12, 62), Size = new Size(1396, 750) };
        ThemeColors.StyleDataGridView(dgvCalibrations);

        panel.Controls.AddRange(new Control[] { lblCalTitle, lblCalTemp, btnRecordCal, btnSaveCal, btnQueryCal, sep, dgvCalibrations });
        tabCalibration.Controls.Add(panel);
    }

    private void BuildSettingsTab()
    {
        tabSettings.BackColor = ThemeColors.Surface;
        var panel = ThemeColors.CreateCardPanel(6, 6, 1540, 866);

        var lblConfig = new Label
        {
            Text = "参数设置",
            Location = new Point(18, 14),
            Size = new Size(300, 32),
            Font = ThemeColors.SubtitleFont,
            ForeColor = ThemeColors.BrandPrimary
        };
        var lblHint = new Label
        {
            Text = "修改 appsettings.json 后重启应用生效",
            Location = new Point(18, 42),
            Size = new Size(300, 18),
            Font = ThemeColors.SmallLabelFont,
            ForeColor = ThemeColors.TextSecondary
        };
        panel.Controls.Add(lblConfig);
        panel.Controls.Add(lblHint);

        int y = 72;
        var config = GlobalContext.Instance.Simulation;

        AddSettingsGroup(panel, "仿真参数", ref y, new[]
        {
            ("仿真模式", $"{config.EnableSimulation}"),
            ("初始炉温", $"{config.InitialFurnaceTemp} °C"),
            ("目标炉温", $"{config.TargetFurnaceTemp} °C"),
            ("升温速度", $"{config.HeatingRatePerSecond} °C/s"),
            ("温度波动", $"±{config.TempFluctuation} °C"),
            ("稳定阈值", $"{config.StableThreshold} °C"),
        });

        y += 14;

        AddSettingsGroup(panel, "存储路径", ref y, new[]
        {
            ("数据存储目录", GlobalContext.Instance.TestDataDirectory),
            ("报告输出目录", GlobalContext.Instance.ReportDirectory),
        });
    }

    private void AddSettingsGroup(Panel parent, string groupTitle, ref int y, (string label, string value)[] items)
    {
        var groupLabel = new Label
        {
            Text = $"▸ {groupTitle}",
            Location = new Point(15, y),
            Size = new Size(300, 24),
            Font = ThemeColors.GroupTitleFont,
            ForeColor = ThemeColors.BrandPrimary
        };
        parent.Controls.Add(groupLabel);
        y += 26;

        foreach (var (label, value) in items)
        {
            var keyLabel = new Label
            {
                Text = label,
                Location = new Point(30, y),
                Size = new Size(180, 22),
                Font = ThemeColors.DefaultFont,
                ForeColor = ThemeColors.TextSecondary,
                TextAlign = ContentAlignment.MiddleRight
            };
            var valLabel = new Label
            {
                Text = value,
                Location = new Point(218, y),
                Size = new Size(500, 22),
                Font = ThemeColors.MonoFont,
                ForeColor = ThemeColors.TextPrimary
            };
            parent.Controls.Add(keyLabel);
            parent.Controls.Add(valLabel);
            y += 26;
        }
    }

    #endregion

    #region 按钮事件

    private void BtnNewTest_Click(object? sender, EventArgs e)
    {
        // 检查是否有未保存的试验
        if (_db.HasUnfinishedTest())
        {
            MessageBox.Show("存在已完成但未保存的试验记录，请先保存试验记录后再新建试验。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new NewTestForm();
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _testMaster.SetTestInfo(dlg.ProductId, dlg.TestId, dlg.PreWeight,
                dlg.AmbTemp, dlg.AmbHumi, GlobalContext.Instance.CurrentOperator!,
                dlg.IsStandard, dlg.TargetSeconds);
            lblProductId.Text = dlg.ProductId;
            _testMaster.AddSystemMessage($"新建试验：{dlg.ProductId} / {dlg.TestId}");
            _testMaster.TriggerBroadcast();
            UpdateButtonStates();
        }
    }

    private void BtnStartHeat_Click(object? sender, EventArgs e)
    {
        if (_testMaster.State == TestState.Idle || _testMaster.State == TestState.Preparing)
        {
            if (_db.HasUnfinishedTest())
            {
                MessageBox.Show("存在已完成但未保存的试验记录，请先保存试验记录。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // 清空曲线数据，开始新的升温曲线
            _plotDataCount = 0;
            _plotTime.Clear(); _plotTF1.Clear(); _plotTF2.Clear(); _plotTS.Clear(); _plotTC.Clear();
            seriesTF1.Points.Clear(); seriesTF2.Points.Clear(); seriesTS.Points.Clear(); seriesTC.Points.Clear();
            foreach (var s in plotModel.Series.Take(4).Cast<AreaSeries>())
                s.Points.Clear();
            plotModel.InvalidatePlot(true);

            _testMaster.StartHeating();
            UpdateButtonStates();
        }
    }

    private void BtnStopHeat_Click(object? sender, EventArgs e)
    {
        _testMaster.StopHeating();
        UpdateButtonStates();
    }

    private void BtnStartRecord_Click(object? sender, EventArgs e)
    {
        _testMaster.StartRecording();
        // 清空曲线数据
        _plotDataCount = 0;
        _plotTime.Clear(); _plotTF1.Clear(); _plotTF2.Clear(); _plotTS.Clear(); _plotTC.Clear();
        seriesTF1.Points.Clear(); seriesTF2.Points.Clear(); seriesTS.Points.Clear(); seriesTC.Points.Clear();
        // 同时清空面积填充系列
        foreach (var s in plotModel.Series.Take(4).Cast<AreaSeries>())
            s.Points.Clear();
        plotModel.InvalidatePlot(true);
        UpdateButtonStates();
    }

    private void BtnStopRecord_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show("确定要手动停止记录吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.Yes)
        {
            _testMaster.StopRecording();
            if (_testMaster.State == TestState.Complete)
            {
                // 自动生成 CSV
                var csvPath = _csvExport.Export(
                    _testMaster.CurrentProductId!,
                    _testMaster.CurrentTestId!,
                    _testMaster.RecordedData);
                _testMaster.AddSystemMessage($"CSV 已生成：{csvPath}");
                // 自动生成 Excel
                AutoExportExcel();
            }
            _testMaster.TriggerBroadcast();
            UpdateButtonStates();
        }
    }

    private void BtnTestRecord_Click(object? sender, EventArgs e)
    {
        if (_testMaster.State != TestState.Complete)
        {
            MessageBox.Show("请等待试验完成后再填写试验记录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new TestRecordForm(_testMaster.PreWeight);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            SaveTestResult(dlg);
        }
    }

    private void SaveTestResult(TestRecordForm dlg)
    {
        try
        {
            var productId = _testMaster.CurrentProductId;
            var testId = _testMaster.CurrentTestId;
            if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(testId))
            {
                MessageBox.Show("试验信息不完整，无法保存。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var preWeight = _testMaster.PreWeight;
            var postWeight = dlg.PostWeight;
            var lostWeight = preWeight - postWeight;
            var lostPer = preWeight > 0 ? lostWeight / preWeight * 100 : 0;

            var starts = _testMaster.GetStartTemps();
            var finals = _testMaster.GetFinalTemps();

            var deltaTf1 = finals[0] - starts[0];
            var deltaTf2 = finals[1] - starts[1];
            var deltaTs = finals[2] - starts[2];
            var deltaTc = finals[3] - starts[3];
            var deltaTf = deltaTs;

            var totalTime = _testMaster.ElapsedSeconds;
            var phenoCode = dlg.HasFlame ? $"火焰:{dlg.FlameTime}s,持续:{dlg.FlameDuration}s" : "";

            _db.UpdateTestResult(
                productId, testId,
                preWeight, postWeight, lostPer, deltaTf, deltaTs, totalTime, phenoCode,
                _testMaster.MaxTf1, _testMaster.MaxTf2, _testMaster.MaxTs, _testMaster.MaxTc,
                0, 0, 0, 0,
                finals[0], finals[1], finals[2], finals[3],
                totalTime, totalTime, totalTime, totalTime,
                deltaTf1, deltaTf2, deltaTc,
                dlg.FlameTime, dlg.FlameDuration);

            // 导出 PDF（包裹 try 防止导出异常导致保存失败）
            try
            {
                var test = _db.GetTest(productId, testId);
                if (test != null && _testMaster.RecordedData.Count > 0)
                {
                    var pdfPath = _pdfExport.Export(test, _testMaster.RecordedData, GlobalContext.Instance.ReportDirectory);
                    _testMaster.AddSystemMessage($"PDF 报告已生成：{pdfPath}");
                }
            }
            catch (Exception ex)
            {
                _testMaster.AddSystemMessage($"PDF 导出失败：{ex.Message}");
            }

            _testMaster.MarkSaved();
            UpdateButtonStates();
            MessageBox.Show("试验记录已保存。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AutoExportExcel()
    {
        try
        {
            if (_testMaster.RecordedData.Count == 0) return;
            var test = new TestEntity
            {
                ProductId = _testMaster.CurrentProductId ?? "",
                TestId = _testMaster.CurrentTestId ?? "",
                TestDate = DateTime.Now.ToString("yyyy-MM-dd"),
                AmbTemp = _testMaster.AmbTemp,
                AmbHumi = _testMaster.AmbHumi,
                Operator = _testMaster.CurrentOperator ?? "",
                PreWeight = _testMaster.PreWeight,
                TotalTestTime = _testMaster.ElapsedSeconds,
                DeltaTf1 = _testMaster.GetFinalTemps()[0] - _testMaster.GetStartTemps()[0],
                DeltaTf2 = _testMaster.GetFinalTemps()[1] - _testMaster.GetStartTemps()[1],
                DeltaTs = _testMaster.GetFinalTemps()[2] - _testMaster.GetStartTemps()[2],
                DeltaTc = _testMaster.GetFinalTemps()[3] - _testMaster.GetStartTemps()[3],
                DeltaTf = _testMaster.GetFinalTemps()[2] - _testMaster.GetStartTemps()[2],
            };
            var xlsxPath = _excelExport.Export(test, _testMaster.RecordedData, GlobalContext.Instance.ReportDirectory);
            _testMaster.AddSystemMessage($"Excel 报告已生成：{xlsxPath}");
        }
        catch (Exception ex)
        {
            _testMaster.AddSystemMessage($"Excel 导出失败：{ex.Message}");
        }
    }

    private void BtnQuery_Click(object? sender, EventArgs e)
    {
        RefreshQueryResults();
    }

    private void BtnExportQuery_Click(object? sender, EventArgs e)
    {
        if (dgvRecords.Rows.Count == 0)
        {
            MessageBox.Show("没有可导出的记录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        // 简单导出为 CSV
        using var sfd = new SaveFileDialog { Filter = "CSV 文件|*.csv", FileName = $"查询导出_{DateTime.Now:yyyyMMdd}.csv" };
        if (sfd.ShowDialog() == DialogResult.OK)
        {
            using var writer = new StreamWriter(sfd.FileName);
            // 表头
            var headers = new[] { "试验ID", "样品编号", "试验日期", "操作员", "失重率(%)", "温升(°C)", "时长(秒)" };
            writer.WriteLine(string.Join(",", headers));
            foreach (DataGridViewRow row in dgvRecords.Rows)
            {
                if (row.IsNewRow) continue;
                var vals = new List<string>();
                for (int i = 0; i < row.Cells.Count && i < headers.Length; i++)
                    vals.Add(row.Cells[i].Value?.ToString() ?? "");
                writer.WriteLine(string.Join(",", vals));
            }
            MessageBox.Show($"导出完成：{sfd.FileName}", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void DgvRecords_DoubleClick(object? sender, EventArgs e)
    {
        if (dgvRecords.SelectedRows.Count == 0) return;
        var row = dgvRecords.SelectedRows[0];
        var testId = row.Cells[0].Value?.ToString() ?? "";
        var productId = row.Cells[1].Value?.ToString() ?? "";

        var test = _db.GetTest(productId, testId);
        if (test == null) return;

        var msg = $"试验ID: {test.TestId}\n" +
                  $"样品编号: {test.ProductId}\n" +
                  $"试验日期: {test.TestDate}\n" +
                  $"操作员: {test.Operator}\n" +
                  $"环境温度: {test.AmbTemp:F1} °C\n" +
                  $"环境湿度: {test.AmbHumi:F1} %\n" +
                  $"试验前质量: {test.PreWeight:F1} g\n" +
                  $"试验后质量: {test.PostWeight:F1} g\n" +
                  $"失重率: {test.LostWeightPer:F1} %\n" +
                  $"综合温升: {test.DeltaTf:F1} °C\n" +
                  $"火焰时间: {test.FlameTime} 秒\n" +
                  $"火焰持续: {test.FlameDuration} 秒\n" +
                  $"总时长: {test.TotalTestTime} 秒";
        MessageBox.Show(msg, "试验详情", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void RefreshQueryResults()
    {
        var tests = _db.QueryTests(dtpFrom.Value, dtpTo.Value, txtQueryProductId.Text);
        dgvRecords.DataSource = null;
        dgvRecords.DataSource = tests.Select(t => new
        {
            试验ID = t.TestId,
            样品编号 = t.ProductId,
            试验日期 = t.TestDate,
            操作员 = t.Operator,
            失重率 = $"{t.LostWeightPer:F1}%",
            温升 = $"{t.DeltaTf:F1}°C",
            时长秒 = t.TotalTestTime,
        }).ToList();
    }

    // 校准相关
    private double _calTempReading = 0;

    private void BtnRecordCal_Click(object? sender, EventArgs e)
    {
        _calTempReading = _testMaster.Temperatures[4]; // TCal
        lblCalTemp.Text = $"{_calTempReading:F1} °C";
        MessageBox.Show($"已记录校准温度：{_calTempReading:F1} °C", "记录", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnSaveCal_Click(object? sender, EventArgs e)
    {
        if (_calTempReading == 0)
        {
            MessageBox.Show("请先点击「记录校准点」。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var cr = new CalibrationRecord
        {
            Id = Guid.NewGuid().ToString(),
            CalibrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            CalibrationType = "Surface",
            ApparatusId = 0,
            Operator = GlobalContext.Instance.CurrentOperator ?? "",
            TemperatureData = System.Text.Json.JsonSerializer.Serialize(new[] { _calTempReading }),
            PassedCriteria = 1,
            Remarks = "",
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        };
        _db.InsertCalibration(cr);
        MessageBox.Show("校准记录已保存。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        RefreshCalibrations();
    }

    private void BtnQueryCal_Click(object? sender, EventArgs e)
    {
        RefreshCalibrations();
    }

    private void RefreshCalibrations()
    {
        var cals = _db.QueryCalibrations(DateTime.Now.AddYears(-1), DateTime.Now);
        dgvCalibrations.DataSource = null;
        dgvCalibrations.DataSource = cals.Select(c => new
        {
            c.Id,
            c.CalibrationDate,
            c.CalibrationType,
            c.Operator,
            c.PassedCriteria
        }).ToList();
    }

    #endregion

    #region 按钮状态控制

    private void UpdateButtonStates()
    {
        var state = _testMaster.State;
        bool hasActive = _testMaster.CurrentProductId != null;
        bool hasUnfinished = _db.HasUnfinishedTest();

        btnNewTest.Enabled = state switch
        {
            TestState.Idle => true,
            TestState.Preparing => !hasActive || hasUnfinished ? false : true,
            TestState.Ready => false,
            TestState.Recording => false,
            TestState.Complete => hasUnfinished ? false : true,
            _ => false
        };
        // 简化：如果有未保存试验，禁止新建
        if (hasUnfinished) btnNewTest.Enabled = false;

        btnStartHeat.Enabled = (state == TestState.Idle || state == TestState.Preparing) && !hasUnfinished;
        btnStopHeat.Enabled = state == TestState.Preparing || state == TestState.Ready || state == TestState.Complete;
        btnStartRecord.Enabled = state == TestState.Ready && !hasUnfinished;
        btnStopRecord.Enabled = state == TestState.Recording;
        btnTestRecord.Enabled = state == TestState.Complete;
    }

    #endregion

    #region 数据广播

    private void OnDataBroadcast(object? sender, DataBroadcastEventArgs e)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(() => OnDataBroadcast(sender, e));
            return;
        }

        // 更新温度显示
        var temps = e.Temperatures;
        lblTF1.Text = $"{temps[0]:F1} °C";
        lblTF2.Text = $"{temps[1]:F1} °C";
        lblTS.Text = $"{temps[2]:F1} °C";
        lblTC.Text = $"{temps[3]:F1} °C";
        lblTCal.Text = $"{temps[4]:F1} °C";

        // 更新状态
        lblState.Text = e.State switch
        {
            TestState.Idle => "空闲",
            TestState.Preparing => "升温中",
            TestState.Ready => "就绪",
            TestState.Recording => "记录中",
            TestState.Complete => "完成",
            _ => "未知"
        };

        // 状态颜色
        lblState.ForeColor = e.State switch
        {
            TestState.Idle => ThemeColors.StateIdle,
            TestState.Preparing => ThemeColors.StatePreparing,
            TestState.Ready => ThemeColors.StateReady,
            TestState.Recording => ThemeColors.StateRecording,
            TestState.Complete => ThemeColors.StateComplete,
            _ => ThemeColors.TextPrimary
        };

        // 更新计时器
        lblTimer.Text = $"{e.ElapsedSeconds} 秒";

        // 更新温漂
        lblDrift.Text = $"{e.TemperatureDrift:F2} °C/10min";

        // 更新产品编号
        if (!string.IsNullOrEmpty(e.ProductId))
            lblProductId.Text = e.ProductId;

        // 更新曲线图 — 升温/就绪/记录阶段都绘制，记录阶段忽略 ElapsedSeconds=0 的初始广播
        bool shouldPlot = e.State == TestState.Preparing ||
                          e.State == TestState.Ready ||
                          (e.State == TestState.Recording && e.ElapsedSeconds > 0);
        if (shouldPlot)
        {
            _plotDataCount++;
            _plotTime.Add(_plotDataCount);
            _plotTF1.Add(temps[0]);
            _plotTF2.Add(temps[1]);
            _plotTS.Add(temps[2]);
            _plotTC.Add(temps[3]);

            // 只保留最近 600 个点（10 分钟）
            while (_plotTime.Count > 600)
            {
                _plotTime.RemoveAt(0);
                _plotTF1.RemoveAt(0);
                _plotTF2.RemoveAt(0);
                _plotTS.RemoveAt(0);
                _plotTC.RemoveAt(0);
            }

            // 更新折线 + 面积填充
            var allSeries = plotModel.Series;
            var areaSeries = allSeries.Take(4).Cast<AreaSeries>().ToArray();
            var lineSeries = new[] { seriesTF1, seriesTF2, seriesTS, seriesTC };
            var dataArrays = new[] { _plotTF1, _plotTF2, _plotTS, _plotTC };

            for (int s = 0; s < 4; s++)
            {
                areaSeries[s].Points.Clear();
                lineSeries[s].Points.Clear();
                for (int i = 0; i < _plotTime.Count; i++)
                {
                    double t = _plotTime[i];
                    double v = dataArrays[s][i];
                    areaSeries[s].Points.Add(new DataPoint(t, v));
                    lineSeries[s].Points.Add(new DataPoint(t, v));
                }
            }

            plotModel.InvalidatePlot(true);
        }

        // 控制 LED 脉冲动画
        if (e.State == TestState.Recording && !_isLedPulsing)
        {
            _isLedPulsing = true;
            _ledPulseTimer?.Start();
        }
        else if (e.State != TestState.Recording && _isLedPulsing)
        {
            _isLedPulsing = false;
            _ledPulseTimer?.Stop();
            // 恢复默认 LED 颜色
            foreach (var lbl in new[] { lblTF1, lblTF2, lblTS, lblTC })
            {
                if (lbl != null) lbl.ForeColor = ThemeColors.LedText;
            }
        }

        // 更新升温进度条
        if (_heatProgressBar != null && _heatProgressLabel != null)
        {
            float progress = (float)Math.Min(1.0, temps[0] / 747.0);
            int barW = (int)(420 * progress);
            _heatProgressBar.Width = Math.Min(420, barW);

            string progressText = e.State switch
            {
                TestState.Idle => "未开始升温",
                TestState.Preparing => $"炉温 {temps[0]:F0}°C / 747°C — 升温中...",
                TestState.Ready => $"✓ 炉温已稳定 ({temps[0]:F0}°C) — 可以开始记录",
                TestState.Recording => $"● 记录中 — 已记录 {e.ElapsedSeconds} 秒",
                TestState.Complete => "✓ 试验完成 — 请保存试验记录",
                _ => ""
            };
            _heatProgressLabel.Text = progressText;
            _heatProgressBar.BackColor = e.State switch
            {
                TestState.Preparing => ThemeColors.StatePreparing,
                TestState.Ready => ThemeColors.StateReady,
                TestState.Recording => ThemeColors.StateRecording,
                TestState.Complete => ThemeColors.StateComplete,
                _ => ThemeColors.ProgressFill
            };
        }

        // 更新状态指示点颜色
        if (_statusDotPanel != null)
        {
            var dotColor = e.State switch
            {
                TestState.Idle => ThemeColors.StateIdle,
                TestState.Preparing => ThemeColors.StatePreparing,
                TestState.Ready => ThemeColors.StateReady,
                TestState.Recording => ThemeColors.StateRecording,
                TestState.Complete => ThemeColors.StateComplete,
                _ => ThemeColors.TextPrimary
            };
            _statusDotPanel.Tag = dotColor;
            _statusDotPanel.Invalidate();
            // 更新 Paint 事件
            _statusDotPanel.Paint -= StatusDot_Paint;
            _statusDotPanel.Paint += StatusDot_Paint;
        }

        // 更新消息日志
        foreach (var msg in e.Messages)
        {
            Color color = msg.Message switch
            {
                string s when s.Contains("终止") || s.Contains("满足") => ThemeColors.LogTextWarn,
                string s when s.Contains("失败") || s.Contains("错误") => ThemeColors.LogTextError,
                _ => ThemeColors.LogTextNormal
            };
            rtbLog.SelectionColor = color;
            rtbLog.AppendText($"{msg.Time}  {msg.Message}\n");
            rtbLog.ScrollToCaret();
        }

        // 更新校准温度显示
        if (tabCalibration.Controls.Count > 0)
            lblCalTemp.Text = $"{temps[4]:F1} °C";
    }

    #endregion

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _testMaster.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _testMaster.Dispose();
        base.Dispose(disposing);
    }
}
