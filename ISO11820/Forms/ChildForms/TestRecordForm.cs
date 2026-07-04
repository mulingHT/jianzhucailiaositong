using ISO11820.Helpers;

namespace ISO11820.Forms;

/// <summary>
/// 试验现象记录窗体 — 严格按开发文档 2.8 节实现
/// </summary>
public partial class TestRecordForm : Form
{
    private CheckBox chkFlame = null!;
    private NumericUpDown nudFlameTime = null!, nudFlameDuration = null!;
    private NumericUpDown nudPostWeight = null!;
    private TextBox txtMemo = null!;

    public bool HasFlame => chkFlame.Checked;
    public int FlameTime => (int)nudFlameTime.Value;
    public int FlameDuration => (int)nudFlameDuration.Value;
    public double PostWeight => (double)nudPostWeight.Value;
    public string Memo => txtMemo.Text.Trim();

    private readonly double _preWeight;

    public TestRecordForm(double preWeight)
    {
        _preWeight = preWeight;
        InitializeComponent();
        this.Load += (s, e) => Helpers.ThemeColors.ApplyFadeIn(this, 12, 0.10);
    }

    private void InitializeComponent()
    {
        this.Text = "试验现象记录";
        this.Size = new Size(570, 480);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = ThemeColors.Surface;

        // 顶部品牌条
        var brandBar = new Panel { Location = new Point(0, 0), Size = new Size(570, 4), BackColor = ThemeColors.BrandPrimary };
        this.Controls.Add(brandBar);

        // 标题
        var headerLabel = new Label
        {
            Text = "试验现象记录",
            Font = ThemeColors.TitleFont,
            ForeColor = ThemeColors.BrandPrimary,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(18, 12),
            Size = new Size(530, 32)
        };
        this.Controls.Add(headerLabel);

        // 卡片面板
        var cardPanel = new Panel
        {
            Location = new Point(14, 50),
            Size = new Size(528, 330),
            BackColor = ThemeColors.CardBackground,
            BorderStyle = BorderStyle.None
        };
        cardPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(ThemeColors.CardBorder);
            e.Graphics.DrawRectangle(pen, 0, 0, cardPanel.Width - 1, cardPanel.Height - 1);
        };
        this.Controls.Add(cardPanel);

        int y = 18;
        int leftLabel = 18;
        int leftInput = 185;
        int inputWidth = 240;

        // 火焰复选框
        chkFlame = new CheckBox
        {
            Text = "是否出现持续火焰",
            Location = new Point(leftLabel, y),
            Size = new Size(250, 26),
            Font = ThemeColors.SubtitleFont,
            ForeColor = ThemeColors.TextPrimary
        };
        chkFlame.CheckedChanged += (s, e) =>
        {
            nudFlameTime.Enabled = chkFlame.Checked;
            nudFlameDuration.Enabled = chkFlame.Checked;
        };
        cardPanel.Controls.Add(chkFlame);
        y += 36;

        // 火焰发生时刻
        AddStyledLabel(cardPanel, "火焰发生时刻(秒)：", leftLabel, y + 3);
        nudFlameTime = new NumericUpDown { Location = new Point(leftInput, y), Size = new Size(inputWidth, 26), Minimum = 0, Maximum = 9999, Enabled = false, Font = ThemeColors.DefaultFont, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        cardPanel.Controls.Add(nudFlameTime);
        y += 34;

        // 火焰持续时间
        AddStyledLabel(cardPanel, "火焰持续时间(秒)：", leftLabel, y + 3);
        nudFlameDuration = new NumericUpDown { Location = new Point(leftInput, y), Size = new Size(inputWidth, 26), Minimum = 0, Maximum = 9999, Enabled = false, Font = ThemeColors.DefaultFont, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        cardPanel.Controls.Add(nudFlameDuration);
        y += 40;

        // 分隔线
        var sep = new Panel { Location = new Point(leftLabel, y), Size = new Size(495, 1), BackColor = ThemeColors.CardBorder, BorderStyle = BorderStyle.None };
        cardPanel.Controls.Add(sep);
        y += 18;

        // 试验后质量
        AddStyledLabel(cardPanel, "试验后质量(g) *必填：", leftLabel, y + 3);
        nudPostWeight = new NumericUpDown
        {
            Location = new Point(leftInput, y),
            Size = new Size(inputWidth, 26),
            Minimum = 0,
            Maximum = 9999,
            DecimalPlaces = 1,
            Value = (decimal)_preWeight,
            Font = ThemeColors.DefaultFont,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        cardPanel.Controls.Add(nudPostWeight);
        y += 34;

        // 失重率预览
        var lblLostPreview = new Label
        {
            Location = new Point(leftInput, y),
            Size = new Size(inputWidth + 30, 22),
            Text = $"预览：失重率 = ({_preWeight:F1} - 后质量) / {_preWeight:F1} × 100%",
            ForeColor = ThemeColors.TextSecondary,
            Font = ThemeColors.DefaultFont
        };
        cardPanel.Controls.Add(lblLostPreview);
        nudPostWeight.ValueChanged += (s, e) =>
        {
            var p = (double)nudPostWeight.Value;
            var rate = _preWeight > 0 ? (_preWeight - p) / _preWeight * 100 : 0;
            lblLostPreview.Text = $"预览：失重率 = ({_preWeight:F1} - {p:F1}) / {_preWeight:F1} × 100% = {rate:F2}%";
        };
        y += 34;

        // 备注
        AddStyledLabel(cardPanel, "备注：", leftLabel, y + 10);
        txtMemo = new TextBox { Location = new Point(leftInput, y), Size = new Size(inputWidth, 55), Multiline = true, Font = ThemeColors.DefaultFont, BorderStyle = BorderStyle.FixedSingle };
        cardPanel.Controls.Add(txtMemo);

        // 按钮
        var btnY = 395;
        var btnCancel = ThemeColors.StyleCancelButton(new Button { Text = "取消", Location = new Point(360, btnY), Size = new Size(90, 38) });
        btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        var btnSave = ThemeColors.StyleButton(new Button { Text = "保存记录", Location = new Point(460, btnY), Size = new Size(90, 38) }, ThemeColors.BtnSuccess);
        btnSave.Click += BtnSave_Click;

        this.Controls.Add(btnCancel);
        this.Controls.Add(btnSave);
        this.AcceptButton = btnSave;
        this.CancelButton = btnCancel;
    }

    private void AddStyledLabel(Panel parent, string text, int x, int y)
    {
        parent.Controls.Add(new Label { Text = text, Location = new Point(x, y), Size = new Size(150, 22), TextAlign = ContentAlignment.MiddleRight, Font = ThemeColors.DefaultFont, ForeColor = ThemeColors.TextSecondary });
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (nudPostWeight.Value <= 0)
        {
            MessageBox.Show("请输入试验后质量。", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            nudPostWeight.Focus();
            return;
        }
        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}
