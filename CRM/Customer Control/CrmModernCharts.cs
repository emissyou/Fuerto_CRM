using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

/// <summary>
/// High-density Activity Column Chart matching the SaaS reference design:
/// - Dotted horizontal grid lines (Y-axis: 100, 200, 300, 400, 500)
/// - Interactive [Day] [Weekly] [Monthly] filter toggles
/// - Up to 30 high-density vertical bars with rounded top caps
/// - Interactive mouse hover highlighting individual bars with floating value tooltips
/// - Clickable navigation to target module
/// </summary>
public class CrmModernActivityChart : Control
{
    public class ActivityBar
    {
        public string Label { get; set; } = "";
        public double Value { get; set; }
        public string Subtitle { get; set; } = "";
    }

    public string Title { get; set; } = "Activity";
    public string TargetSection { get; set; } = "";
    public Action<string>? NavigateRequested { get; set; }

    public List<ActivityBar> DayData { get; set; } = new();
    public List<ActivityBar> WeekData { get; set; } = new();

    public int ActivePeriod { get; set; } = 0; // 0 = Day, 1 = Weekly

    public Color BarColor { get; set; } = Color.FromArgb(24, 144, 255);
    public Color BarHoverColor { get; set; } = Color.FromArgb(244, 63, 94);
    public Color GridColor { get; set; } = Color.FromArgb(241, 245, 249);
    public Color TextDark { get; set; } = Color.FromArgb(15, 23, 42);
    public Color TextMuted { get; set; } = Color.FromArgb(148, 163, 184);

    private readonly List<(RectangleF Rect, ActivityBar Bar, int Index)> _hitAreas = new();
    private int _hoverIndex = -1;
    private Point _mousePos;

    private Rectangle _btnDayRect;
    private Rectangle _btnWeekRect;

