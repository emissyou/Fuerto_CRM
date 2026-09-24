using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

public class CrmKpiCard : Control
{
    // ---- Public data ----
    public string Label { get; set; } = "METRIC";
    public string Value { get; set; } = "0";
    public string? DeltaText { get; set; }
    public bool DeltaPositive { get; set; } = true;
    public string? SubLabel { get; set; }
    public Color AccentColor { get; set; } = Color.FromArgb(255, 168, 0);
    public Color IconBgColor { get; set; } = Color.FromArgb(255, 245, 225);
    public Color IconFgColor { get; set; } = Color.FromArgb(160, 95, 0);
    public string Icon { get; set; } = "●";

    // ---- NEW: Navigation target ----
    public string? TargetPage { get; set; }

    // ---- NEW: Click event (fires with target page name) ----
    public event EventHandler<string>? NavigateRequested;

    // ---- Sparkline data ----
    public List<double> TrendValues { get; set; } = new();
    public bool ShowTrend { get; set; } = true;

    private readonly CrmSparkline _sparkline;
    private bool _isHovered = false;

    public CrmKpiCard()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.ResizeRedraw, true);

        BackColor = Color.White;
        Padding = new Padding(0);
        Size = new Size(260, 130);

        // Clickable
        Cursor = Cursors.Hand;

        _sparkline = new CrmSparkline
        {
            ShowFill = true,
            ShowDots = false,
            BackColor = Color.White
        };
        Controls.Add(_sparkline);

        // =========================================================
        // HOVER EFFECT
        // =========================================================
        MouseEnter += (_, _) => { _isHovered = true; Invalidate(); };
        MouseLeave += (_, _) => { _isHovered = false; Invalidate(); };

        // Child controls forward hover
        _sparkline.MouseEnter += (_, _) => { _isHovered = true; Invalidate(); };
        _sparkline.MouseLeave += (_, _) => { _isHovered = false; Invalidate(); };

        // =========================================================
        // CLICK HANDLING
        // =========================================================
        Click += OnCardClicked;
        _sparkline.Click += OnCardClicked;
    }

    private void OnCardClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TargetPage))
        {
            NavigateRequested?.Invoke(this, TargetPage!);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Clear background
        g.Clear(Color.White);

        // Rounded bg
        var rect = new Rectangle(2, 2, Width - 5, Height - 5);

        // Hover shadow lift
        if (_isHovered)
        {
            using var shadowBrush = new SolidBrush(Color.FromArgb(20, 180, 185, 195));
            var shadowRect = new Rectangle(rect.X - 1, rect.Y + 2, rect.Width + 2, rect.Height);
            using var shadowPath = RoundedRect(shadowRect, 12);
            g.FillPath(shadowBrush, shadowPath);
        }

        using (var path = RoundedRect(rect, 12))
        using (var bgBrush = new SolidBrush(Color.White))
        {
            g.FillPath(bgBrush, path);
        }

        // Border — thicker gold on hover
        using (var path = RoundedRect(rect, 12))
        using (var pen = new Pen(_isHovered
            ? Color.FromArgb(255, 168, 0)
            : Color.FromArgb(238, 240, 244), _isHovered ? 2 : 1))
        {
            g.DrawPath(pen, path);
        }

        // Top accent stripe
        using (var accentPath = RoundedRectTop(
            new Rectangle(rect.X, rect.Y, rect.Width, 4), 12))
        using (var accentBrush = new SolidBrush(AccentColor))
        {
            g.FillPath(accentBrush, accentPath);
        }

        int y = 16;

        // ---- Icon circle ----
        var iconRect = new Rectangle(16, y, 34, 34);
        using (var iconBrush = new SolidBrush(IconBgColor))
            g.FillEllipse(iconBrush, iconRect);

        using (var iconFont = new Font("Segoe UI", 12f, FontStyle.Bold))
        using (var iconTextBrush = new SolidBrush(IconFgColor))
        {
            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(Icon, iconFont, iconTextBrush, iconRect, format);
        }

        // ---- Label ----
        using (var labelFont = new Font("Segoe UI", 7.75f, FontStyle.Bold))
        using (var labelBrush = new SolidBrush(Color.FromArgb(140, 148, 162)))
        {
            g.DrawString(Label, labelFont, labelBrush, 60, y + 10);
        }

        y += 44;

        // ---- Big value ----
        using (var valueFont = new Font("Segoe UI", 22f, FontStyle.Bold))
        using (var valueBrush = new SolidBrush(Color.FromArgb(28, 32, 40)))
        {
            g.DrawString(Value, valueFont, valueBrush, 14, y);
        }
        y += 40;

        // ---- Delta badge ----
        if (!string.IsNullOrWhiteSpace(DeltaText))
        {
            var badgeColor = DeltaPositive
                ? Color.FromArgb(230, 248, 236)
                : Color.FromArgb(254, 232, 232);
            var badgeTextColor = DeltaPositive
                ? Color.FromArgb(34, 140, 78)
                : Color.FromArgb(200, 55, 55);

            using var badgeFont = new Font("Segoe UI", 8f, FontStyle.Bold);
            var badgeText = DeltaPositive ? $"▲ {DeltaText}" : $"▼ {DeltaText}";
            var textSize = g.MeasureString(badgeText, badgeFont);
            int badgeW = (int)textSize.Width + 16;
            int badgeH = 20;

            var badgeRect = new Rectangle(16, y, badgeW, badgeH);
            using (var path = RoundedRect(badgeRect, 10))
            using (var brush = new SolidBrush(badgeColor))
            {
                g.FillPath(brush, path);
            }

            using var badgeBrush = new SolidBrush(badgeTextColor);
            g.DrawString(badgeText, badgeFont, badgeBrush, badgeRect.X + 8, badgeRect.Y + 3);
        }

        // ---- Sparkline ----
        if (ShowTrend && TrendValues.Count > 1)
        {
            _sparkline.Visible = true;
            _sparkline.LineColor = AccentColor;
            _sparkline.FillColor = AccentColor;
            _sparkline.SetData(TrendValues);
            _sparkline.Bounds = new Rectangle(Width - 90, Height - 55, 80, 34);
        }
        else
        {
            _sparkline.Visible = false;
        }

        // ---- Hover arrow hint ----
        if (_isHovered && !string.IsNullOrWhiteSpace(TargetPage))
        {
            using var arrowFont = new Font("Segoe UI", 12f, FontStyle.Bold);
            using var arrowBrush = new SolidBrush(Color.FromArgb(255, 168, 0));
            g.DrawString("→", arrowFont, arrowBrush, Width - 26, 12);
        }
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        if (d > bounds.Width) d = bounds.Width;
        if (d > bounds.Height) d = bounds.Height;
        if (d <= 0) { path.AddRectangle(bounds); return path; }

        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static GraphicsPath RoundedRectTop(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        if (d > bounds.Width) d = bounds.Width;

        path.AddArc(bounds.X, bounds.Y - d / 2, d, d, 180, 90);
        path.AddLine(bounds.X + d / 2, bounds.Y, bounds.Right - d / 2, bounds.Y);
        path.AddArc(bounds.Right - d, bounds.Y - d / 2, d, d, 270, 90);
        path.AddLine(bounds.Right, bounds.Y, bounds.X, bounds.Y);
        path.CloseFigure();
        return path;
    }
}