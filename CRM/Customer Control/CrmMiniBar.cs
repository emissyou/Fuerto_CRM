namespace CRM_DesignServices.winforms;

public class CrmMiniBar : Control
{
    public List<double> Values { get; set; } = new();
    public Color BarColor { get; set; } = Color.FromArgb(255, 168, 0);
    public Color BarColorActive { get; set; } = Color.FromArgb(255, 168, 0);
    public int BarWidth { get; set; } = 6;
    public int BarGap { get; set; } = 3;
    public int ActiveIndex { get; set; } = -1;

    public CrmMiniBar()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.ResizeRedraw, true);

        BackColor = Color.White;
        Size = new Size(60, 34);
    }

    public void SetData(IEnumerable<double> values)
    {
        Values = values.ToList();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Values.Count == 0) return;

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        double max = Values.Max();
        if (max <= 0) max = 1;

        int totalWidth = Values.Count * BarWidth + (Values.Count - 1) * BarGap;
        int startX = (Width - totalWidth) / 2;
        int baseY = Height;

        for (int i = 0; i < Values.Count; i++)
        {
            int h = (int)(Values[i] / max * (Height - 4));
            if (h < 2) h = 2;

            int x = startX + i * (BarWidth + BarGap);
            int y = baseY - h;

            var color = (ActiveIndex == i || ActiveIndex == -1)
                ? BarColorActive
                : Color.FromArgb(60, BarColor.R, BarColor.G, BarColor.B);

            using var brush = new SolidBrush(color);
            using var path = RoundedBar(new Rectangle(x, y, BarWidth, h), 2);
            g.FillPath(brush, path);
        }
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedBar(
        Rectangle bounds, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
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