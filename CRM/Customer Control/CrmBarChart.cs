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
    public bool ShowValueOnHover { get; set; } = true;
    public int BarGap { get; set; } = 3;

    private readonly List<(RectangleF Rect, double Value, int CategoryIndex, int SeriesIndex, Color Color)> _hitAreas = new();
    private int _hoverCategory = -1;
    private int _hoverSeries = -1;

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
        _hoverCategory = -1;
        _hoverSeries = -1;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        int catIdx = -1, serIdx = -1;
        foreach (var area in _hitAreas)
        {
            if (!area.Rect.Contains(e.Location)) continue;
            catIdx = area.CategoryIndex;
            serIdx = area.SeriesIndex;
            break;
        }

        if (catIdx != _hoverCategory || serIdx != _hoverSeries)
        {
            _hoverCategory = catIdx;
            _hoverSeries = serIdx;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverCategory != -1 || _hoverSeries != -1)
        {
            _hoverCategory = -1;
            _hoverSeries = -1;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        _hitAreas.Clear();

        if (Categories.Count == 0 || SeriesList.Count == 0) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int leftPad = 45;
        int rightPad = 20;
        int topPad = ShowLegend ? 45 : 26;
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

        // Grid + Y labels — light dashed lines read as more refined than solid rules
        if (ShowGridLines)
        {
            using var gridPen = new Pen(Color.FromArgb(235, 237, 242)) { DashStyle = DashStyle.Dash, DashPattern = new float[] { 3f, 3f } };
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
        using var labelFontBold = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var labelBrush = new SolidBrush(Color.FromArgb(140, 148, 162));
        using var labelBrushActive = new SolidBrush(Color.FromArgb(28, 32, 40));

        RectangleF? hoverRect = null;
        double hoverValue = 0;
        Color hoverColor = Color.Gray;

        for (int i = 0; i < groupCount; i++)
        {
            float groupX = leftPad + i * groupWidth + 10;
            bool groupHovered = i == _hoverCategory;

            for (int s = 0; s < seriesCount; s++)
            {
                var series = SeriesList[s];
                if (i >= series.Values.Count) continue;

                double val = series.Values[i];
                float barH = (float)(val / maxValue * chartH);
                float x = groupX + s * barSlotWidth;
                float y = topPad + chartH - barH;
                var barRect = new RectangleF(x, y, barWidth, barH);

                bool isHovered = groupHovered && s == _hoverSeries;

                using var path = RoundedBar(barRect, Math.Min(4, (int)(barWidth / 2)));

                // Subtle top-to-bottom gradient for depth, brightened further on hover
                var topColor = Lighten(series.Color, isHovered ? 0.45f : 0.25f);
                using (var brush = new LinearGradientBrush(
                    new PointF(x, y), new PointF(x, y + Math.Max(barH, 1)),
                    topColor, series.Color))
                {
                    g.FillPath(brush, path);
                }

                if (isHovered)
                {
                    using var hoverPen = new Pen(Color.FromArgb(255, 168, 0), 1.5f);
                    g.DrawPath(hoverPen, path);
                    hoverRect = barRect;
                    hoverValue = val;
                    hoverColor = series.Color;
                }

                _hitAreas.Add((barRect, val, i, s, series.Color));
            }

            string category = Categories[i];
            var catSize = g.MeasureString(category, labelFont);
            g.DrawString(category, groupHovered ? labelFontBold : labelFont, groupHovered ? labelBrushActive : labelBrush,
                groupX + groupWidth / 2 - catSize.Width / 2 - 10,
                topPad + chartH + 12);
        }

        // Hover tooltip — value pill floating above the active bar
        if (ShowValueOnHover && hoverRect.HasValue)
        {
            var val = hoverValue;
            var txt = val >= 1_000_000 ? $"{val / 1_000_000:F1}M"
                    : val >= 1_000 ? $"{val / 1_000:F1}K"
                    : $"{val:N0}";

            using var tipFont = new Font("Segoe UI", 8f, FontStyle.Bold);
            var tipSize = g.MeasureString(txt, tipFont);
            int tipW = (int)tipSize.Width + 16;
            int tipH = (int)tipSize.Height + 8;
            var r = hoverRect.Value;
            float tipX = r.X + r.Width / 2 - tipW / 2;
            float tipY = Math.Max(2, r.Y - tipH - 8);

            using (var tipPath = RoundedBar(new RectangleF(tipX, tipY, tipW, tipH), 6))
            using (var tipBg = new SolidBrush(Color.FromArgb(28, 32, 40)))
            {
                g.FillPath(tipBg, tipPath);
            }
            using var tipTextBrush = new SolidBrush(Color.White);
            g.DrawString(txt, tipFont, tipTextBrush, tipX + 8, tipY + 4);
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

    private static Color Lighten(Color c, float amount)
    {
        int r = c.R + (int)((255 - c.R) * amount);
        int gr = c.G + (int)((255 - c.G) * amount);
        int b = c.B + (int)((255 - c.B) * amount);
        return Color.FromArgb(c.A, Math.Min(255, r), Math.Min(255, gr), Math.Min(255, b));
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