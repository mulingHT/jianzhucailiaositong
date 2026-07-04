using ISO11820.Core;
using ISO11820.Data;
using ISO11820.Forms;
using ISO11820.Helpers;

namespace ISO11820;

/// <summary>
/// 登录窗体 — 角色选择 + 密码验证
/// 严格按开发文档 2.1 节实现
/// </summary>
public partial class LoginForm : Form
{
    private TextBox txtPassword = null!;
    private RadioButton rbAdmin = null!;
    private RadioButton rbExperimenter = null!;
    private Button btnLogin = null!;
    private Label lblError = null!;

    public LoginForm()
    {
        InitializeComponent();
        this.Load += (s, e) => Helpers.ThemeColors.ApplyFadeIn(this, 12, 0.08);
    }

    private void InitializeComponent()
    {
        this.Text = "ISO 11820 建筑材料不燃性试验系统 - 登录";
        this.Size = new Size(440, 340);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = ThemeColors.Surface;

        // 顶部品牌条
        var brandBar = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(440, 4),
            BackColor = ThemeColors.BrandPrimary
        };

        // 应用图标区（纯文字图标）
        var iconLabel = new Label
        {
            Text = "◇",
            Font = new Font("Segoe UI Symbol", 28, FontStyle.Regular),
            ForeColor = ThemeColors.BrandPrimary,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(20, 18),
            Size = new Size(50, 50)
        };

        // 标题
        var lblTitle = new Label
        {
            Text = "ISO 11820 建筑材料不燃性试验系统",
            Font = ThemeColors.TitleFont,
            ForeColor = ThemeColors.BrandPrimary,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(75, 20),
            Size = new Size(340, 30)
        };
        var lblSubtitle = new Label
        {
            Text = "Non-Combustibility Test Simulation System",
            Font = ThemeColors.SmallLabelFont,
            ForeColor = ThemeColors.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(75, 48),
            Size = new Size(340, 20)
        };

        // 登录卡片
        var loginCard = new Panel
        {
            Location = new Point(25, 80),
            Size = new Size(375, 205),
            BackColor = ThemeColors.CardBackground,
            BorderStyle = BorderStyle.None
        };
        loginCard.Paint += (s, e) =>
        {
            using var pen = new Pen(ThemeColors.CardBorder);
            e.Graphics.DrawRectangle(pen, 0, 0, loginCard.Width - 1, loginCard.Height - 1);
        };

        // 角色选择
        var gbRole = new GroupBox
        {
            Text = "选择角色",
            Location = new Point(30, 18),
            Size = new Size(310, 55),
            Font = ThemeColors.DefaultFont,
            ForeColor = ThemeColors.TextPrimary
        };

        rbAdmin = new RadioButton
        {
            Text = "管理员 (admin)",
            Location = new Point(25, 22),
            Size = new Size(150, 25),
            Font = ThemeColors.DefaultFont,
            Checked = true
        };

        rbExperimenter = new RadioButton
        {
            Text = "试验员 (experimenter)",
            Location = new Point(175, 22),
            Size = new Size(145, 25),
            Font = ThemeColors.DefaultFont
        };

        gbRole.Controls.Add(rbAdmin);
        gbRole.Controls.Add(rbExperimenter);

        // 密码
        var lblPwd = new Label
        {
            Text = "访问口令：",
            Location = new Point(55, 88),
            Size = new Size(75, 28),
            TextAlign = ContentAlignment.MiddleRight,
            Font = ThemeColors.DefaultFont,
            ForeColor = ThemeColors.TextSecondary
        };

        txtPassword = new TextBox
        {
            Location = new Point(135, 90),
            Size = new Size(175, 28),
            PasswordChar = '*',
            UseSystemPasswordChar = true,
            Font = ThemeColors.DefaultFont,
            BorderStyle = BorderStyle.FixedSingle
        };
        txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnLogin.PerformClick(); };

        // 登录按钮
        btnLogin = ThemeColors.StyleButton(
            new Button { Text = "登  录", Location = new Point(135, 130), Size = new Size(175, 36) },
            ThemeColors.BrandPrimary);
        btnLogin.Click += BtnLogin_Click;

        // 错误提示
        lblError = new Label
        {
            ForeColor = ThemeColors.BtnDanger,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(40, 175),
            Size = new Size(295, 22),
            Font = ThemeColors.DefaultFont,
            Visible = false
        };

        loginCard.Controls.Add(gbRole);
        loginCard.Controls.Add(lblPwd);
        loginCard.Controls.Add(txtPassword);
        loginCard.Controls.Add(btnLogin);
        loginCard.Controls.Add(lblError);

        this.Controls.Add(brandBar);
        this.Controls.Add(iconLabel);
        this.Controls.Add(lblTitle);
        this.Controls.Add(lblSubtitle);
        this.Controls.Add(loginCard);
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        var username = rbAdmin.Checked ? "admin" : "experimenter";
        var pwd = txtPassword.Text;

        var db = GlobalContext.Instance.Db;
        if (db.Login(username, pwd, out string userid, out string usertype))
        {
            GlobalContext.Instance.CurrentOperator = username;
            GlobalContext.Instance.CurrentUserType = usertype;
            lblError.Visible = false;

            var mainForm = new MainForm();
            mainForm.FormClosed += (s, args) => this.Close();
            mainForm.Show();
            this.Hide();
        }
        else
        {
            lblError.Text = "密码错误，请重新输入";
            lblError.Visible = true;
            txtPassword.SelectAll();
            txtPassword.Focus();
        }
    }
}
