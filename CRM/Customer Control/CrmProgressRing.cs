using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

public class CrmProgressRing : Control
{
    public double Percentage { get; set; } = 75;
    public Color RingColor { get; set; } = Color.FromArgb(34, 197, 94);
    public Color TrackColor { get; set; } = Color.FromArgb(240, 242, 248);
    public int RingThickness { get; set; } = 14;
    public string? CenterLabel { get; set; }
    public int CenterFontSize { get; set; } = 14;

    public CrmProgressRing()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.ResizeRedraw, true);

        BackColor = Color.White;
        Size = new Size(80, 80);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(BackColor);

        int diameter = Math.Min(Width, Height) - RingThickness;
        if (diameter <= 0) return;

        int x = (Width - diameter) / 2;
        int y = (Height - diameter) / 2;
        var rect = new Rectangle(x, y, diameter, diameter);

        // Track
        using (var trackPen = new Pen(TrackColor, RingThickness)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        })
        {
            g.DrawEllipse(trackPen, rect);
        }

        // Progress
        float sweep = (float)(Percentage / 100.0 * 360.0);
        if (sweep > 0)
        {
            using var progPen = new Pen(RingColor, RingThickness)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawArc(progPen, rect, -90, sweep);
        }

        // Center text
        var text = CenterLabel ?? $"{Percentage:F0}%";
        using var font = new Font("Segoe UI", CenterFontSize, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.FromArgb(26, 29, 41));

        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        g.DrawString(text, font, textBrush,
            new RectangleF(0, 0, Width, Height), format);
    }
}