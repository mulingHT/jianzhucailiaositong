using OxyPlot;
using System.Drawing.Drawing2D;

namespace ISO11820.Helpers;

/// <summary>
/// 全局主题颜色、字体、样式和动画工具 — 统一管理整个应用的视觉风格
/// </summary>
public static class ThemeColors
{
    #region 颜色常量 — 丰富鲜艳的工业HMI风格

    // 窗口背景
    public static Color Surface => Color.FromArgb(238, 242, 255);
    public static Color CardBackground => Color.White;
    public static Color CardBorder => Color.FromArgb(200, 212, 230);
    public static Color ShadowColor => Color.FromArgb(30, 0, 0, 0);

    // 文字
    public static Color TextPrimary => Color.FromArgb(15, 23, 42);
    public static Color TextSecondary => Color.FromArgb(100, 116, 139);
    public static Color TextOnDark => Color.FromArgb(241, 245, 249);

    // 品牌色 — 浓郁海军蓝
    public static Color BrandPrimary => Color.FromArgb(15, 43, 74);
    public static Color BrandAccent => Color.FromArgb(59, 130, 246);
    public static Color BrandLight => Color.FromArgb(96, 165, 250);
    public static Color GradientStart => Color.FromArgb(15, 43, 74);
    public static Color GradientEnd => Color.FromArgb(33, 68, 125);

    // 按钮颜色 — 鲜艳
    public static Color BtnPrimary => Color.FromArgb(59, 130, 246);
    public static Color BtnSuccess => Color.FromArgb(16, 185, 129);
    public static Color BtnWarning => Color.FromArgb(245, 158, 11);
    public static Color BtnDanger => Color.FromArgb(239, 68, 68);
    public static Color BtnPurple => Color.FromArgb(139, 92, 246);
    public static Color BtnDisabled => Color.FromArgb(203, 213, 225);
    public static Color BtnDisabledText => Color.FromArgb(148, 163, 184);
    public static Color BtnCancel => Color.FromArgb(226, 232, 240);

    // LED 温度显示
    public static Color LedBezel => Color.FromArgb(15, 15, 30);
    public static Color LedBg => Color.FromArgb(5, 8, 12);
    public static Color LedText => Color.FromArgb(0, 255, 85);
    public static Color LedTextPulse => Color.FromArgb(100, 255, 150);
    public static Color LedTitle => Color.FromArgb(140, 160, 180);

    // 日志
    public static Color LogBg => Color.FromArgb(10, 17, 32);
    public static Color LogTextNormal => Color.FromArgb(200, 215, 235);
    public static Color LogTextWarn => Color.FromArgb(251, 191, 36);
    public static Color LogTextError => Color.FromArgb(248, 113, 113);

    // 图表 (OxyPlot)
    public static OxyColor ChartBg => OxyColors.White;
    public static OxyColor ChartPlotAreaBg => OxyColors.White;
    public static OxyColor ChartPlotBorder => OxyColor.FromRgb(200, 212, 230);
    public static OxyColor ChartGrid => OxyColor.FromRgb(238, 242, 248);
    public static OxyColor ChartAxisText => OxyColor.FromRgb(100, 116, 139);
    public static OxyColor ChartTitle => OxyColor.FromRgb(15, 43, 74);

    // 图线（鲜艳版）
    public static OxyColor LineTF1 => OxyColor.FromRgb(239, 68, 68);
    public static OxyColor LineTF2 => OxyColor.FromRgb(59, 130, 246);
    public static OxyColor LineTS => OxyColor.FromRgb(16, 185, 129);
    public static OxyColor LineTC => OxyColor.FromRgb(245, 158, 11);

    // 面积填充（半透明）
    public static OxyColor AreaTF1 => OxyColor.FromArgb(30, 239, 68, 68);
    public static OxyColor AreaTF2 => OxyColor.FromArgb(30, 59, 130, 246);
    public static OxyColor AreaTS => OxyColor.FromArgb(30, 16, 185, 129);
    public static OxyColor AreaTC => OxyColor.FromArgb(30, 245, 158, 11);

    // DataGridView
    public static Color GridHeaderBg => Color.FromArgb(15, 43, 74);
    public static Color GridHeaderText => Color.White;
    public static Color GridRowBg => Color.White;
    public static Color GridRowAltBg => Color.FromArgb(246, 249, 255);
    public static Color GridSelectionBg => Color.FromArgb(219, 234, 254);
    public static Color GridBorder => Color.FromArgb(226, 232, 240);

    // 状态颜色 — 更鲜明
    public static Color StateIdle => Color.FromArgb(100, 116, 139);
    public static Color StatePreparing => Color.FromArgb(245, 158, 11);
    public static Color StateReady => Color.FromArgb(16, 185, 129);
    public static Color StateRecording => Color.FromArgb(59, 130, 246);
    public static Color StateComplete => Color.FromArgb(5, 150, 105);

