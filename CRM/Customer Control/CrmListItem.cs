using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

public class CrmListItem : Control
{
    public string Title { get; set; } = "Music Soundsation 2020";
    public string Subtitle { get; set; } = "Jeappo Kemayoran, Jakarta";
    public string TimeRange { get; set; } = "08 Am - 12 Pm";
    public string Tag { get; set; } = "Music";
    public Color TagColor { get; set; } = Color.FromArgb(124, 92, 252);
    public Color IconBg { get; set; } = Color.FromArgb(255, 245, 225);
    public Color IconFg { get; set; } = Color.FromArgb(160, 95, 0);
    public string IconText { get; set; } = "18";
    public bool ShowSeparator { get; set; } = true;

    private bool _isHovered = false;

    public CrmListItem()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.ResizeRedraw, true);

        BackColor = Color.White;
        Height = 60;
        Cursor = Cursors.Hand;

        // =========================================================
        // HOVER EFFECT — subtle tint, consistent with CrmKpiCard/table rows
        // =========================================================
        MouseEnter += (_, _) => { _isHovered = true; Invalidate(); };
        MouseLeave += (_, _) => { _isHovered = false; Invalidate(); };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rowColor = _isHovered ? Color.FromArgb(252, 250, 246) : BackColor;
        g.Clear(rowColor);

        // ---- Icon on the left (square with rounded corners) ----
        int iconSize = 36;
        int iconY = (Height - iconSize) / 2;
        var iconRect = new Rectangle(8, iconY, iconSize, iconSize);

        using (var path = RoundedRect(iconRect, 10))
        using (var brush = new SolidBrush(IconBg))
        {
            g.FillPath(brush, path);
        }

        using (var font = new Font("Segoe UI", 10f, FontStyle.Bold))
        using (var brush = new SolidBrush(IconFg))
        {
            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(IconText, font, brush, iconRect, format);
        }

        // ---- Title + Subtitle ----
        int textX = iconRect.Right + 12;
        int titleY = 14;

        using (var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
        using (var titleBrush = new SolidBrush(Color.FromArgb(26, 29, 41)))
        {
            g.DrawString(Title, titleFont, titleBrush, textX, titleY);
        }

        using (var subFont = new Font("Segoe UI", 8f))
        using (var subBrush = new SolidBrush(Color.FromArgb(160, 168, 185)))
        {
            g.DrawString(Subtitle, subFont, subBrush, textX, titleY + 22);
        }

        // ---- Right side: tag + time ----
        int rightX = Width - 130;

        // Tag pill
        if (!string.IsNullOrWhiteSpace(Tag))
        {
            using var tagFont = new Font("Segoe UI", 7.75f, FontStyle.Bold);
            var tagSize = g.MeasureString(Tag, tagFont);

            int tagW = (int)tagSize.Width + 16;
            int tagH = 20;
            int tagY = (Height - tagH) / 2;

            var tagRect = new Rectangle(rightX, tagY, tagW, tagH);
            using (var tagPath = RoundedRect(tagRect, 10))
            using (var tagBrush = new SolidBrush(Color.FromArgb(30, TagColor)))
            {
                g.FillPath(tagBrush, tagPath);
            }

            using var tagTextBrush = new SolidBrush(TagColor);
            g.DrawString(Tag, tagFont, tagTextBrush, tagRect.X + 8, tagRect.Y + 4);

            rightX = tagRect.Right + 12;
        }

        // Time
        if (!string.IsNullOrWhiteSpace(TimeRange))
        {
            using var timeFont = new Font("Segoe UI", 7.75f);
            using var timeBrush = new SolidBrush(Color.FromArgb(160, 168, 185));
            var timeSize = g.MeasureString(TimeRange, timeFont);

            g.DrawString(TimeRange, timeFont, timeBrush,
                Width - timeSize.Width - 12,
                (Height - timeSize.Height) / 2);
        }

        // ---- Bottom hairline separator ----
        if (ShowSeparator)
        {
            using var linePen = new Pen(Color.FromArgb(238, 240, 244), 1);
            g.DrawLine(linePen, textX, Height - 1, Width - 12, Height - 1);
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
}   