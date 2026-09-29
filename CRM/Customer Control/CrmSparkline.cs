using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

public class CrmSparkline : Control
{
    private List<double> _values = new();

    public Color LineColor { get; set; } = Color.FromArgb(255, 168, 0);
    public Color FillColor { get; set; } = Color.FromArgb(255, 232, 180);
    public int LineThickness { get; set; } = 2;
    public bool ShowFill { get; set; } = true;
    public bool ShowDots { get; set; } = false;
    public bool HighlightLastPoint { get; set; } = true;

    public CrmSparkline()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.ResizeRedraw, true);

        BackColor = Color.White;   // ✅ SOLID — was Transparent
        Height = 30;
    }

    public void SetData(IEnumerable<double> values)
    {
        _values = values.ToList();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Color.White);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (_values.Count < 2) return;

        double max = _values.Max();
        double min = _values.Min();
        double range = max - min;
        if (range == 0) range = 1;

        float padding = LineThickness + 2;
        float w = Width - padding * 2;
        float h = Height - padding * 2;

        var points = new PointF[_values.Count];
        for (int i = 0; i < _values.Count; i++)
        {
            float x = padding + (i / (float)(_values.Count - 1)) * w;
            float y = padding + h - (float)((_values[i] - min) / range) * h;
            points[i] = new PointF(x, y);
        }

        const float tension = 0.35f;

        // Fill — smooth curve outline dropped down to the baseline
        if (ShowFill && points.Length > 1)
        {
            using var fillPath = new GraphicsPath();
            fillPath.AddCurve(points, tension);
            fillPath.AddLine(points[^1].X, Height - padding, points[0].X, Height - padding);
            fillPath.CloseFigure();

            using var fillBrush = new LinearGradientBrush(
                new PointF(0, 0),
                new PointF(0, Height),
                Color.FromArgb(130, FillColor),
                Color.FromArgb(0, FillColor));

            g.FillPath(fillBrush, fillPath);
        }

        // Line — smoothed via Catmull-Rom curve instead of straight segments
        using var linePen = new Pen(LineColor, LineThickness)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        g.DrawCurve(linePen, points, tension);

        // Dots
        if (ShowDots)
        {
            using var dotBrush = new SolidBrush(LineColor);
            foreach (var p in points)
                g.FillEllipse(dotBrush, p.X - 2, p.Y - 2, 4, 4);
        }

        // Highlighted end point — a small ringed dot draws the eye to "now"
        if (HighlightLastPoint)
        {
            var last = points[^1];
            using var ringBrush = new SolidBrush(Color.White);
            using var ringPen = new Pen(LineColor, 1.5f);
            using var coreBrush = new SolidBrush(LineColor);

            g.FillEllipse(ringBrush, last.X - 4, last.Y - 4, 8, 8);
            g.DrawEllipse(ringPen, last.X - 4, last.Y - 4, 8, 8);
            g.FillEllipse(coreBrush, last.X - 2, last.Y - 2, 4, 4);
        }
    }
}