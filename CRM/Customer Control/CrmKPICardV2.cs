using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

public class CrmKPICardV2 : Control
{
    public string Label { get; set; } = "Active Event";
    public string Value { get; set; } = "4.321";
    public string? DeltaText { get; set; } = "+3.9%";
    public bool DeltaPositive { get; set; } = true;
    public string? SubLabel { get; set; } = "Event in This Month";

    // Visual style
    public enum VisualKind { ProgressRing, MiniBars, None }
    public VisualKind Visual { get; set; } = VisualKind.ProgressRing;

    // Ring
    public double RingPercentage { get; set; } = 75;
    public Color RingColor { get; set; } = Color.FromArgb(34, 197, 94);

    // Mini bars
    public List<double> BarValues { get; set; } = new();
    public Color BarColor { get; set; } = Color.FromArgb(255, 168, 0);

    // Chart zone size (width reserved on the right for the ring/bars)
    public int VisualZoneWidth { get; set; } = 90;

    public CrmKPICardV2()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.ResizeRedraw, true);

        BackColor = Color.White;
        Size = new Size(300, 130);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // ---- Card background ----
        g.Clear(Color.FromArgb(245, 246, 248));

        var rect = new Rectangle(2, 2, Width - 5, Height - 5);

        // Card shadow (subtle)
        using (var shadowBrush = new SolidBrush(Color.FromArgb(10, 200, 205, 215)))
        {
            using var shadowPath = RoundedRect(
                new Rectangle(rect.X + 1, rect.Y + 2, rect.Width, rect.Height), 14);
            g.FillPath(shadowBrush, shadowPath);
        }

        using (var bgPath = RoundedRect(rect, 14))
        using (var bgBrush = new SolidBrush(Color.White))
        {
            g.FillPath(bgBrush, bgPath);
        }

        // Border
        using (var borderPath = RoundedRect(rect, 14))
        using (var borderPen = new Pen(Color.FromArgb(240, 242, 246), 1))
        {
            g.DrawPath(borderPen, borderPath);
        }

        // ---- Left zone: label, value, delta, sublabel ----
        int leftPad = 20;
        int y = 18;

        // Label
        using (var labelFont = new Font("Segoe UI", 9f))
        using (var labelBrush = new SolidBrush(Color.FromArgb(138, 145, 166)))
        {
            g.DrawString(Label, labelFont, labelBrush, leftPad, y);
        }

        y += 26;

        // Value
        using (var valueFont = new Font("Segoe UI", 20f, FontStyle.Bold))
        using (var valueBrush = new SolidBrush(Color.FromArgb(26, 29, 41)))
        {
            var valueSize = g.MeasureString(Value, valueFont);
            g.DrawString(Value, valueFont, valueBrush, leftPad, y);

            // Delta badge next to value
            if (!string.IsNullOrWhiteSpace(DeltaText))
            {
                int deltaX = leftPad + (int)valueSize.Width + 8;
                int deltaY = y + 8;

                var deltaColor = DeltaPositive
                    ? Color.FromArgb(34, 197, 94)
                    : Color.FromArgb(239, 68, 68);

                using var deltaFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                using var deltaBrush = new SolidBrush(deltaColor);

                var text = (DeltaPositive ? "+" : "") + DeltaText;
                // Strip leading + if already provided
                if (text.StartsWith("++")) text = text.Substring(1);

                g.DrawString(text, deltaFont, deltaBrush, deltaX, deltaY);
            }
        }

        y += 42;

        // Sub-label
        if (!string.IsNullOrWhiteSpace(SubLabel))
        {
            using var subFont = new Font("Segoe UI", 8.5f);
            using var subBrush = new SolidBrush(Color.FromArgb(160, 168, 185));
            g.DrawString(SubLabel, subFont, subBrush, leftPad, y);
        }

        // ---- Right zone: visual ----
        int visualX = Width - VisualZoneWidth - 16;
        int visualY = 24;
        int visualH = Height - 48;

        if (Visual == VisualKind.ProgressRing && visualH > 20)
        {
            int ringSize = Math.Min(VisualZoneWidth - 20, visualH);
            int rx = visualX + (VisualZoneWidth - ringSize) / 2;
            int ry = 14 + (Height - 28 - ringSize) / 2;

            DrawRing(g, rx, ry, ringSize, RingPercentage, RingColor);
        }
        else if (Visual == VisualKind.MiniBars && BarValues.Count > 0)
        {
            DrawMiniBars(g, visualX, 20, VisualZoneWidth - 20, Height - 40);
        }
    }

    private static void DrawRing(Graphics g, int x, int y, int size, double pct, Color color)
    {
        int thickness = 10;
        var rect = new Rectangle(x, y, size, size);

        // Track
        using (var trackPen = new Pen(Color.FromArgb(240, 242, 248), thickness)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        })
        {
            g.DrawArc(trackPen, rect, 0, 360);
        }

        // Progress
        float sweep = (float)(pct / 100.0 * 360.0);
        if (sweep > 0)
        {
            using var progPen = new Pen(color, thickness)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawArc(progPen, rect, -90, sweep);
        }

        // Center %
        using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.FromArgb(26, 29, 41));

        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        g.DrawString($"{pct:F0}%", font, textBrush, rect, format);
    }

    private void DrawMiniBars(Graphics g, int x, int y, int w, int h)
    {
        if (BarValues.Count == 0) return;

        double max = BarValues.Max();
        if (max <= 0) max = 1;

        int barW = 6;
        int gap = 3;
        int totalW = BarValues.Count * barW + (BarValues.Count - 1) * gap;
        int startX = x + (w - totalW) / 2;
        int baseY = y + h;

        for (int i = 0; i < BarValues.Count; i++)
        {
            int bh = (int)(BarValues[i] / max * (h - 4));
            if (bh < 2) bh = 2;

            int bx = startX + i * (barW + gap);
            int by = baseY - bh;

            var color = (i == BarValues.Count - 2)
                ? Color.FromArgb(255, 168, 0)
                : Color.FromArgb(70, 255, 168, 0);

            using var brush = new SolidBrush(color);
            using var path = RoundedBar(new Rectangle(bx, by, barW, bh), 2);
            g.FillPath(brush, path);
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

    private static GraphicsPath RoundedBar(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        if (d > bounds.Width) d = bounds.Width;
        if (d > bounds.Height) d = bounds.Height;
        if (d <= 0) { path.AddRectangle(bounds); return path; }

        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddLine(bounds.Right, bounds.Y + d / 2, bounds.Right, bounds.Bottom);
        path.AddLine(bounds.Right, bounds.Bottom, bounds.X, bounds.Bottom);
        path.AddLine(bounds.X, bounds.Bottom, bounds.X, bounds.Y + d / 2);
        path.CloseFigure();
        return path;
    }
}