    public CrmModernActivityChart()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        BackColor = Color.White;
        Cursor = Cursors.Hand;
    }

    public void SetData(List<ActivityBar> dayData, List<ActivityBar> weekData)
    {
        DayData = dayData;
        WeekData = weekData;
        _hoverIndex = -1;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _mousePos = e.Location;

        int found = -1;
        foreach (var (r, _, idx) in _hitAreas)
        {
            if (r.Contains(e.Location))
            {
                found = idx;
                break;
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

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        if (_btnDayRect.Contains(e.Location))
        {
            ActivePeriod = 0;
            _hoverIndex = -1;
            Invalidate();
            return;
        }

        if (_btnWeekRect.Contains(e.Location))
        {
            ActivePeriod = 1;
            _hoverIndex = -1;
            Invalidate();
            return;
        }

        if (!string.IsNullOrWhiteSpace(TargetSection))
        {
            NavigateRequested?.Invoke(TargetSection);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        _hitAreas.Clear();
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Draw card border
        using (var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
        {
            g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
        }

        // Header Title
        using var fontTitle = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var brushTitle = new SolidBrush(TextDark);
        g.DrawString(Title, fontTitle, brushTitle, 16, 12);

        // [Day] [Weekly] toggle pills at top-right
        int toggleW = 110;
        int toggleH = 26;
        int toggleX = Width - toggleW - 16;
        int toggleY = 10;

        _btnDayRect = new Rectangle(toggleX, toggleY, 54, toggleH);
        _btnWeekRect = new Rectangle(toggleX + 56, toggleY, 54, toggleH);

        using (var pnlBg = new SolidBrush(Color.FromArgb(241, 245, 249)))
        {
            g.FillRectangle(pnlBg, toggleX, toggleY, toggleW, toggleH);
        }

        using var fontPill = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var pillActiveBg = new SolidBrush(Color.FromArgb(37, 99, 235));
        using var pillActiveFg = new SolidBrush(Color.White);
        using var pillInactiveFg = new SolidBrush(TextMuted);

        var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        if (ActivePeriod == 0)
        {
            g.FillRectangle(pillActiveBg, _btnDayRect);
            g.DrawString("Day", fontPill, pillActiveFg, _btnDayRect, sfCenter);
            g.DrawString("Weekly", fontPill, pillInactiveFg, _btnWeekRect, sfCenter);
        }
        else
        {
            g.FillRectangle(pillActiveBg, _btnWeekRect);
            g.DrawString("Day", fontPill, pillInactiveFg, _btnDayRect, sfCenter);
            g.DrawString("Weekly", fontPill, pillActiveFg, _btnWeekRect, sfCenter);
        }

        var activeBars = ActivePeriod == 0 ? DayData : WeekData;
        if (activeBars.Count == 0)
        {
            using var fontEmpty = new Font("Segoe UI", 9f);
            using var brushEmpty = new SolidBrush(TextMuted);
            g.DrawString("No activity recorded yet", fontEmpty, brushEmpty, Width / 2f - 70, Height / 2f);
            return;
        }

        int padLeft = 45;
        int padBottom = 26;
        int padTop = 48;
        int padRight = 16;

        int w = Width - padLeft - padRight;
        int h = Height - padTop - padBottom;
        if (w <= 0 || h <= 0) return;

        double maxVal = activeBars.Max(b => b.Value);
        if (maxVal <= 0) maxVal = 100;
        maxVal = Math.Ceiling(maxVal / 100.0) * 100.0;
        if (maxVal < 100) maxVal = 500;

        // Dotted gridlines
        using var gridPen = new Pen(GridColor, 1f) { DashStyle = DashStyle.Dash };
        using var axisFont = new Font("Segoe UI", 7.5f);
        using var textMutedB = new SolidBrush(TextMuted);

        int gridSteps = 5;
        for (int i = 0; i <= gridSteps; i++)
        {
            float yPos = padTop + (h * (i / (float)gridSteps));
            g.DrawLine(gridPen, padLeft, yPos, Width - padRight, yPos);

            double stepVal = maxVal * (1.0 - (i / (double)gridSteps));
            string label = stepVal >= 1000 ? $"{stepVal / 1000:N0}K" : $"{stepVal:N0}";
            g.DrawString(label, axisFont, textMutedB, 10, yPos - 7);
        }

        // Column bars
        int count = activeBars.Count;
        float gap = w / (float)count;
        float barW = Math.Max(4, gap - 3);

        using var normalBrush = new SolidBrush(BarColor);
        using var hoverBrush = new SolidBrush(BarHoverColor);

        RectangleF? hoveredRect = null;
        ActivityBar? hoveredBar = null;

        for (int i = 0; i < count; i++)
        {
            var item = activeBars[i];
            float bh = (float)(item.Value / maxVal * h);
            if (bh < 3) bh = 3;
            float bx = padLeft + i * gap + 1;
            float by = padTop + h - bh;

            var r = new RectangleF(bx, by, barW, bh);
            _hitAreas.Add((r, item, i));

            bool isHover = (i == _hoverIndex);
            if (isHover)
            {
                hoveredRect = r;
                hoveredBar = item;
                g.FillRectangle(hoverBrush, bx, by, barW, bh);
            }
            else
            {
                g.FillRectangle(normalBrush, bx, by, barW, bh);
            }
        }

        // Draw hover tooltip
        if (hoveredRect.HasValue && hoveredBar != null)
        {
            var r = hoveredRect.Value;
            string tipText = $"{hoveredBar.Label}: {hoveredBar.Value:N0}" +
                             (!string.IsNullOrWhiteSpace(hoveredBar.Subtitle) ? $"\n{hoveredBar.Subtitle}" : "");

            using var tipFont = new Font("Segoe UI", 8f, FontStyle.Bold);
            var tipSz = g.MeasureString(tipText, tipFont);
            float tipW = tipSz.Width + 16;
            float tipH = tipSz.Height + 10;
            float tipX = Math.Max(padLeft, Math.Min(Width - tipW - 10, r.X + r.Width / 2 - tipW / 2));
            float tipY = Math.Max(padTop - tipH, r.Y - tipH - 6);

            using (var tipBg = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (var tipBorder = new Pen(Color.FromArgb(51, 65, 85), 1))
            {
                g.FillRectangle(tipBg, tipX, tipY, tipW, tipH);
                g.DrawRectangle(tipBorder, tipX, tipY, tipW, tipH);
            }
            using var tipFg = new SolidBrush(Color.White);
            g.DrawString(tipText, tipFont, tipFg, tipX + 8, tipY + 5);
        }
    }
}

/// <summary>
/// Dual-Line Trajectory Chart with Node Points matching the reference design:
/// - Smooth Catmull-Rom or cubic curve lines
/// - Node points with hover rings
/// - Gradient fill below the primary curve
/// - Floating legend with colored indicators
/// - Rich interactive hover tooltip
/// </summary>
public class CrmModernTrajectoryChart : Control
{
    public class TrajectoryPoint
    {
        public string Month { get; set; } = "";
        public double Value1 { get; set; }
        public double Value2 { get; set; }
    }

    public string Title { get; set; } = "Trajectory";
    public string Series1Name { get; set; } = "Revenue";
    public string Series2Name { get; set; } = "Volume";
    public string Value1Prefix { get; set; } = "₱";
    public string Value2Suffix { get; set; } = "";
    public string TargetSection { get; set; } = "";
    public Action<string>? NavigateRequested { get; set; }

    public List<TrajectoryPoint> Data { get; set; } = new();

    public Color Color1 { get; set; } = Color.FromArgb(24, 144, 255);
    public Color Color2 { get; set; } = Color.FromArgb(244, 63, 94);
    public Color TextDark { get; set; } = Color.FromArgb(15, 23, 42);
    public Color TextMuted { get; set; } = Color.FromArgb(148, 163, 184);

    private int _hoverNodeIndex = -1;
    private readonly List<(PointF Pt1, PointF Pt2, TrajectoryPoint Data, int Index)> _nodeHits = new();

    public CrmModernTrajectoryChart()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        BackColor = Color.White;
        Cursor = Cursors.Hand;
    }

    public void SetData(List<TrajectoryPoint> data)
    {
        Data = data;
        _hoverNodeIndex = -1;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        int found = -1;
        foreach (var (p1, p2, _, idx) in _nodeHits)
        {
            double d1 = Math.Sqrt(Math.Pow(e.X - p1.X, 2) + Math.Pow(e.Y - p1.Y, 2));
            double d2 = Math.Sqrt(Math.Pow(e.X - p2.X, 2) + Math.Pow(e.Y - p2.Y, 2));
            if (d1 <= 14 || d2 <= 14)
            {
                found = idx;
                break;
            }
        }

        if (found != _hoverNodeIndex)
        {
            _hoverNodeIndex = found;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverNodeIndex != -1)
        {
            _hoverNodeIndex = -1;
            Invalidate();
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (!string.IsNullOrWhiteSpace(TargetSection))
            NavigateRequested?.Invoke(TargetSection);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        _nodeHits.Clear();
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Border
        using (var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

        // Header Title
        using var fontTitle = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var brushTitle = new SolidBrush(TextDark);
        g.DrawString(Title, fontTitle, brushTitle, 16, 12);

        // Legend at top-right
        int lx = Width - 210;
        using var fontLegend = new Font("Segoe UI", 8.25f, FontStyle.Bold);
        using var b1 = new SolidBrush(Color1);
        using var b2 = new SolidBrush(Color2);
        using var bText = new SolidBrush(TextMuted);

        g.FillEllipse(b1, lx, 16, 9, 9);
        g.DrawString(Series1Name, fontLegend, bText, lx + 14, 13);

        g.FillEllipse(b2, lx + 100, 16, 9, 9);
        g.DrawString(Series2Name, fontLegend, bText, lx + 114, 13);

        if (Data.Count < 2) return;

        int padLeft = 45;
        int padRight = 20;
        int padTop = 45;
        int padBottom = 30;

        int w = Width - padLeft - padRight;
        int h = Height - padTop - padBottom;
        if (w <= 0 || h <= 0) return;

        double max1 = Data.Max(d => d.Value1);
        double max2 = Data.Max(d => d.Value2);
        if (max1 <= 0) max1 = 1;
        if (max2 <= 0) max2 = 1;

        // Dotted gridlines
        using var gridPen = new Pen(Color.FromArgb(241, 245, 249), 1f) { DashStyle = DashStyle.Dash };
        using var axisFont = new Font("Segoe UI", 7.5f);
        using var axisBrush = new SolidBrush(TextMuted);

        for (int i = 0; i <= 4; i++)
        {
            float yPos = padTop + (h * (i / 4f));
            g.DrawLine(gridPen, padLeft, yPos, Width - padRight, yPos);

            double v = max1 * (1.0 - (i / 4.0));
            string lbl = v >= 1000000 ? $"{Value1Prefix}{v / 1000000:F1}M"
                       : v >= 1000 ? $"{Value1Prefix}{v / 1000:F0}K"
                       : $"{Value1Prefix}{v:F0}";
            g.DrawString(lbl, axisFont, axisBrush, 6, yPos - 7);
        }

        // Calculate points
        int n = Data.Count;
        var pts1 = new PointF[n];
        var pts2 = new PointF[n];

        for (int i = 0; i < n; i++)
        {
            float x = padLeft + (i / (float)(n - 1)) * w;
            float y1 = padTop + h - (float)(Data[i].Value1 / max1 * h);
            float y2 = padTop + h - (float)(Data[i].Value2 / max2 * h);

            pts1[i] = new PointF(x, y1);
            pts2[i] = new PointF(x, y2);
            _nodeHits.Add((pts1[i], pts2[i], Data[i], i));

            // X-axis label
            var sz = g.MeasureString(Data[i].Month, axisFont);
            g.DrawString(Data[i].Month, axisFont, axisBrush, x - sz.Width / 2f, padTop + h + 8);
        }

        // Soft gradient fill under Curve 1
        var fillPath = new GraphicsPath();
        fillPath.AddCurve(pts1, 0.4f);
        fillPath.AddLine(pts1[^1].X, padTop + h, pts1[0].X, padTop + h);
        fillPath.CloseFigure();

        using (var fillGrad = new LinearGradientBrush(
            new PointF(0, padTop), new PointF(0, padTop + h),
            Color.FromArgb(40, Color1.R, Color1.G, Color1.B),
            Color.FromArgb(0, Color1.R, Color1.G, Color1.B)))
        {
            g.FillPath(fillGrad, fillPath);
        }

        // Draw Lines
        using var pen1 = new Pen(Color1, 2.5f);
        using var pen2 = new Pen(Color2, 2f);
        g.DrawCurve(pen1, pts1, 0.4f);
        g.DrawCurve(pen2, pts2, 0.4f);

        // Draw Nodes
        using var bWhite = new SolidBrush(Color.White);
        for (int i = 0; i < n; i++)
        {
            bool isHover = (i == _hoverNodeIndex);

            // Node 1
            g.FillEllipse(b1, pts1[i].X - 4.5f, pts1[i].Y - 4.5f, 9f, 9f);
            g.FillEllipse(bWhite, pts1[i].X - 2.5f, pts1[i].Y - 2.5f, 5f, 5f);

            // Node 2
            g.FillEllipse(b2, pts2[i].X - 4f, pts2[i].Y - 4f, 8f, 8f);
            g.FillEllipse(bWhite, pts2[i].X - 2f, pts2[i].Y - 2f, 4f, 4f);

            if (isHover)
            {
                using var ringPen1 = new Pen(Color1, 2f);
                using var ringPen2 = new Pen(Color2, 2f);
                g.DrawEllipse(ringPen1, pts1[i].X - 7.5f, pts1[i].Y - 7.5f, 15f, 15f);
                g.DrawEllipse(ringPen2, pts2[i].X - 7f, pts2[i].Y - 7f, 14f, 14f);
            }
        }

        // Draw Tooltip
        if (_hoverNodeIndex >= 0 && _hoverNodeIndex < Data.Count)
        {
            var d = Data[_hoverNodeIndex];
            var pt = pts1[_hoverNodeIndex];

            string t1 = d.Value1 >= 1000 ? $"{Value1Prefix}{d.Value1:N0}" : $"{Value1Prefix}{d.Value1:N0}";
            string t2 = $"{d.Value2:N0}{Value2Suffix}";
            string tipText = $"{d.Month}\n{Series1Name}: {t1}\n{Series2Name}: {t2}";

            using var tipFont = new Font("Segoe UI", 8.25f, FontStyle.Bold);
            var tipSz = g.MeasureString(tipText, tipFont);
            float tipW = tipSz.Width + 18;
            float tipH = tipSz.Height + 12;

            float tipX = Math.Max(padLeft, Math.Min(Width - tipW - 10, pt.X - tipW / 2f));
            float tipY = Math.Max(padTop - 10, pt.Y - tipH - 12);

            using (var tipBg = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (var tipBorder = new Pen(Color.FromArgb(51, 65, 85), 1))
            {
                g.FillRectangle(tipBg, tipX, tipY, tipW, tipH);
                g.DrawRectangle(tipBorder, tipX, tipY, tipW, tipH);
            }
            using var tipFg = new SolidBrush(Color.White);
            g.DrawString(tipText, tipFont, tipFg, tipX + 9, tipY + 6);
        }
    }
}

/// <summary>
/// 4 Circular Gauge Progress Rings (01 - 04) matching the reference design:
/// - Circular progress gauge arcs (e.g. 25%, 50%, 75%, 100%)
/// - Numbered tags (01, 02, 03, 04)
/// - Subtitle / KPI label below each gauge
/// - Interactive hover highlights with tooltip
/// - Clickable navigation to target module
/// </summary>
public class CrmModernGaugeGroup : Control
{
    public class GaugeItem
    {
        public string NumberTag { get; set; } = "01";
        public string Title { get; set; } = "";
        public double Percentage { get; set; }
        public Color ArcColor { get; set; } = Color.FromArgb(24, 144, 255);
        public string TargetSection { get; set; } = "";
        public string ValueDetail { get; set; } = "";
    }

    public string Title { get; set; } = "Platform Capacity & Health";
    public Action<string>? NavigateRequested { get; set; }

    public List<GaugeItem> Gauges { get; set; } = new();

    private int _hoverGauge = -1;
    private readonly List<(RectangleF Rect, GaugeItem Item, int Index)> _gaugeHits = new();

    public CrmModernGaugeGroup()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        BackColor = Color.White;
        Cursor = Cursors.Hand;
    }

    public void SetGauges(List<GaugeItem> gauges)
    {
        Gauges = gauges;
        _hoverGauge = -1;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int found = -1;
        foreach (var (r, _, idx) in _gaugeHits)
        {
            if (r.Contains(e.Location))
            {
                found = idx;
                break;
            }
        }
        if (found != _hoverGauge)
        {
            _hoverGauge = found;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverGauge != -1)
        {
            _hoverGauge = -1;
            Invalidate();
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (_hoverGauge >= 0 && _hoverGauge < Gauges.Count)
        {
            var g = Gauges[_hoverGauge];
            if (!string.IsNullOrWhiteSpace(g.TargetSection))
                NavigateRequested?.Invoke(g.TargetSection);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        _gaugeHits.Clear();
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Border
        using (var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

        // Header Title
        using var fontTitle = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var brushTitle = new SolidBrush(Color.FromArgb(15, 23, 42));
        g.DrawString(Title, fontTitle, brushTitle, 16, 12);

        int count = Gauges.Count;
        if (count == 0) return;

        int padTop = 44;
        int padBottom = 16;
        int w = Width / count;
        int h = Height - padTop - padBottom;
        int ringSize = Math.Min(w - 24, h - 34);
        if (ringSize <= 0) return;

        using var fontPct = new Font("Segoe UI", 13f, FontStyle.Bold);
        using var fontTag = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", 8.25f, FontStyle.Bold);
        using var brushDark = new SolidBrush(Color.FromArgb(15, 23, 42));
        using var brushMuted = new SolidBrush(Color.FromArgb(148, 163, 184));
        using var trackPen = new Pen(Color.FromArgb(241, 245, 249), 10f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        for (int i = 0; i < count; i++)
        {
            var item = Gauges[i];
            float cx = i * w + w / 2f;
            float cy = padTop + ringSize / 2f + 4;
            var box = new RectangleF(cx - ringSize / 2f, cy - ringSize / 2f, ringSize, ringSize);
            _gaugeHits.Add((new RectangleF(i * w, padTop, w, h), item, i));

            bool isHover = (i == _hoverGauge);

            // Background arc (240 degrees)
            g.DrawArc(trackPen, box, 150, 240);

            // Active progress arc
            float sweep = (float)(item.Percentage / 100.0 * 240.0);
            if (sweep > 240) sweep = 240;
            if (sweep > 0)
            {
                using var progPen = new Pen(item.ArcColor, isHover ? 12f : 10f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawArc(progPen, box, 150, sweep);
            }

            // Tag number ("01") at top of ring
            g.DrawString(item.NumberTag, fontTag, brushMuted, cx, cy - ringSize / 4f - 2, sf);

            // Center percentage
            g.DrawString($"{item.Percentage:F0}%", fontPct, brushDark, cx, cy + 2, sf);

            // Subtitle label
            var subSz = g.MeasureString(item.Title, fontSub);
            float subY = cy + ringSize / 2f + 6;
            g.DrawString(item.Title, fontSub, isHover ? new SolidBrush(item.ArcColor) : brushDark, cx, subY, sf);
        }

        // Draw hover tooltip
        if (_hoverGauge >= 0 && _hoverGauge < Gauges.Count)
        {
            var item = Gauges[_hoverGauge];
            if (!string.IsNullOrWhiteSpace(item.ValueDetail))
            {
                string tip = $"{item.Title}: {item.ValueDetail} ({item.Percentage:F1}%)";
                using var tipFont = new Font("Segoe UI", 8.25f, FontStyle.Bold);
                var tipSz = g.MeasureString(tip, tipFont);
                float tipW = tipSz.Width + 18;
                float tipH = tipSz.Height + 10;
                float tipX = Math.Max(10, Math.Min(Width - tipW - 10, (_hoverGauge * w + w / 2f) - tipW / 2f));
                float tipY = padTop + ringSize + 12;

                using (var tipBg = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (var tipBorder = new Pen(Color.FromArgb(51, 65, 85), 1))
                {
                    g.FillRectangle(tipBg, tipX, tipY, tipW, tipH);
                    g.DrawRectangle(tipBorder, tipX, tipY, tipW, tipH);
                }
                using var tipFg = new SolidBrush(Color.White);
                g.DrawString(tip, tipFont, tipFg, tipX + 9, tipY + 5);
            }
        }
    }
}

/// <summary>
/// Segmented Dual-Donut Chart matching the reference design:
/// - Left Donut: Segmented ring with center currency callout and growth percentage
/// - Right Donut: Multi-segment ring with center total count callout
/// - Interactive hover on individual donut slices with tooltip
/// - Clickable navigation to target module
/// </summary>
public class CrmModernDualDonutChart : Control
{
    public class DonutSlice
    {
        public string Label { get; set; } = "";
        public double Value { get; set; }
        public Color Color { get; set; } = Color.FromArgb(24, 144, 255);
        public string Detail { get; set; } = "";
    }

    public string TitleLeft { get; set; } = "Revenue Distribution";
    public string TitleRight { get; set; } = "Portfolio Breakdown";
    public string CenterValueLeft { get; set; } = "₱0";
    public string CenterSubLeft { get; set; } = "+0.0%";
    public string CenterValueRight { get; set; } = "0";
    public string CenterSubRight { get; set; } = "Total";
    public string TargetSection { get; set; } = "";
    public Action<string>? NavigateRequested { get; set; }

    public List<DonutSlice> SlicesLeft { get; set; } = new();
    public List<DonutSlice> SlicesRight { get; set; } = new();

    private readonly List<(float StartAngle, float SweepAngle, DonutSlice Slice, bool IsLeft)> _sliceAngles = new();
    private int _hoverSliceIdx = -1;
    private Point _mousePos;

    public CrmModernDualDonutChart()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        BackColor = Color.White;
        Cursor = Cursors.Hand;
    }

    public void SetData(
        List<DonutSlice> left, string centerValLeft, string centerSubLeft,
        List<DonutSlice> right, string centerValRight, string centerSubRight)
    {
        SlicesLeft = left;
        CenterValueLeft = centerValLeft;
        CenterSubLeft = centerSubLeft;
        SlicesRight = right;
        CenterValueRight = centerValRight;
        CenterSubRight = centerSubRight;
        _hoverSliceIdx = -1;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _mousePos = e.Location;

        int halfW = Width / 2;
        int size = Math.Min(halfW - 24, Height - 60);
        if (size <= 0) return;

        int holeSize = (int)(size * 0.65f);

        int found = -1;
        for (int i = 0; i < _sliceAngles.Count; i++)
        {
            var s = _sliceAngles[i];
            float cx = s.IsLeft ? (halfW / 2f) : (halfW + halfW / 2f);
            float cy = 40 + size / 2f;

            float dx = e.X - cx;
            float dy = e.Y - cy;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist >= holeSize / 2f && dist <= size / 2f)
            {
                double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                if (angle < -90) angle += 360;

                double end = s.StartAngle + s.SweepAngle;
                if (angle >= s.StartAngle && angle < end)
                {
                    found = i;
                    break;
                }
            }
        }

        if (found != _hoverSliceIdx)
        {
            _hoverSliceIdx = found;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverSliceIdx != -1)
        {
            _hoverSliceIdx = -1;
            Invalidate();
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (!string.IsNullOrWhiteSpace(TargetSection))
            NavigateRequested?.Invoke(TargetSection);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        _sliceAngles.Clear();
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Border
        using (var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

        int halfW = Width / 2;
        int size = Math.Min(halfW - 30, Height - 60);
        if (size <= 0) return;

        // Header Titles
        using var fontTitle = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var brushTitle = new SolidBrush(Color.FromArgb(15, 23, 42));
        g.DrawString(TitleLeft, fontTitle, brushTitle, 16, 12);
        g.DrawString(TitleRight, fontTitle, brushTitle, halfW + 16, 12);

        // Divider
        using (var divPen = new Pen(Color.FromArgb(241, 245, 249), 1))
            g.DrawLine(divPen, halfW, 10, halfW, Height - 10);

        int holeSize = (int)(size * 0.65f);

        // Render Left Donut
        float cy = 42 + size / 2f;
        float cx1 = halfW / 2f;
        var r1 = new RectangleF(cx1 - size / 2f, cy - size / 2f, size, size);
        RenderDonut(g, r1, SlicesLeft, true, CenterValueLeft, CenterSubLeft);

        // Render Right Donut
        float cx2 = halfW + halfW / 2f;
        var r2 = new RectangleF(cx2 - size / 2f, cy - size / 2f, size, size);
        RenderDonut(g, r2, SlicesRight, false, CenterValueRight, CenterSubRight);

        // Draw hover tooltip
        if (_hoverSliceIdx >= 0 && _hoverSliceIdx < _sliceAngles.Count)
        {
            var sa = _sliceAngles[_hoverSliceIdx];
            string tip = $"{sa.Slice.Label}: {sa.Slice.Detail}";

            using var tipFont = new Font("Segoe UI", 8.25f, FontStyle.Bold);
            var tipSz = g.MeasureString(tip, tipFont);
            float tipW = tipSz.Width + 18;
            float tipH = tipSz.Height + 10;
            float tipX = Math.Max(10, Math.Min(Width - tipW - 10, _mousePos.X - tipW / 2f));
            float tipY = Math.Max(10, _mousePos.Y - tipH - 12);

            using (var tipBg = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (var tipBorder = new Pen(Color.FromArgb(51, 65, 85), 1))
            {
                g.FillRectangle(tipBg, tipX, tipY, tipW, tipH);
                g.DrawRectangle(tipBorder, tipX, tipY, tipW, tipH);
            }
            using var tipFg = new SolidBrush(Color.White);
            g.DrawString(tip, tipFont, tipFg, tipX + 9, tipY + 5);
        }
    }

    private void RenderDonut(
        Graphics g, RectangleF rect, List<DonutSlice> slices, bool isLeft,
        string centerVal, string centerSub)
    {
        if (slices.Count == 0) return;

        double total = slices.Sum(s => s.Value);
        if (total <= 0) total = 1;

        float startAngle = -90f;
        for (int i = 0; i < slices.Count; i++)
        {
            var slice = slices[i];
            float sweep = (float)(slice.Value / total * 360.0);
            if (sweep <= 0) continue;

            int globalIdx = _sliceAngles.Count;
            _sliceAngles.Add((startAngle, sweep, slice, isLeft));

            bool isHover = (globalIdx == _hoverSliceIdx);

            using (var b = new SolidBrush(slice.Color))
            {
                if (isHover)
                {
                    // Slightly expand slice
                    var expRect = new RectangleF(rect.X - 3, rect.Y - 3, rect.Width + 6, rect.Height + 6);
                    g.FillPie(b, expRect.X, expRect.Y, expRect.Width, expRect.Height, startAngle, sweep);
                }
                else
                {
                    g.FillPie(b, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweep);
                }
            }

            // White separator line
            using (var whitePen = new Pen(Color.White, 2f))
            {
                float rad = (float)(startAngle * Math.PI / 180.0);
                float cx = rect.X + rect.Width / 2f;
                float cy = rect.Y + rect.Height / 2f;
                g.DrawLine(whitePen, cx, cy, cx + (float)Math.Cos(rad) * rect.Width / 2f, cy + (float)Math.Sin(rad) * rect.Height / 2f);
            }

            startAngle += sweep;
        }

        // Cut out hole
        int holeSize = (int)(rect.Width * 0.65f);
        float hx = rect.X + (rect.Width - holeSize) / 2f;
        float hy = rect.Y + (rect.Height - holeSize) / 2f;

        using (var bgBrush = new SolidBrush(Color.White))
            g.FillEllipse(bgBrush, hx, hy, holeSize, holeSize);

        // Center labels
        using var fontVal = new Font("Segoe UI", holeSize * 0.18f, FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", holeSize * 0.10f, FontStyle.Bold);
        using var textDark = new SolidBrush(Color.FromArgb(15, 23, 42));
        using var textGreen = new SolidBrush(Color.FromArgb(16, 185, 129));
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        g.DrawString(centerVal, fontVal, textDark, hx + holeSize / 2f, hy + holeSize / 2f - 8, sf);
        g.DrawString(centerSub, fontSub, textGreen, hx + holeSize / 2f, hy + holeSize / 2f + 12, sf);
    }
}

/// <summary>
/// Grouped Dual-Color Column Chart with Sparklines matching reference design:
/// - Top dual sparkline cards with metrics and growth percentage badges (+34.4%)
/// - Bottom paired vertical columns (Coral Pink vs Electric Blue)
/// - Interactive hover highlights with tooltip
/// - Clickable navigation to target module
/// </summary>
public class CrmModernGroupedSparkChart : Control
{
    public class GroupedBarItem
    {
        public string Label { get; set; } = "";
        public double Value1 { get; set; }
        public double Value2 { get; set; }
        public string Detail { get; set; } = "";
    }

    public string Metric1Title { get; set; } = "Revenue";
    public string Metric1Value { get; set; } = "₱0";
    public string Metric1Delta { get; set; } = "+0.0%";
    public bool Metric1Positive { get; set; } = true;

    public string Metric2Title { get; set; } = "Pipeline";
    public string Metric2Value { get; set; } = "₱0";
    public string Metric2Delta { get; set; } = "+0.0%";
    public bool Metric2Positive { get; set; } = true;

    public string Series1Name { get; set; } = "Cost";
    public string Series2Name { get; set; } = "Revenue";

    public Color Color1 { get; set; } = Color.FromArgb(244, 63, 94); // Coral pink
    public Color Color2 { get; set; } = Color.FromArgb(24, 144, 255); // Electric blue

    public string TargetSection { get; set; } = "";
    public Action<string>? NavigateRequested { get; set; }

    public List<GroupedBarItem> Data { get; set; } = new();

    private readonly List<(RectangleF Rect1, RectangleF Rect2, GroupedBarItem Item, int Index)> _barHits = new();
    private int _hoverIndex = -1;
    private Point _mousePos;

    public CrmModernGroupedSparkChart()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        BackColor = Color.White;
        Cursor = Cursors.Hand;
    }

    public void SetData(
        string m1Title, string m1Val, string m1Delta, bool m1Pos,
        string m2Title, string m2Val, string m2Delta, bool m2Pos,
        List<GroupedBarItem> items)
    {
        Metric1Title = m1Title;
        Metric1Value = m1Val;
        Metric1Delta = m1Delta;
        Metric1Positive = m1Pos;
        Metric2Title = m2Title;
        Metric2Value = m2Val;
        Metric2Delta = m2Delta;
        Metric2Positive = m2Pos;
        Data = items;
        _hoverIndex = -1;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _mousePos = e.Location;

        int found = -1;
        for (int i = 0; i < _barHits.Count; i++)
        {
            var (r1, r2, _, idx) = _barHits[i];
            if (r1.Contains(e.Location) || r2.Contains(e.Location))
            {
                found = idx;
                break;
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

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (!string.IsNullOrWhiteSpace(TargetSection))
            NavigateRequested?.Invoke(TargetSection);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        _barHits.Clear();
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Card Border
        using (var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

        int halfW = Width / 2;

        // Top Row: 2 Sparkline Metric Badges
        RenderSparkBadge(g, new Rectangle(14, 12, halfW - 20, 52), Metric1Title, Metric1Value, Metric1Delta, Metric1Positive, Color2);
        RenderSparkBadge(g, new Rectangle(halfW + 6, 12, halfW - 20, 52), Metric2Title, Metric2Value, Metric2Delta, Metric2Positive, Color1);

        if (Data.Count == 0) return;

        int padLeft = 40;
        int padRight = 16;
        int padTop = 76;
        int padBottom = 26;

        int w = Width - padLeft - padRight;
        int h = Height - padTop - padBottom;
        if (w <= 0 || h <= 0) return;

        double maxVal = Data.Max(d => Math.Max(d.Value1, d.Value2));
        if (maxVal <= 0) maxVal = 100;
        maxVal = Math.Ceiling(maxVal / 100.0) * 100.0;

        // Dotted gridlines
        using var gridPen = new Pen(Color.FromArgb(241, 245, 249), 1f) { DashStyle = DashStyle.Dash };
        using var axisFont = new Font("Segoe UI", 7.5f);
        using var textMutedB = new SolidBrush(Color.FromArgb(148, 163, 184));

        for (int i = 0; i <= 3; i++)
        {
            float yPos = padTop + (h * (i / 3f));
            g.DrawLine(gridPen, padLeft, yPos, Width - padRight, yPos);

            double v = maxVal * (1.0 - (i / 3.0));
            string lbl = v >= 1000 ? $"{v / 1000:N0}K" : $"{v:N0}";
            g.DrawString(lbl, axisFont, textMutedB, 6, yPos - 7);
        }

        // Grouped bars
        int count = Data.Count;
        float groupSlot = w / (float)count;
        float barWidth = Math.Max(6, (groupSlot - 20) / 2f);

        using var b1 = new SolidBrush(Color1);
        using var b2 = new SolidBrush(Color2);

        for (int i = 0; i < count; i++)
        {
            var item = Data[i];
            float gx = padLeft + i * groupSlot + 8;

            float bh1 = (float)(item.Value1 / maxVal * h);
            float bh2 = (float)(item.Value2 / maxVal * h);
            if (bh1 < 2) bh1 = 2;
            if (bh2 < 2) bh2 = 2;

            var r1 = new RectangleF(gx, padTop + h - bh1, barWidth, bh1);
            var r2 = new RectangleF(gx + barWidth + 2, padTop + h - bh2, barWidth, bh2);

            _barHits.Add((r1, r2, item, i));

            bool isHover = (i == _hoverIndex);

            g.FillRectangle(b1, r1);
            g.FillRectangle(b2, r2);

            if (isHover)
            {
                using var ring = new Pen(Color.FromArgb(255, 168, 0), 1.5f);
                g.DrawRectangle(ring, r1.X, r1.Y, r1.Width, r1.Height);
                g.DrawRectangle(ring, r2.X, r2.Y, r2.Width, r2.Height);
            }

            // X Axis label
            var sz = g.MeasureString(item.Label, axisFont);
            g.DrawString(item.Label, axisFont, textMutedB, gx + barWidth - sz.Width / 2f, padTop + h + 8);
        }

        // Hover tooltip
        if (_hoverIndex >= 0 && _hoverIndex < Data.Count)
        {
            var item = Data[_hoverIndex];
            string tip = $"{item.Label}\n{Series1Name}: {item.Value1:N0}\n{Series2Name}: {item.Value2:N0}" +
                         (!string.IsNullOrWhiteSpace(item.Detail) ? $"\n{item.Detail}" : "");

            using var tipFont = new Font("Segoe UI", 8.25f, FontStyle.Bold);
            var tipSz = g.MeasureString(tip, tipFont);
            float tipW = tipSz.Width + 18;
            float tipH = tipSz.Height + 12;
            float tipX = Math.Max(10, Math.Min(Width - tipW - 10, _mousePos.X - tipW / 2f));
            float tipY = Math.Max(10, _mousePos.Y - tipH - 12);

            using (var tipBg = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (var tipBorder = new Pen(Color.FromArgb(51, 65, 85), 1))
            {
                g.FillRectangle(tipBg, tipX, tipY, tipW, tipH);
                g.DrawRectangle(tipBorder, tipX, tipY, tipW, tipH);
            }
            using var tipFg = new SolidBrush(Color.White);
            g.DrawString(tip, tipFont, tipFg, tipX + 9, tipY + 6);
        }
    }

    private void RenderSparkBadge(
        Graphics g, Rectangle rect, string title, string val, string delta, bool pos, Color sparkColor)
    {
        using (var bgBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
        using (var borderPen = new Pen(Color.FromArgb(241, 245, 249), 1))
        {
            g.FillRectangle(bgBrush, rect);
            g.DrawRectangle(borderPen, rect);
        }

        using var fontTitle = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var fontVal = new Font("Segoe UI", 12f, FontStyle.Bold);
        using var fontBadge = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var brushMuted = new SolidBrush(Color.FromArgb(148, 163, 184));
        using var brushDark = new SolidBrush(Color.FromArgb(15, 23, 42));

        g.DrawString(title.ToUpperInvariant(), fontTitle, brushMuted, rect.X + 10, rect.Y + 6);
        g.DrawString(val, fontVal, brushDark, rect.X + 10, rect.Y + 22);

        // Badge (+34.4%)
        var badgeSz = g.MeasureString(delta, fontBadge);
        float bx = rect.Right - badgeSz.Width - 14;
        float by = rect.Y + 8;
        var bRect = new RectangleF(bx, by, badgeSz.Width + 8, 18);

        using (var bBg = new SolidBrush(pos ? Color.FromArgb(240, 253, 244) : Color.FromArgb(254, 242, 242)))
        using (var bFg = new SolidBrush(pos ? Color.FromArgb(21, 128, 61) : Color.FromArgb(185, 28, 28)))
        {
            g.FillRectangle(bBg, bRect);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(delta, fontBadge, bFg, bRect, sf);
        }
    }
}

