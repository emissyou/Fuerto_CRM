using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

public class CrmCard : Panel
{
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public Color AccentColor { get; set; } = Color.FromArgb(255, 168, 0);
    public bool ShowTopAccent { get; set; } = false;
    public bool RoundedCorners { get; set; } = true;
    public int CornerRadius { get; set; } = 12;
    public int ShadowDepth { get; set; } = 2;

    private Label? _titleLabel;
    private Label? _subtitleLabel;
    private Panel? _contentArea;

    public Panel ContentArea => _contentArea ?? this;

    public CrmCard()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.ResizeRedraw, true);

        BackColor = Color.White;   // ✅ SOLID
        Padding = new Padding(0);

        _titleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            Location = new Point(20, 16),
            Visible = false,
            BackColor = Color.White   // ✅ SOLID — was Transparent
        };
        Controls.Add(_titleLabel);

        _subtitleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(110, 118, 132),
            Location = new Point(20, 42),
            Visible = false,
            BackColor = Color.White   // ✅ SOLID — was Transparent
        };
        Controls.Add(_subtitleLabel);

        _contentArea = new Panel
        {
            BackColor = Color.White,   // ✅ SOLID
            Location = new Point(0, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        Controls.Add(_contentArea);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Clear with white — parent will layer properly
        g.Clear(Color.White);

        var rect = new Rectangle(
            ShadowDepth, ShadowDepth,
            Width - ShadowDepth * 2 - 1,
            Height - ShadowDepth * 2 - 1);

        if (rect.Width <= 0 || rect.Height <= 0) return;

        if (ShadowDepth > 0)
        {
            for (int i = ShadowDepth; i > 0; i--)
            {
                using var shadowBrush = new SolidBrush(Color.FromArgb(6 + i * 3, 180, 185, 195));
                var shadowRect = new Rectangle(
                    rect.X - i + 1, rect.Y - i + 1,
                    rect.Width + i * 2, rect.Height + i * 2);

                using var path = RoundedRect(shadowRect, CornerRadius + i);
                g.FillPath(shadowBrush, path);
            }
        }

        using (var bgBrush = new SolidBrush(Color.White))
        using (var path = RoundedRect(rect, CornerRadius))
        {
            g.FillPath(bgBrush, path);
        }

        using (var borderPen = new Pen(Color.FromArgb(238, 240, 244), 1))
        using (var path = RoundedRect(rect, CornerRadius))
        {
            g.DrawPath(borderPen, path);
        }

        if (ShowTopAccent)
        {
            var accentRect = new Rectangle(rect.X, rect.Y, rect.Width, 4);
            using var accentPath = RoundedRectTop(accentRect, CornerRadius);
            using var accentBrush = new SolidBrush(AccentColor);
            g.FillPath(accentBrush, accentPath);
        }

        if (_titleLabel != null)
        {
            _titleLabel.Text = Title;
            _titleLabel.Visible = !string.IsNullOrWhiteSpace(Title);
        }
        if (_subtitleLabel != null)
        {
            _subtitleLabel.Text = Subtitle;
            _subtitleLabel.Visible = !string.IsNullOrWhiteSpace(Subtitle);
        }

        if (_contentArea != null)
        {
            int headerHeight = string.IsNullOrWhiteSpace(Title)
                ? 0
                : (string.IsNullOrWhiteSpace(Subtitle) ? 50 : 72);

            _contentArea.Location = new Point(ShadowDepth + 1, ShadowDepth + headerHeight);
            _contentArea.Size = new Size(
                Math.Max(0, Width - ShadowDepth * 2 - 2),
                Math.Max(0, Height - ShadowDepth * 2 - headerHeight - 2));
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