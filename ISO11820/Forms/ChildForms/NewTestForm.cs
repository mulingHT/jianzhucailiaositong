using ISO11820.Core;
using ISO11820.Data;
using ISO11820.Helpers;

namespace ISO11820.Forms;

/// <summary>
/// 新建试验窗体 — 严格按开发文档 2.2 节实现
/// </summary>
public partial class NewTestForm : Form
{
    private TextBox txtTestId = null!, txtProductId = null!, txtProductName = null!, txtSpecific = null!;
    private TextBox txtDiameter = null!, txtHeight = null!;
    private TextBox txtAmbTemp = null!, txtAmbHumi = null!;
    private TextBox txtPreWeight = null!;
    private RadioButton rbStandard = null!, rbCustom = null!;
    private NumericUpDown nudCustomMinutes = null!;
    private Label lblApparatusInfo = null!;

    public string ProductId => txtProductId.Text.Trim();
    public string TestId => txtTestId.Text.Trim();
    public double PreWeight => double.TryParse(txtPreWeight.Text, out var v) ? v : 0;
    public double AmbTemp => double.TryParse(txtAmbTemp.Text, out var v) ? v : 25;
    public double AmbHumi => double.TryParse(txtAmbHumi.Text, out var v) ? v : 50;
    public bool IsStandard => rbStandard.Checked;
    public int TargetSeconds => (int)(nudCustomMinutes.Value * 60);

    public NewTestForm()
    {
        InitializeComponent();
        LoadApparatusInfo();
        this.Load += (s, e) => Helpers.ThemeColors.ApplyFadeIn(this, 12, 0.10);
    }

    private void InitializeComponent()
    {
        this.Text = "新建试验";
        this.Size = new Size(620, 620);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = ThemeColors.Surface;

        // 顶部蓝色品牌条
        var brandBar = new Panel { Location = new Point(0, 0), Size = new Size(620, 4), BackColor = ThemeColors.BrandPrimary };
        this.Controls.Add(brandBar);

        // 标题
        var headerLabel = new Label
        {
            Text = "新建试验配置",
            Font = ThemeColors.TitleFont,
            ForeColor = ThemeColors.BrandPrimary,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(18, 12),
            Size = new Size(580, 32)
        };
        this.Controls.Add(headerLabel);

        int y = 55;
        var leftLabel = 12;
        var leftInput = 145;
        var labelWidth = 125;
        var inputWidth = 260;

        // 卡片背景面板
        var cardPanel = new Panel
        {
            Location = new Point(12, y - 5),
            Size = new Size(580, 440),
            BackColor = ThemeColors.CardBackground,
            BorderStyle = BorderStyle.None
        };
        cardPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(ThemeColors.CardBorder);
            e.Graphics.DrawRectangle(pen, 0, 0, cardPanel.Width - 1, cardPanel.Height - 1);
        };
        this.Controls.Add(cardPanel);

        int cy = 12; // y within cardPanel

        // 试验ID
        AddStyledLabel(cardPanel, "试验ID：", leftLabel, cy + 3, labelWidth);
        txtTestId = new TextBox { Location = new Point(leftInput, cy), Size = new Size(inputWidth, 25), Text = DateTime.Now.ToString("yyyyMMdd-HHmmss"), Font = ThemeColors.DefaultFont, BorderStyle = BorderStyle.FixedSingle };
        cardPanel.Controls.Add(txtTestId);
        cy += 32;

        // 样品编号
        AddStyledLabel(cardPanel, "样品编号：", leftLabel, cy + 3, labelWidth);
        txtProductId = new TextBox { Location = new Point(leftInput, cy), Size = new Size(inputWidth, 25), Text = DateTime.Now.ToString("yyyyMMdd-HHmm"), Font = ThemeColors.DefaultFont, BorderStyle = BorderStyle.FixedSingle };
        cardPanel.Controls.Add(txtProductId);
        cy += 32;

        // 样品名称
        AddStyledLabel(cardPanel, "样品名称：", leftLabel, cy + 3, labelWidth);
        txtProductName = new TextBox { Location = new Point(leftInput, cy), Size = new Size(inputWidth, 25), Text = "岩棉隔热板", Font = ThemeColors.DefaultFont, BorderStyle = BorderStyle.FixedSingle };
        cardPanel.Controls.Add(txtProductName);
        cy += 32;

