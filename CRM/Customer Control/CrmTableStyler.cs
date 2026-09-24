using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

public static class CrmTableStyler
{
    // ---- Palette ----
    public static readonly Color ColorCard = Color.White;
    public static readonly Color ColorRowHover = Color.FromArgb(255, 252, 245);
    public static readonly Color ColorRowSelected = Color.FromArgb(255, 245, 225);
    public static readonly Color ColorBorder = Color.FromArgb(232, 235, 240);
    public static readonly Color ColorTextPri = Color.FromArgb(28, 32, 40);
    public static readonly Color ColorTextSec = Color.FromArgb(140, 148, 162);
    public static readonly Color ColorHeaderBg = Color.FromArgb(248, 250, 253);
    public static readonly Color ColorGridBg = Color.FromArgb(245, 247, 250);

    /// <summary>
    /// Apply modern card-row styling to a DataGridView.
    /// Rows become rounded cards with 8px gap.
    /// </summary>
    public static void Apply(DataGridView grid, params string[] pillColumns)
    {
        // =========================================================
        // CORE SETUP
        // =========================================================
        grid.BackgroundColor = ColorGridBg;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
        grid.GridColor = ColorGridBg;   // ✅ matches background — appears invisible (was Color.Transparent, which crashes)
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 40;
        grid.ScrollBars = ScrollBars.Vertical;
        grid.DoubleBuffered(true);
        grid.Padding = new Padding(0, 0, 0, 8);
        grid.RowTemplate.Height = 64;

        // =========================================================
        // HEADER STYLE
        // =========================================================
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorHeaderBg,
            ForeColor = ColorTextSec,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            Padding = new Padding(16, 0, 16, 0),
            SelectionBackColor = ColorHeaderBg,
            SelectionForeColor = ColorTextSec,
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        // =========================================================
        // ROW STYLE
        // =========================================================
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCard,
            ForeColor = ColorTextPri,
            Font = new Font("Segoe UI", 9.5f),
            Padding = new Padding(16, 8, 16, 8),
            SelectionBackColor = ColorRowSelected,
            SelectionForeColor = ColorTextPri,
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCard
        };

        // =========================================================
        // PILL COLUMNS
        // =========================================================
        var pillSet = new HashSet<string>(
            pillColumns ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        // =========================================================
        // ROW PRE-PAINT — card rows
        // =========================================================
        grid.RowPrePaint += (s, e) =>
        {
            if (e.RowIndex < 0) return;

            var row = grid.Rows[e.RowIndex];
            var isSelected = row.Selected;
            var isHovered = row.Tag?.ToString() == "hover";

            var g = e.Graphics!;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rowRect = grid.GetRowDisplayRectangle(e.RowIndex, false);
            var cardRect = new Rectangle(
                rowRect.X + 4,
                rowRect.Y + 4,
                rowRect.Width - 8,
                rowRect.Height - 8);

            if (cardRect.Width <= 0 || cardRect.Height <= 0) return;

            var bgColor = isSelected ? ColorRowSelected
                        : isHovered ? ColorRowHover
                        : ColorCard;

            using (var path = RoundedRect(cardRect, 10))
            using (var bgBrush = new SolidBrush(bgColor))
            {
                g.FillPath(bgBrush, path);
            }

            using (var path = RoundedRect(cardRect, 10))
            using (var pen = new Pen(isSelected
                ? Color.FromArgb(255, 200, 100)
                : ColorBorder, 1))
            {
                g.DrawPath(pen, path);
            }
        };

        // =========================================================
        // HOVER TRACKING
        // =========================================================
        grid.CellMouseEnter += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count) return;
            grid.Rows[e.RowIndex].Tag = "hover";
            grid.InvalidateRow(e.RowIndex);
        };

        grid.CellMouseLeave += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count) return;
            grid.Rows[e.RowIndex].Tag = null;
            grid.InvalidateRow(e.RowIndex);
        };

        // =========================================================
        // PILL RENDERING
        // =========================================================
        grid.CellPainting += (s, e) =>
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex < 0) return;

            var colName = grid.Columns[e.ColumnIndex].Name;
            if (!pillSet.Contains(colName)) return;

            var raw = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
            if (string.IsNullOrWhiteSpace(raw)) return;

            e.PaintBackground(e.CellBounds, true);
            e.Handled = true;

            var (bg, fg) = GetPillColors(raw);

            var text = raw.Trim();
            var g = e.Graphics!;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var font = new Font("Segoe UI", 8f, FontStyle.Bold);
            var textSize = g.MeasureString(text, font);

            int padX = 10;
            int padY = 4;
            int pillW = (int)textSize.Width + padX * 2;
            int pillH = (int)textSize.Height + padY * 2;
            int pillX = e.CellBounds.Left + 16;
            int pillY = e.CellBounds.Top + (e.CellBounds.Height - pillH) / 2;

            using (var path = RoundedRect(new Rectangle(pillX, pillY, pillW, pillH), pillH / 2))
            using (var brush = new SolidBrush(bg))
            {
                g.FillPath(brush, path);
            }

            using (var textBrush = new SolidBrush(fg))
            {
                g.DrawString(text, font, textBrush, pillX + padX, pillY + padY);
            }
        };

        // =========================================================
        // INVALIDATE ON SELECTION CHANGE
        // =========================================================
        grid.SelectionChanged += (_, _) =>
        {
            grid.Invalidate();
        };
    }

    /// <summary>
    /// Map a status/priority value to a pill color pair.
    /// </summary>
    public static (Color Bg, Color Fg) GetPillColors(string value)
    {
        var v = value.Trim().ToLowerInvariant();

        return v switch
        {
            // Success — green
            "completed" or "active" or "approved" or "fullypaid" or "resolved"
                or "in review" or "inreview" or "accepted" or "champion"
                or "designerassigned" or "quotationissued" or "quotationaccepted"
                or "depositreceived" or "converted"
                => (Color.FromArgb(220, 245, 228), Color.FromArgb(30, 120, 65)),

            // Warning — amber
            "in progress" or "inprogress" or "pending" or "planning"
                or "promising" or "loyal" or "contacted" or "qualified"
                or "partialpaid" or "partiallypaid"
                => (Color.FromArgb(255, 245, 220), Color.FromArgb(180, 110, 0)),

            // Danger — red
            "open" or "rejected" or "critical" or "detractor" or "high"
                or "cancelled" or "expired"
                => (Color.FromArgb(254, 232, 232), Color.FromArgb(200, 55, 55)),

            // Info — blue
            "issued" or "new" or "medium" or "at risk" or "atrisk"
                or "dormant"
                => (Color.FromArgb(230, 240, 254), Color.FromArgb(60, 110, 180)),

            // Neutral — gray
            "draft" or "closed" or "lost" or "inactive" or "low"
                => (Color.FromArgb(238, 240, 244), Color.FromArgb(120, 128, 140)),

            // Purple
            "adjustment" or "rework"
                => (Color.FromArgb(244, 236, 254), Color.FromArgb(120, 70, 180)),

            // Default
            _ => (Color.FromArgb(232, 235, 240), Color.FromArgb(70, 78, 92))
        };
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

/// <summary>
/// Enable double-buffering on a DataGridView.
/// </summary>
public static class DataGridViewExtensions
{
    public static void DoubleBuffered(this DataGridView dgv, bool setting)
    {
        var prop = typeof(DataGridView).GetProperty(
            "DoubleBuffered",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        prop?.SetValue(dgv, setting, null);
    }
}