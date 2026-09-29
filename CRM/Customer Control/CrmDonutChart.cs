using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

public class CrmDonutChart : Control
{
    public class Slice
    {
        public string Label { get; set; } = "";
        public double Value { get; set; }
        public Color Color { get; set; } = Color.Gray;
    }

    public List<Slice> Slices { get; set; } = new();
    public string CenterLabel { get; set; } = "";
    public string CenterValue { get; set; } = "0";
    public int DonutThickness { get; set; } = 26;
    public bool ShowLegend { get; set; } = true;

    private readonly List<(float Start, float Sweep, Slice Slice, int Index)> _sliceAngles = new();
    private PointF _center;
    private float _outerRadius;
    private float _innerRadius;
    private int _hoverIndex = -1;

    public CrmDonutChart()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);

        BackColor = Color.Transparent;
    }

    public void SetData(IEnumerable<Slice> slices, string centerValue, string centerLabel)
    {
        Slices = slices.ToList();
        CenterValue = centerValue;
        CenterLabel = centerLabel;
        _hoverIndex = -1;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        int found = -1;
        float dx = e.X - _center.X;
        float dy = e.Y - _center.Y;
        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

        if (dist >= _innerRadius && dist <= _outerRadius)
        {
            double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
            if (angle < -90) angle += 360;

            foreach (var s in _sliceAngles)
            {
                double end = s.Start + s.Sweep;
                if (angle >= s.Start && angle < end)
                {
                    found = s.Index;
                    break;
                }
            }
        }

        if (found != _hoverIndex)
        {
            _hoverIndex = found;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverIndex != -1)
        {
            _hoverIndex = -1;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        _sliceAngles.Clear();

        if (Slices.Count == 0)
        {
            using var emptyFont = new Font("Segoe UI", 9f);
            using var emptyBrush = new SolidBrush(Color.FromArgb(160, 168, 180));
            var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString("No data yet", emptyFont, emptyBrush, ClientRectangle, fmt);
            return;
        }

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int legendWidth = ShowLegend ? 150 : 0;
        int donutSize = Math.Min(Height - 30, Width - legendWidth - 30);
        if (donutSize <= 0) return;

        int cx = (Width - legendWidth - 30) / 2 + 15;
        int cy = Height / 2;
        _center = new PointF(cx, cy);

        var outerRect = new Rectangle(
            cx - donutSize / 2, cy - donutSize / 2,
            donutSize, donutSize);

        var innerRect = new Rectangle(
            outerRect.X + DonutThickness,
            outerRect.Y + DonutThickness,
            outerRect.Width - DonutThickness * 2,
            outerRect.Height - DonutThickness * 2);

        if (innerRect.Width <= 0 || innerRect.Height <= 0) return;

        _outerRadius = donutSize / 2f;
        _innerRadius = innerRect.Width / 2f;

        double total = Slices.Sum(s => s.Value);
        if (total <= 0) total = 1;

        // Soft shadow under the ring
        using (var shadowBrush = new SolidBrush(Color.FromArgb(10, 90, 96, 110)))
        {
            var shadowOuter = new Rectangle(outerRect.X, outerRect.Y + 2, outerRect.Width, outerRect.Height);
            var shadowInner = new Rectangle(innerRect.X, innerRect.Y + 2, innerRect.Width, innerRect.Height);
            using var ringPath = new GraphicsPath();
            ringPath.AddEllipse(shadowOuter);
            ringPath.AddEllipse(shadowInner);
            ringPath.FillMode = FillMode.Alternate;
            g.FillPath(shadowBrush, ringPath);
        }

        float startAngle = -90f;

        for (int i = 0; i < Slices.Count; i++)
        {
            var slice = Slices[i];
            float sweepAngle = (float)(slice.Value / total * 360.0);
            if (sweepAngle <= 0) continue;

            _sliceAngles.Add((startAngle, sweepAngle, slice, i));

            bool isHovered = i == _hoverIndex;

            // Hovered slice "explodes" a few px outward along its bisecting angle
            var sliceOuter = outerRect;
            var sliceInner = innerRect;
            if (isHovered)
            {
                double midAngle = (startAngle + sweepAngle / 2) * Math.PI / 180.0;
                float offset = 6f;
                var shift = new PointF((float)(offset * Math.Cos(midAngle)), (float)(offset * Math.Sin(midAngle)));
                sliceOuter = new Rectangle((int)(outerRect.X + shift.X), (int)(outerRect.Y + shift.Y), outerRect.Width, outerRect.Height);
                sliceInner = new Rectangle((int)(innerRect.X + shift.X), (int)(innerRect.Y + shift.Y), innerRect.Width, innerRect.Height);
            }

            using var path = new GraphicsPath();
            path.AddArc(sliceOuter, startAngle, sweepAngle);
            path.AddArc(sliceInner, startAngle + sweepAngle, -sweepAngle);
            path.CloseFigure();

            var fillColor = isHovered ? Lighten(slice.Color, 0.12f) : slice.Color;
            using var brush = new SolidBrush(fillColor);
            g.FillPath(brush, path);

            if (isHovered)
            {
                using var outline = new Pen(Color.White, 1.5f);
                g.DrawPath(outline, path);
            }

            startAngle += sweepAngle;
        }

        // Center label — shows the hovered slice's own value while hovering
        string centerTopText = CenterValue;
        string centerBottomText = CenterLabel;
        if (_hoverIndex >= 0 && _hoverIndex < Slices.Count)
        {
            var hovered = Slices[_hoverIndex];
            double pct = hovered.Value / total * 100.0;
            centerTopText = $"{pct:F0}%";
            centerBottomText = hovered.Label;
        }

        using (var valueFont = new Font("Segoe UI", 18f, FontStyle.Bold))
        using (var valueBrush = new SolidBrush(Color.FromArgb(28, 32, 40)))
        using (var labelFont = new Font("Segoe UI", 8f))
        using (var labelBrush = new SolidBrush(Color.FromArgb(140, 148, 162)))
        {
            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            g.DrawString(centerTopText, valueFont, valueBrush,
                new RectangleF(outerRect.X, outerRect.Y - 10,
                    outerRect.Width, outerRect.Height), format);

            g.DrawString(centerBottomText, labelFont, labelBrush,
                new RectangleF(outerRect.X, outerRect.Y + 22,
                    outerRect.Width, outerRect.Height), format);
        }

        // Legend — label, count, and share of total; hovered row highlights
        if (ShowLegend)
        {
            int lx = Width - legendWidth + 10;
            int ly = 26;

            using var legendLabelFont = new Font("Segoe UI", 8.5f);
            using var legendValueFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var legendPctFont = new Font("Segoe UI", 7.5f);

            for (int i = 0; i < Slices.Count; i++)
            {
                var slice = Slices[i];
                bool isHovered = i == _hoverIndex;
                double pct = slice.Value / total * 100.0;

                if (isHovered)
                {
                    using var hoverBg = new SolidBrush(Color.FromArgb(10, 0, 0, 0));
                    using var hoverPath = RoundedRect(new Rectangle(lx - 6, ly - 4, legendWidth - 14, 34), 8);
                    g.FillPath(hoverBg, hoverPath);
                }

                using (var dotBrush = new SolidBrush(slice.Color))
                    g.FillEllipse(dotBrush, lx, ly + 5, 10, 10);

                using (var labelBrush = new SolidBrush(Color.FromArgb(110, 118, 132)))
                    g.DrawString(slice.Label, legendLabelFont, labelBrush, lx + 18, ly);

                using (var valueBrush = new SolidBrush(Color.FromArgb(28, 32, 40)))
                    g.DrawString($"{slice.Value:N0}", legendValueFont, valueBrush, lx + 18, ly + 18);

                using (var pctBrush = new SolidBrush(Color.FromArgb(160, 168, 180)))
                    g.DrawString($"{pct:F0}%", legendPctFont, pctBrush, lx + legendWidth - 40, ly + 19);

                ly += 40;
            }
        }
    }

    private static Color Lighten(Color c, float amount)
    {
        int r = c.R + (int)((255 - c.R) * amount);
        int gr = c.G + (int)((255 - c.G) * amount);
        int b = c.B + (int)((255 - c.B) * amount);
        return Color.FromArgb(c.A, Math.Min(255, r), Math.Min(255, gr), Math.Min(255, b));
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