    // Tab
    public static Color TabActiveBg => Color.White;
    public static Color TabInactiveBg => Color.FromArgb(226, 232, 240);
    public static Color TabIndicator => Color.FromArgb(59, 130, 246);

    // 信息面板
    public static Color InfoPanelBg => Color.FromArgb(246, 249, 255);

    // 进度条
    public static Color ProgressBg => Color.FromArgb(226, 232, 240);
    public static Color ProgressFill => Color.FromArgb(59, 130, 246);

    #endregion

    #region 字体预设

    public static readonly Font DefaultFont = new("Microsoft YaHei", 9.5f);
    public static readonly Font TitleFont = new("Microsoft YaHei", 15f, FontStyle.Bold);
    public static readonly Font SubtitleFont = new("Microsoft YaHei", 11f, FontStyle.Bold);
    public static readonly Font ButtonFont = new("Microsoft YaHei", 9f, FontStyle.Bold);
    public static readonly Font MonoFont = new("Consolas", 11f, FontStyle.Bold);
    public static readonly Font MonoSmallFont = new("Consolas", 9f);
    public static readonly Font LedFont = new("Consolas", 18f, FontStyle.Bold);
    public static readonly Font LedLabelFont = new("Microsoft YaHei", 10f);
    public static readonly Font GroupTitleFont = new("Microsoft YaHei", 9f, FontStyle.Bold);
    public static readonly Font SmallLabelFont = new("Microsoft YaHei", 8f);
    public static readonly Font HeaderLabelFont = new("Microsoft YaHei", 8.5f, FontStyle.Bold);
    public static readonly Font BigNumberFont = new("Consolas", 22f, FontStyle.Bold);

    #endregion

    #region 渐变绘制

    public static void PaintGradientHeader(Panel panel, PaintEventArgs e, Color top, Color bottom)
    {
        using var brush = new LinearGradientBrush(
            panel.ClientRectangle, top, bottom, LinearGradientMode.Vertical);
        e.Graphics.FillRectangle(brush, panel.ClientRectangle);
    }

    public static Panel CreateGradientHeader(string text, int x, int y, int w, int h, Color topColor, Color bottomColor)
    {
        var panel = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(w, h),
            BorderStyle = BorderStyle.None
        };
        panel.Paint += (s, e) => PaintGradientHeader(panel, e, topColor, bottomColor);

