using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

public class CrmBarChart : Control
{
    public class Series
    {
        public string Label { get; set; } = "";
        public Color Color { get; set; } = Color.Gray;
        public List<double> Values { get; set; } = new();
    }

    public List<string> Categories { get; set; } = new();
    public List<Series> SeriesList { get; set; } = new();
    public bool ShowLegend { get; set; } = true;
    public bool ShowGridLines { get; set; } = true;
    public int BarGap { get; set; } = 3;

    public CrmBarChart()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);

        BackColor = Color.Transparent;
    }

    public void SetData(List<string> categories, List<Series> series)
    {
        Categories = categories;
        SeriesList = series;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Categories.Count == 0 || SeriesList.Count == 0) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int leftPad = 45;
        int rightPad = 20;
        int topPad = ShowLegend ? 45 : 20;
        int bottomPad = 40;

        int chartW = Width - leftPad - rightPad;
        int chartH = Height - topPad - bottomPad;

        if (chartW <= 0 || chartH <= 0) return;

        double maxValue = SeriesList
            .SelectMany(s => s.Values)
            .DefaultIfEmpty(1)
            .Max();
        if (maxValue <= 0) maxValue = 1;

        maxValue = RoundUpNice(maxValue);

        // Grid + Y labels
        if (ShowGridLines)
        {
            using var gridPen = new Pen(Color.FromArgb(240, 242, 246));
            using var axisFont = new Font("Segoe UI", 7.5f);
            using var axisBrush = new SolidBrush(Color.FromArgb(160, 168, 180));

            for (int i = 0; i <= 4; i++)
            {
                int y = topPad + chartH - (int)(chartH * (i / 4.0));
                g.DrawLine(gridPen, leftPad, y, leftPad + chartW, y);

                var val = maxValue * i / 4.0;
                var txt = val >= 1_000_000 ? $"{val / 1_000_000:F1}M"
                        : val >= 1_000 ? $"{val / 1_000:F0}K"
                        : $"{val:F0}";
                g.DrawString(txt, axisFont, axisBrush, 6, y - 7);
            }
        }

        // Bars
        int groupCount = Categories.Count;
        int seriesCount = SeriesList.Count;

        float groupWidth = (float)chartW / groupCount;
        float barSlotWidth = (groupWidth - 20) / seriesCount;
        float barWidth = barSlotWidth - BarGap;

        using var labelFont = new Font("Segoe UI", 7.5f);
        using var labelBrush = new SolidBrush(Color.FromArgb(140, 148, 162));

        for (int i = 0; i < groupCount; i++)
        {
            float groupX = leftPad + i * groupWidth + 10;

            for (int s = 0; s < seriesCount; s++)
            {
                var series = SeriesList[s];
                if (i >= series.Values.Count) continue;

                double val = series.Values[i];
                float barH = (float)(val / maxValue * chartH);
                float x = groupX + s * barSlotWidth;
                float y = topPad + chartH - barH;

                using var path = RoundedBar(
                    new RectangleF(x, y, barWidth, barH),
                    Math.Min(4, (int)(barWidth / 2)));

                using var brush = new SolidBrush(series.Color);
                g.FillPath(brush, path);
            }

            string category = Categories[i];
            var catSize = g.MeasureString(category, labelFont);
            g.DrawString(category, labelFont, labelBrush,
                groupX + groupWidth / 2 - catSize.Width / 2 - 10,
                topPad + chartH + 12);
        }

        // Legend
        if (ShowLegend)
        {
            int lx = leftPad;
            int ly = 12;
            using var legendFont = new Font("Segoe UI", 8f);

            foreach (var series in SeriesList)
            {
                using (var dotBrush = new SolidBrush(series.Color))
                {
                    using var path = RoundedBar(new RectangleF(lx, ly + 2, 10, 10), 3);
                    g.FillPath(dotBrush, path);
                }

                using var legendBrush = new SolidBrush(Color.FromArgb(110, 118, 132));
                var size = g.MeasureString(series.Label, legendFont);
                g.DrawString(series.Label, legendFont, legendBrush, lx + 16, ly);

                lx += (int)size.Width + 30;
            }
        }
    }

    private static GraphicsPath RoundedBar(RectangleF bounds, int radius)
    {
        var path = new GraphicsPath();
        if (bounds.Height < 2) bounds.Height = 2;
        if (bounds.Width < 2) bounds.Width = 2;

        int d = radius * 2;
        if (d > bounds.Width) d = (int)bounds.Width;
        if (d > bounds.Height) d = (int)bounds.Height;

        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddLine(bounds.Right, bounds.Y + d / 2, bounds.Right, bounds.Bottom);
        path.AddLine(bounds.Right, bounds.Bottom, bounds.X, bounds.Bottom);
        path.AddLine(bounds.X, bounds.Bottom, bounds.X, bounds.Y + d / 2);
        path.CloseFigure();
        return path;
    }

    private static double RoundUpNice(double value)
    {
        if (value <= 0) return 1;
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
        double normalized = value / magnitude;
        double niceNormalized = normalized <= 1 ? 1
                              : normalized <= 2 ? 2
                              : normalized <= 5 ? 5
                              : 10;
        return niceNormalized * magnitude;
    }
}