        // 规格
        AddStyledLabel(cardPanel, "规格型号：", leftLabel, cy + 3, labelWidth);
        txtSpecific = new TextBox { Location = new Point(leftInput, cy), Size = new Size(inputWidth, 25), Text = "100×50×25mm", Font = ThemeColors.DefaultFont, BorderStyle = BorderStyle.FixedSingle };
        cardPanel.Controls.Add(txtSpecific);
        cy += 32;

        // 直径 + 高度
        AddStyledLabel(cardPanel, "直径(mm)：", leftLabel, cy + 3, 75);
        txtDiameter = new TextBox { Location = new Point(leftInput, cy), Size = new Size(75, 25), Text = "100", Font = ThemeColors.DefaultFont, BorderStyle = BorderStyle.FixedSingle };
        AddStyledLabel(cardPanel, "高度(mm)：", leftInput + 85, cy + 3, 70);
        txtHeight = new TextBox { Location = new Point(leftInput + 160, cy), Size = new Size(75, 25), Text = "50", Font = ThemeColors.DefaultFont, BorderStyle = BorderStyle.FixedSingle };
        cardPanel.Controls.Add(txtDiameter);
        cardPanel.Controls.Add(txtHeight);
        cy += 32;

        // 环境温度 + 湿度
        AddStyledLabel(cardPanel, "环境温度(°C)：", leftLabel, cy + 3, labelWidth);
        txtAmbTemp = new TextBox { Location = new Point(leftInput, cy), Size = new Size(75, 25), Text = "25.0", Font = ThemeColors.DefaultFont, BorderStyle = BorderStyle.FixedSingle };
        AddStyledLabel(cardPanel, "湿度(%)：", leftInput + 85, cy + 3, 55);
        txtAmbHumi = new TextBox { Location = new Point(leftInput + 145, cy), Size = new Size(75, 25), Text = "50.0", Font = ThemeColors.DefaultFont, BorderStyle = BorderStyle.FixedSingle };
        cardPanel.Controls.Add(txtAmbTemp);
        cardPanel.Controls.Add(txtAmbHumi);
        cy += 32;

        // 试验前质量
        AddStyledLabel(cardPanel, "试验前质量(g)：", leftLabel, cy + 3, labelWidth);
        txtPreWeight = new TextBox { Location = new Point(leftInput, cy), Size = new Size(inputWidth, 25), Text = "250.0", Font = ThemeColors.DefaultFont, BorderStyle = BorderStyle.FixedSingle };
        cardPanel.Controls.Add(txtPreWeight);
        cy += 32;