        var label = new Label
        {
            Text = text,
            Location = new Point(12, 4),
            Size = new Size(w - 24, h - 8),
            ForeColor = Color.FromArgb(241, 245, 249),
            Font = HeaderLabelFont,
            BackColor = Color.Transparent
        };
        panel.Controls.Add(label);
        return panel;
    }

    #endregion

    #region 阴影效果

    public static Panel CreateShadowCard(int x, int y, int w, int h, Panel parent)
    {
        var shadow = new Panel
        {
            Location = new Point(x + 3, y + 3),
            Size = new Size(w, h),
            BackColor = Color.FromArgb(15, 0, 0, 0),
            BorderStyle = BorderStyle.None
        };
        parent.Controls.Add(shadow);

        var card = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = CardBackground,
            BorderStyle = BorderStyle.None
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(CardBorder);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        return card;
    }

    #endregion

    #region 按钮样式

    public static Button StyleButton(Button btn, Color backColor)
    {
        btn.BackColor = backColor;
        btn.ForeColor = Color.White;
        btn.Font = ButtonFont;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseDownBackColor = DarkenColor(backColor, 0.2f);
        btn.UseVisualStyleBackColor = false;
        btn.Cursor = Cursors.Hand;
        btn.TextAlign = ContentAlignment.MiddleCenter;

        Color originalColor = backColor;
        Color hoverColor = LightenColor(backColor, 0.2f);
        Color pressColor = DarkenColor(backColor, 0.25f);
        Color disabledBack = BtnDisabled;
        Color disabledFore = BtnDisabledText;

        btn.MouseEnter += (s, e) => { if (btn.Enabled) btn.BackColor = hoverColor; };
        btn.MouseLeave += (s, e) => { if (btn.Enabled) btn.BackColor = originalColor; };
        btn.MouseDown += (s, e) => { if (btn.Enabled) btn.BackColor = pressColor; };
        btn.MouseUp += (s, e) =>
        {
            if (btn.Enabled && btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position)))
                btn.BackColor = hoverColor;
            else if (btn.Enabled) btn.BackColor = originalColor;
        };
        btn.EnabledChanged += (s, e) =>
        {
            if (btn.Enabled)
            {
                btn.BackColor = originalColor;
                btn.ForeColor = Color.White;
                btn.Cursor = Cursors.Hand;
            }
            else
            {
                btn.BackColor = disabledBack;
                btn.ForeColor = disabledFore;
                btn.Cursor = Cursors.Default;
            }
        };

        return btn;
    }

    public static Button StyleCancelButton(Button btn)
    {
        btn.BackColor = BtnCancel;
        btn.ForeColor = TextPrimary;
        btn.Font = ButtonFont;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.UseVisualStyleBackColor = false;
        btn.Cursor = Cursors.Hand;
        btn.TextAlign = ContentAlignment.MiddleCenter;

        Color hoverColor = Color.FromArgb(203, 213, 225);
        Color pressColor = Color.FromArgb(180, 190, 200);

        btn.MouseEnter += (s, e) => { if (btn.Enabled) btn.BackColor = hoverColor; };
        btn.MouseLeave += (s, e) => { if (btn.Enabled) btn.BackColor = BtnCancel; };
        btn.MouseDown += (s, e) => { if (btn.Enabled) btn.BackColor = pressColor; };
        btn.MouseUp += (s, e) =>
        {
            if (btn.Enabled && btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position)))
                btn.BackColor = hoverColor;
            else if (btn.Enabled) btn.BackColor = BtnCancel;
        };
        btn.EnabledChanged += (s, e) =>
        {
            if (!btn.Enabled) { btn.BackColor = BtnDisabled; btn.ForeColor = BtnDisabledText; btn.Cursor = Cursors.Default; }
            else { btn.BackColor = BtnCancel; btn.ForeColor = TextPrimary; btn.Cursor = Cursors.Hand; }
        };

        return btn;
    }

    private static Color LightenColor(Color c, float amount)
    {
        int r = Math.Min(255, (int)(c.R + (255 - c.R) * amount));
        int g = Math.Min(255, (int)(c.G + (255 - c.G) * amount));
        int b = Math.Min(255, (int)(c.B + (255 - c.B) * amount));
        return Color.FromArgb(r, g, b);
    }

    private static Color DarkenColor(Color c, float amount)
    {
        int r = Math.Max(0, (int)(c.R * (1 - amount)));
        int g = Math.Max(0, (int)(c.G * (1 - amount)));
        int b = Math.Max(0, (int)(c.B * (1 - amount)));
        return Color.FromArgb(r, g, b);
    }

    #endregion

    #region DataGridView 样式

    public static DataGridView StyleDataGridView(DataGridView dgv)
    {
        dgv.BackgroundColor = GridRowBg;
        dgv.BorderStyle = BorderStyle.None;
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgv.GridColor = GridBorder;
        dgv.RowHeadersVisible = false;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.ColumnHeadersHeight = 38;
        dgv.RowTemplate.Height = 34;
        dgv.AllowUserToAddRows = false;
        dgv.ReadOnly = true;

        dgv.EnableHeadersVisualStyles = false;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderBg;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = GridHeaderText;
        dgv.ColumnHeadersDefaultCellStyle.Font = GroupTitleFont;
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

        dgv.DefaultCellStyle.BackColor = GridRowBg;
        dgv.DefaultCellStyle.ForeColor = TextPrimary;
        dgv.DefaultCellStyle.Font = DefaultFont;
        dgv.DefaultCellStyle.SelectionBackColor = GridSelectionBg;
        dgv.DefaultCellStyle.SelectionForeColor = TextPrimary;
        dgv.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        dgv.AlternatingRowsDefaultCellStyle.BackColor = GridRowAltBg;
        dgv.AlternatingRowsDefaultCellStyle.ForeColor = TextPrimary;
        dgv.AlternatingRowsDefaultCellStyle.Font = DefaultFont;

        return dgv;
    }

    #endregion

    #region 工具方法

    public static Label CreateHeaderLabel(string text)
    {
        return new Label
        {
            Text = text,
            Font = TitleFont,
            ForeColor = BrandPrimary,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(420, 35)
        };
    }

    public static Panel CreateSeparator(int x, int y, int width)
    {
        return new Panel
        {
            Location = new Point(x, y),
            Size = new Size(width, 1),
            BackColor = CardBorder,
            BorderStyle = BorderStyle.None
        };
    }

    public static Panel CreateCardPanel(int x, int y, int w, int h)
    {
        return new Panel
        {
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = CardBackground,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    /// <summary>
    /// 创建动画淡入效果
    /// </summary>
    public static System.Windows.Forms.Timer ApplyFadeIn(Form form, int intervalMs = 15, double step = 0.06)
    {
        form.Opacity = 0;
        var timer = new System.Windows.Forms.Timer { Interval = intervalMs };
        timer.Tick += (s, e) =>
        {
            form.Opacity += step;
            if (form.Opacity >= 1.0)
            {
                form.Opacity = 1.0;
                timer.Stop();
                timer.Dispose();
            }
        };
        timer.Start();
        return timer;
    }

    /// <summary>
    /// 绘制状态指示圆点（带光晕）
    /// </summary>
    public static void PaintStatusDot(Graphics g, int x, int y, int size, Color color, float alpha = 1.0f)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        // 光晕
        using var glowBrush = new SolidBrush(Color.FromArgb((int)(alpha * 50), color));
        g.FillEllipse(glowBrush, x - 3, y - 3, size + 6, size + 6);
        // 主圆点
        using var brush = new SolidBrush(Color.FromArgb((int)(alpha * 255), color));
        g.FillEllipse(brush, x, y, size, size);
    }

    #endregion
}
