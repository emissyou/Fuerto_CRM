using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

public class CrmAvatar : Control
{
    public string Text { get; set; } = "?";
    public Color BackgroundColor { get; set; } = Color.FromArgb(255, 245, 225);
    public Color ForegroundColor { get; set; } = Color.FromArgb(160, 95, 0);
    public int FontSize { get; set; } = 11;
    public bool Border { get; set; } = false;

    private static readonly Color[] Palette =
    {
        Color.FromArgb(255, 245, 225),
        Color.FromArgb(232, 240, 254),
        Color.FromArgb(230, 248, 236),
        Color.FromArgb(254, 232, 232),
        Color.FromArgb(244, 236, 254),
        Color.FromArgb(255, 242, 232),
    };

    private static readonly Color[] TextPalette =
    {
        Color.FromArgb(160, 95, 0),
        Color.FromArgb(80, 140, 200),
        Color.FromArgb(34, 140, 78),
        Color.FromArgb(200, 55, 55),
        Color.FromArgb(140, 80, 190),
        Color.FromArgb(220, 120, 30),
    };

    public CrmAvatar()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);

        BackColor = Color.Transparent;
        Size = new Size(40, 40);
    }

    public void SetFromName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            Text = "?";
            return;
        }

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string initials;
        if (parts.Length >= 2)
            initials = $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
        else
            initials = parts[0].Length >= 2
                ? parts[0].Substring(0, 2).ToUpperInvariant()
                : parts[0].ToUpperInvariant();

        Text = initials;

        int hash = Math.Abs(fullName.GetHashCode());
        int idx = hash % Palette.Length;

        BackgroundColor = Palette[idx];
        ForegroundColor = TextPalette[idx];

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int diameter = Math.Min(Width, Height) - 4;
        if (diameter <= 0) return;

        int x = (Width - diameter) / 2;
        int y = (Height - diameter) / 2;

        using (var brush = new SolidBrush(BackgroundColor))
            g.FillEllipse(brush, x, y, diameter, diameter);

        if (Border)
        {
            using var pen = new Pen(Color.FromArgb(20, 0, 0, 0), 1);
            g.DrawEllipse(pen, x, y, diameter, diameter);
        }

        using var font = new Font("Segoe UI", FontSize, FontStyle.Bold);
        using var textBrush = new SolidBrush(ForegroundColor);

        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        g.DrawString(Text, font, textBrush,
            new RectangleF(x, y, diameter, diameter), format);
    }
}