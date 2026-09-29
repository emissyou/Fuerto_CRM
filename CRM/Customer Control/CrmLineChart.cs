using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

public class CrmLineChart : Control
{
    public class DataPoint
    {
        public string Label { get; set; } = "";
        public double Value { get; set; }
    }

    public List<DataPoint> Points { get; set; } = new();
    public Color LineColor { get; set; } = Color.FromArgb(124, 92, 252);
    public Color FillTopColor { get; set; } = Color.FromArgb(80, 124, 92, 252);
    public Color FillBottomColor { get; set; } = Color.FromArgb(0, 124, 92, 252);
    public Color GridColor { get; set; } = Color.FromArgb(240, 242, 248);
    public Color LabelColor { get; set; } = Color.FromArgb(138, 145, 166);
    public Color TooltipBg { get; set; } = Color.FromArgb(124, 92, 252);
    public Color TooltipText { get; set; } = Color.White;
    public Color DotColor { get; set; } = Color.White;

    public int SelectedIndex { get; set; } = -1;   // shows tooltip on this point
    public bool ShowTooltip { get; set; } = true;
    public bool ShowGrid { get; set; } = true;
    public int LineThickness { get; set; } = 3;
    public int PaddingLeft { get; set; } = 30;
    public int PaddingRight { get; set; } = 30;
    public int PaddingTop { get; set; } = 40;
    public int PaddingBottom { get; set; } = 40;

    public CrmLineChart()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.ResizeRedraw, true);

        BackColor = Color.White;
        Height = 220;
    }

    public void SetData(IEnumerable<DataPoint> points)
    {
        Points = points.ToList();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(BackColor);

        if (Points.Count < 2) return;

        int chartW = Width - PaddingLeft - PaddingRight;
        int chartH = Height - PaddingTop - PaddingBottom;

        if (chartW <= 0 || chartH <= 0) return;

        double max = Points.Max(p => p.Value);
        double min = Points.Min(p => p.Value);
        if (max == min) max = min + 1;

        // ---- Grid lines ----
        if (ShowGrid)
        {
            using var gridPen = new Pen(GridColor, 1);
            for (int i = 0; i <= 4; i++)
            {
                int yy = PaddingTop + (chartH * i / 4);
                g.DrawLine(gridPen, PaddingLeft, yy, PaddingLeft + chartW, yy);
            }
        }

        // ---- Compute points ----
        var pts = new PointF[Points.Count];
        for (int i = 0; i < Points.Count; i++)
        {
            float x = PaddingLeft + (i / (float)(Points.Count - 1)) * chartW;
            float y = PaddingTop + chartH -
                     (float)((Points[i].Value - min) / (max - min)) * chartH;
            pts[i] = new PointF(x, y);
        }

        // ---- Smooth curve (Catmull-Rom) ----
        var curve = GetSmoothCurve(pts);

        // ---- Gradient fill under curve ----
        var fillPts = new List<PointF>(curve);
        fillPts.Add(new PointF(curve.Last().X, PaddingTop + chartH));
        fillPts.Add(new PointF(curve.First().X, PaddingTop + chartH));

        using (var fillBrush = new LinearGradientBrush(
            new PointF(0, PaddingTop),
            new PointF(0, PaddingTop + chartH),
            FillTopColor,
            FillBottomColor))
        {
            g.FillPolygon(fillBrush, fillPts.ToArray());
        }

        // ---- Line ----
        using (var linePen = new Pen(LineColor, LineThickness)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        })
        {
            g.DrawLines(linePen, curve.ToArray());
        }

        // ---- X-axis labels ----
        using var labelFont = new Font("Segoe UI", 7.75f);
        using var labelBrush = new SolidBrush(LabelColor);

        for (int i = 0; i < Points.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(Points[i].Label)) continue;

            var textSize = g.MeasureString(Points[i].Label, labelFont);
            float textX = pts[i].X - textSize.Width / 2;
            float textY = PaddingTop + chartH + 12;

            // Clamp to bounds
            if (textX < 0) textX = 0;
            if (textX + textSize.Width > Width)
                textX = Width - textSize.Width;

            g.DrawString(Points[i].Label, labelFont, labelBrush, textX, textY);
        }

        // ---- Selected point marker + tooltip ----
        int selIdx = SelectedIndex >= 0 && SelectedIndex < Points.Count
            ? SelectedIndex
            : Points.Count - 1;

        var selPt = pts[selIdx];

        // Dashed vertical line
        using (var dashPen = new Pen(Color.FromArgb(124, 92, 252, 120), 1)
        {
            DashStyle = DashStyle.Dash
        })
        {
            g.DrawLine(dashPen, selPt.X, PaddingTop, selPt.X, PaddingTop + chartH);
        }

        // Dot
        using (var outerBrush = new SolidBrush(LineColor))
            g.FillEllipse(outerBrush, selPt.X - 7, selPt.Y - 7, 14, 14);
        using (var innerBrush = new SolidBrush(DotColor))
            g.FillEllipse(innerBrush, selPt.X - 4, selPt.Y - 4, 8, 8);

        // Tooltip
        if (ShowTooltip)
        {
            var valueText = $"${Points[selIdx].Value:N0}";
            using var tipFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            var textSize = g.MeasureString(valueText, tipFont);

            int tipW = (int)textSize.Width + 20;
            int tipH = (int)textSize.Height + 12;
            int tipX = (int)selPt.X - tipW / 2;
            int tipY = (int)selPt.Y - tipH - 16;

            // Clamp to control
            if (tipX < 4) tipX = 4;
            if (tipX + tipW > Width - 4) tipX = Width - tipW - 4;
            if (tipY < 4) tipY = 4;

            using var tipPath = RoundedRect(new Rectangle(tipX, tipY, tipW, tipH), 6);
            using var tipBrush = new SolidBrush(TooltipBg);
            g.FillPath(tipBrush, tipPath);

            using var tipTextBrush = new SolidBrush(TooltipText);
            g.DrawString(valueText, tipFont, tipTextBrush,
                tipX + 10, tipY + 6);
        }
    }

    private static List<PointF> GetSmoothCurve(PointF[] pts)
    {
        var result = new List<PointF>();

        if (pts.Length < 2) return pts.ToList();

        result.Add(pts[0]);

        for (int i = 0; i < pts.Length - 1; i++)
        {
            var p0 = i > 0 ? pts[i - 1] : pts[i];
            var p1 = pts[i];
            var p2 = pts[i + 1];
            var p3 = i + 2 < pts.Length ? pts[i + 2] : pts[i + 1];

            const int segments = 16;

            for (int t = 1; t <= segments; t++)
            {
                float s = t / (float)segments;
                float s2 = s * s;
                float s3 = s2 * s;

                float x = 0.5f * (
                    2 * p1.X +
                    (-p0.X + p2.X) * s +
                    (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * s2 +
                    (-p0.X + 3 * p1.X - 3 * p2.X + p3.X) * s3);

                float y = 0.5f * (
                    2 * p1.Y +
                    (-p0.Y + p2.Y) * s +
                    (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * s2 +
                    (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * s3);

                result.Add(new PointF(x, y));
            }
        }

        return result;
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