        // 试验时长模式
        AddStyledLabel(cardPanel, "试验时长模式：", leftLabel, cy + 3, labelWidth);
        rbStandard = new RadioButton { Text = "标准 60 分钟", Location = new Point(leftInput, cy), Size = new Size(115, 25), Checked = true, Font = ThemeColors.DefaultFont };
        rbStandard.CheckedChanged += (s, e) => nudCustomMinutes.Enabled = !rbStandard.Checked;
        rbCustom = new RadioButton { Text = "自定义(分钟)：", Location = new Point(leftInput + 120, cy), Size = new Size(110, 25), Font = ThemeColors.DefaultFont };
        nudCustomMinutes = new NumericUpDown { Location = new Point(leftInput + 235, cy), Size = new Size(60, 25), Minimum = 1, Maximum = 180, Value = 5, Enabled = false, Font = ThemeColors.DefaultFont, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        cardPanel.Controls.Add(rbStandard);
        cardPanel.Controls.Add(rbCustom);
        cardPanel.Controls.Add(nudCustomMinutes);
        cy += 32;

        // 设备信息
        AddStyledLabel(cardPanel, "设备信息：", leftLabel, cy + 3, labelWidth);
        lblApparatusInfo = new Label { Location = new Point(leftInput, cy + 3), Size = new Size(330, 22), Text = "加载中...", Font = ThemeColors.DefaultFont, ForeColor = ThemeColors.TextSecondary };
        cardPanel.Controls.Add(lblApparatusInfo);
        cy += 30;

        // 分隔线
        var sep = new Panel { Location = new Point(leftLabel + 5, cy), Size = new Size(400, 1), BackColor = ThemeColors.CardBorder, BorderStyle = BorderStyle.None };
        cardPanel.Controls.Add(sep);
        cy += 12;

        // 操作员
        AddStyledLabel(cardPanel, "操作员：", leftLabel, cy + 3, labelWidth);
        var lblOperator = new Label { Location = new Point(leftInput, cy + 3), Size = new Size(inputWidth, 22), Text = GlobalContext.Instance.CurrentOperator ?? "-", Font = ThemeColors.MonoFont, ForeColor = ThemeColors.BrandPrimary };
        cardPanel.Controls.Add(lblOperator);

        // 按钮（在卡片外部）
        var btnY = y + 445;
        var btnCancel = ThemeColors.StyleCancelButton(new Button { Text = "取消", Location = new Point(400, btnY), Size = new Size(90, 38) });
        btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        var btnCreate = ThemeColors.StyleButton(new Button { Text = "创建试验", Location = new Point(500, btnY), Size = new Size(100, 38) }, ThemeColors.BtnSuccess);
        btnCreate.Click += BtnCreate_Click;

        this.Controls.Add(btnCancel);
        this.Controls.Add(btnCreate);

        this.AcceptButton = btnCreate;
        this.CancelButton = btnCancel;
    }

    private void AddLabel(string text, int x, int y, int width)
    {
        this.Controls.Add(new Label { Text = text, Location = new Point(x, y), Size = new Size(width, 20), TextAlign = ContentAlignment.MiddleRight, Font = ThemeColors.DefaultFont, ForeColor = ThemeColors.TextSecondary });
    }

    private void AddStyledLabel(Panel parent, string text, int x, int y, int width)
    {
        parent.Controls.Add(new Label { Text = text, Location = new Point(x, y), Size = new Size(width, 22), TextAlign = ContentAlignment.MiddleRight, Font = ThemeColors.DefaultFont, ForeColor = ThemeColors.TextSecondary });
    }

    private void LoadApparatusInfo()
    {
        var apparatus = GlobalContext.Instance.Db.GetApparatus();
        if (apparatus != null)
            lblApparatusInfo.Text = $"{apparatus.ApparatusName} ({apparatus.InnerNumber}) | 检定至 {apparatus.CheckDateT} | 恒功率 {apparatus.ConstPower ?? 2048} W";
        else
            lblApparatusInfo.Text = "一号试验炉 (FURNACE-01)";
    }

    private void BtnCreate_Click(object? sender, EventArgs e)
    {
        // 验证
        if (string.IsNullOrWhiteSpace(txtTestId.Text))
        {
            MessageBox.Show("请输入试验ID。", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtTestId.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(txtProductId.Text))
        {
            MessageBox.Show("请输入样品编号。", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtProductId.Focus();
            return;
        }
        if (!double.TryParse(txtPreWeight.Text, out var pw) || pw <= 0)
        {
            MessageBox.Show("请输入有效的试验前质量。", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPreWeight.Focus();
            return;
        }

        // 保存样品信息
        var product = new Models.ProductMaster
        {
            ProductId = txtProductId.Text.Trim(),
            ProductName = txtProductName.Text.Trim(),
            Specific = txtSpecific.Text.Trim(),
            Diameter = double.TryParse(txtDiameter.Text, out var d) ? d : 100,
            Height = double.TryParse(txtHeight.Text, out var h) ? h : 50,
        };
        GlobalContext.Instance.Db.UpsertProduct(product);

        // 创建试验记录
        var ambTemp = double.TryParse(txtAmbTemp.Text, out var at) ? at : 25;
        var ambHumi = double.TryParse(txtAmbHumi.Text, out var ah) ? ah : 50;
        GlobalContext.Instance.Db.InsertTest(product.ProductId, TestId, GlobalContext.Instance.CurrentOperator!, pw, ambTemp, ambHumi);

        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}
