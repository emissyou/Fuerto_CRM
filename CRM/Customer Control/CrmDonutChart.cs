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
    public int DonutThickness { get; set; } = 28;
    public bool ShowLegend { get; set; } = true;

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
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Slices.Count == 0) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int legendWidth = ShowLegend ? 140 : 0;
        int donutSize = Math.Min(Height - 30, Width - legendWidth - 30);
        if (donutSize <= 0) return;

        int cx = (Width - legendWidth - 30) / 2 + 15;
        int cy = Height / 2;

        var outerRect = new Rectangle(
            cx - donutSize / 2, cy - donutSize / 2,
            donutSize, donutSize);

        var innerRect = new Rectangle(
            outerRect.X + DonutThickness,
            outerRect.Y + DonutThickness,
            outerRect.Width - DonutThickness * 2,
            outerRect.Height - DonutThickness * 2);

        if (innerRect.Width <= 0 || innerRect.Height <= 0) return;

        double total = Slices.Sum(s => s.Value);
        if (total <= 0) total = 1;

        float startAngle = -90f;

        foreach (var slice in Slices)
        {
            float sweepAngle = (float)(slice.Value / total * 360.0);
            if (sweepAngle <= 0) continue;

            using var path = new GraphicsPath();
            path.AddArc(outerRect, startAngle, sweepAngle);
            path.AddArc(innerRect, startAngle + sweepAngle, -sweepAngle);
            path.CloseFigure();

            using var brush = new SolidBrush(slice.Color);
            g.FillPath(brush, path);

            startAngle += sweepAngle;
        }

        // Center label
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

            g.DrawString(CenterValue, valueFont, valueBrush,
                new RectangleF(outerRect.X, outerRect.Y - 10,
                    outerRect.Width, outerRect.Height), format);

            g.DrawString(CenterLabel, labelFont, labelBrush,
                new RectangleF(outerRect.X, outerRect.Y + 22,
                    outerRect.Width, outerRect.Height), format);
        }

        // Legend
        if (ShowLegend)
        {
            int lx = Width - legendWidth + 10;
            int ly = 30;

            using var legendLabelFont = new Font("Segoe UI", 8.5f);
            using var legendValueFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);

            foreach (var slice in Slices)
            {
                using (var dotBrush = new SolidBrush(slice.Color))
                    g.FillEllipse(dotBrush, lx, ly + 5, 10, 10);

                using (var labelBrush = new SolidBrush(Color.FromArgb(110, 118, 132)))
                    g.DrawString(slice.Label, legendLabelFont, labelBrush, lx + 18, ly);

                using (var valueBrush = new SolidBrush(Color.FromArgb(28, 32, 40)))
                    g.DrawString($"{slice.Value:N0}", legendValueFont, valueBrush,
                        lx + 18, ly + 18);

                ly += 40;
            }
        }
    }
}