using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

/// <summary>
/// Modern SaaS Data Table Styler matching the reference image specification:
/// - Pure white background with generous 54px row height
/// - Modern headers with column sorting carets (⌃) and fine bottom border
/// - Circular vibrant initials avatars for name/title columns with leading state indicators
/// - Clean uppercase tracking for roles and positions
/// - Status indicator colored dots (●) and pill badges
/// - Location pin icons (📍), paperclip attachment icons (📎), and favorite stars (★/☆)
/// - Soft blue selection highlight (#F0F9FF) with electric blue primary text (#0284C7)
/// - Rounded selection checkboxes (Select) on the right
/// </summary>
public static class CrmTableStyler
{
    // ---- Modern SaaS Palette matching the image ----
    public static readonly Color ColorCard = Color.White;
    public static readonly Color ColorRowHover = Color.FromArgb(248, 250, 252);
    public static readonly Color ColorRowSelected = Color.FromArgb(240, 249, 255);
    public static readonly Color ColorSelectedText = Color.FromArgb(2, 132, 199);
    public static readonly Color ColorBorder = Color.FromArgb(241, 245, 249);
    public static readonly Color ColorHeaderBorder = Color.FromArgb(241, 245, 249);
    public static readonly Color ColorTextPri = Color.FromArgb(15, 23, 42);
    public static readonly Color ColorTextSec = Color.FromArgb(100, 116, 139);
    public static readonly Color ColorTextMuted = Color.FromArgb(148, 163, 184);
    public static readonly Color ColorHeaderBg = Color.White;
    public static readonly Color ColorHeaderFg = Color.FromArgb(51, 65, 85);
    public static readonly Color ColorAccent = Color.FromArgb(2, 132, 199);

    private static readonly Color[] AvatarPalette =
    {
        Color.FromArgb(6, 182, 212),   // Cyan (like "DD" in mockup)
        Color.FromArgb(14, 165, 233),  // Sky Blue (like "DE" in mockup)
        Color.FromArgb(236, 72, 153),  // Pink (like "EA" in mockup)
        Color.FromArgb(139, 92, 246),  // Purple
        Color.FromArgb(16, 185, 129),  // Emerald Mint
        Color.FromArgb(245, 158, 11)   // Amber Gold
    };

    /// <summary>
    /// Apply clean, modern SaaS table styling matching the reference UI mockup.
    /// </summary>
    public static void Apply(DataGridView grid, params string[] pillColumns)
    {
        // =========================================================
        // CORE SETUP
        // =========================================================
        grid.BackgroundColor = ColorCard;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
        grid.GridColor = ColorBorder;
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AllowUserToResizeColumns = true;
        grid.ReadOnly = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 46;
        grid.ScrollBars = ScrollBars.Both;
        grid.DoubleBuffered(true);
        grid.Padding = Padding.Empty;
        grid.RowTemplate.Height = 54;
        grid.Cursor = Cursors.Hand;

        // =========================================================
        // HEADER STYLE
        // =========================================================
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorHeaderBg,
            ForeColor = ColorHeaderFg,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Padding = new Padding(12, 0, 12, 0),
            SelectionBackColor = ColorHeaderBg,
            SelectionForeColor = ColorHeaderFg,
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        // =========================================================
        // ROW STYLE
        // =========================================================
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCard,
            ForeColor = ColorTextPri,
            Font = new Font("Segoe UI", 9.25f),
            Padding = new Padding(12, 2, 12, 2),
            SelectionBackColor = ColorRowSelected,
            SelectionForeColor = ColorSelectedText,
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCard
        };

        var pillSet = new HashSet<string>(
            pillColumns ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        // Track hovered row without modifying row.Tag
        int hoveredRowIndex = -1;

        // =========================================================
        // CELL PAINTING — Beautiful GDI+ rendering matching reference
        // =========================================================
        grid.CellPainting += (s, e) =>
        {
            if (e.ColumnIndex < 0) return;
            var g = e.Graphics!;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var colName = grid.Columns[e.ColumnIndex].Name.ToLowerInvariant();
            var colHeader = grid.Columns[e.ColumnIndex].HeaderText;

            // 1. COLUMN HEADER PAINTING
            if (e.RowIndex == -1)
            {
                using var hBrush = new SolidBrush(ColorHeaderBg);
                g.FillRectangle(hBrush, e.CellBounds);

                string headerText = colHeader;
                if (!headerText.EndsWith("⌃") && !headerText.EndsWith("⌵"))
                {
                    headerText = (colName.Contains("select") || colName.Contains("chk"))
                        ? $"{headerText} ⌵"
                        : $"{headerText} ⌃";
                }

                using var hFont = new Font("Segoe UI", 8.25f, FontStyle.Bold);
                using var hTextBrush = new SolidBrush(ColorHeaderFg);
                using var caretBrush = new SolidBrush(ColorTextMuted);

                var sz = g.MeasureString(headerText, hFont);
                float tx = e.CellBounds.Left + 12;
                float ty = e.CellBounds.Top + (e.CellBounds.Height - sz.Height) / 2f;
                g.DrawString(headerText, hFont, hTextBrush, tx, ty);

                // Subtle bottom header divider line
                using var hPen = new Pen(ColorHeaderBorder, 1.25f);
                g.DrawLine(hPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);

                e.Handled = true;
                return;
            }

            // 2. DATA ROW CELL PAINTING
            if (e.RowIndex >= 0 && e.RowIndex < grid.Rows.Count)
            {
                var row = grid.Rows[e.RowIndex];
                bool isSelected = row.Selected;
                bool isHovered = (e.RowIndex == hoveredRowIndex);

                Color bgColor = isSelected ? ColorRowSelected
                              : isHovered ? ColorRowHover
                              : ColorCard;

                using (var bgBrush = new SolidBrush(bgColor))
                {
                    g.FillRectangle(bgBrush, e.CellBounds);
                }

                // 1px subtle row divider line
                using (var divPen = new Pen(ColorBorder, 1f))
                {
                    g.DrawLine(divPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                }

                // Accent rail on selected row (Column 0 only)
                if (isSelected && e.ColumnIndex == 0)
                {
                    using var railBrush = new SolidBrush(ColorAccent);
                    g.FillRectangle(railBrush, e.CellBounds.Left, e.CellBounds.Top, 3, e.CellBounds.Height - 1);
                }

                var raw = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";
                var clean = raw.Trim();

                // -------------------------------------------------------------
                // A. PRIMARY NAME / AVATAR COLUMN (Resolved dynamically)
                // -------------------------------------------------------------
                int avatarColIdx = GetAvatarColumnIndex(grid);
                bool isNameCol = (e.ColumnIndex == avatarColIdx);

                if (isNameCol && !string.IsNullOrWhiteSpace(clean))
                {
                    // 1. Leading state arrow icon (↗)
                    using var iconFont = new Font("Segoe UI", 9f);
                    using var iconBrush = new SolidBrush(ColorTextMuted);
                    g.DrawString("↗", iconFont, iconBrush, e.CellBounds.Left + 8, e.CellBounds.Top + (e.CellBounds.Height - 14) / 2f);

                    // 2. Circular Avatar with initials
                    string initials = GetInitials(clean);
                    int avatarColorIdx = Math.Abs(clean.GetHashCode()) % AvatarPalette.Length;
                    var avColor = AvatarPalette[avatarColorIdx];

                    int avSize = 32;
                    float avX = e.CellBounds.Left + 26;
                    float avY = e.CellBounds.Top + (e.CellBounds.Height - avSize) / 2f;

                    using (var avBrush = new SolidBrush(avColor))
                    {
                        g.FillEllipse(avBrush, avX, avY, avSize, avSize);
                    }

                    using var initFont = new Font("Segoe UI", 8.25f, FontStyle.Bold);
                    using var initBrush = new SolidBrush(Color.White);
                    var initSz = g.MeasureString(initials, initFont);
                    g.DrawString(initials, initFont, initBrush,
                        avX + (avSize - initSz.Width) / 2f,
                        avY + (avSize - initSz.Height) / 2f);

                    // 3. Name Label
                    using var nameFont = new Font("Segoe UI", 9.25f, FontStyle.Bold);
                    using var nameBrush = new SolidBrush((isSelected || isHovered) ? ColorSelectedText : ColorTextPri);
                    float nameX = avX + avSize + 10;
                    float nameY = e.CellBounds.Top + (e.CellBounds.Height - 16) / 2f;
                    g.DrawString(clean, nameFont, nameBrush, nameX, nameY);

                    e.Handled = true;
                    return;
                }

                // -------------------------------------------------------------
                // B. SELECT CHECKBOX COLUMN (Rightmost or named Select/Check)
                // -------------------------------------------------------------
                bool isSelectCol = colName.Contains("select") || colName.Contains("chk") || colName == "checkbox";
                if (isSelectCol)
                {
                    int chkSize = 18;
                    float chkX = e.CellBounds.Left + (e.CellBounds.Width - chkSize) / 2f;
                    float chkY = e.CellBounds.Top + (e.CellBounds.Height - chkSize) / 2f;
                    var chkRect = new Rectangle((int)chkX, (int)chkY, chkSize, chkSize);

                    using var chkPath = RoundedRect(chkRect, 4);
                    if (isSelected)
                    {
                        using var bActive = new SolidBrush(ColorAccent);
                        g.FillPath(bActive, chkPath);
                        using var whitePen = new Pen(Color.White, 2f);
                        g.DrawLines(whitePen, new[]
                        {
                            new PointF(chkX + 4, chkY + 9),
                            new PointF(chkX + 7.5f, chkY + 13),
                            new PointF(chkX + 13.5f, chkY + 5)
                        });
                    }
                    else
                    {
                        using var bBorder = new Pen(Color.FromArgb(203, 213, 225), 1.25f);
                        using var bWhite = new SolidBrush(Color.White);
                        g.FillPath(bWhite, chkPath);
                        g.DrawPath(bBorder, chkPath);
                    }

                    e.Handled = true;
                    return;
                }

                // -------------------------------------------------------------
                // C. STATUS COLUMN (Dot Indicator + Pill or Clean Label)
                // -------------------------------------------------------------
                bool isStatusCol = pillSet.Contains(colName) ||
                                   colName.Contains("status") || colName.Contains("state") ||
                                   colName.Contains("segment") || colName.Contains("isactive");

                if (isStatusCol && !string.IsNullOrWhiteSpace(clean))
                {
                    var (pillBg, pillFg) = GetPillColors(clean);

                    const int dotSize = 8;
                    float dotX = e.CellBounds.Left + 14;
                    float dotY = e.CellBounds.Top + (e.CellBounds.Height - dotSize) / 2f;

                    // Draw colored dot (●) matching reference image
                    using (var dotBrush = new SolidBrush(pillFg))
                    {
                        g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
                    }

                    // Draw label text next to dot
                    using var statusFont = new Font("Segoe UI", 9f);
                    using var statusTextBrush = new SolidBrush(ColorTextPri);
                    float txtX = dotX + dotSize + 8;
                    float txtY = e.CellBounds.Top + (e.CellBounds.Height - 16) / 2f;
                    g.DrawString(clean, statusFont, statusTextBrush, txtX, txtY);

                    e.Handled = true;
                    return;
                }

                // -------------------------------------------------------------
                // D. STAR / RATING / FAVORITE COLUMN
                // -------------------------------------------------------------
                bool isStarCol = colName.Contains("star") || colName.Contains("rating") || colName.Contains("score");
                if (isStarCol && !string.IsNullOrWhiteSpace(clean))
                {
                    bool hasStar = clean.Contains("★") || clean.Contains("4") || clean.Contains("5") || clean.ToLowerInvariant().Contains("high");
                    using var starFont = new Font("Segoe UI", 11f);
                    using var starBrush = new SolidBrush(hasStar ? Color.FromArgb(244, 63, 94) : Color.FromArgb(203, 213, 225));
                    string starChar = hasStar ? "★" : "☆";

                    float sx = e.CellBounds.Left + 12;
                    float sy = e.CellBounds.Top + (e.CellBounds.Height - 18) / 2f;
                    g.DrawString(starChar, starFont, starBrush, sx, sy);

                    // Show value next to star if numeric
                    if (double.TryParse(clean.Replace("★", "").Trim(), out var rVal))
                    {
                        using var numFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                        using var numBrush = new SolidBrush(ColorTextSec);
                        g.DrawString($"{rVal:F1}", numFont, numBrush, sx + 18, sy + 2);
                    }

                    e.Handled = true;
                    return;
                }

                // -------------------------------------------------------------
                // E. LOCATION / BRANCH COLUMN (Pin Icon 📍)
                // -------------------------------------------------------------
                bool isLocCol = colName.Contains("location") || colName.Contains("branch") || colName.Contains("city");
                if (isLocCol && !string.IsNullOrWhiteSpace(clean))
                {
                    using var pinFont = new Font("Segoe UI", 8.5f);
                    using var pinBrush = new SolidBrush(ColorTextSec);
                    string disp = clean.StartsWith("📍") ? clean : $"📍  {clean}";
                    g.DrawString(disp, pinFont, pinBrush, e.CellBounds.Left + 12, e.CellBounds.Top + (e.CellBounds.Height - 16) / 2f);
                    e.Handled = true;
                    return;
                }

                // -------------------------------------------------------------
                // F. FILES / ATTACHMENTS COLUMN (Paperclip Icon 📎)
                // -------------------------------------------------------------
                bool isFileCol = colName.Contains("file") || colName.Contains("attachment");
                if (isFileCol && !string.IsNullOrWhiteSpace(clean) && clean != "0")
                {
                    using var clipFont = new Font("Segoe UI", 8.5f);
                    using var clipBrush = new SolidBrush(ColorTextSec);
                    string disp = clean.StartsWith("📎") ? clean : $"📎  {clean}";
                    g.DrawString(disp, clipFont, clipBrush, e.CellBounds.Left + 12, e.CellBounds.Top + (e.CellBounds.Height - 16) / 2f);
                    e.Handled = true;
                    return;
                }

                // -------------------------------------------------------------
                // G. POSITION / ROLE COLUMN (Uppercase clean tracking)
                // -------------------------------------------------------------
                bool isRoleCol = colName.Contains("role") || colName.Contains("position") || colName.Contains("designation");
                if (isRoleCol && !string.IsNullOrWhiteSpace(clean))
                {
                    using var roleFont = new Font("Segoe UI", 8f, FontStyle.Bold);
                    using var roleBrush = new SolidBrush(ColorTextSec);
                    g.DrawString(clean.ToUpperInvariant(), roleFont, roleBrush, e.CellBounds.Left + 12, e.CellBounds.Top + (e.CellBounds.Height - 15) / 2f);
                    e.Handled = true;
                    return;
                }

                // -------------------------------------------------------------
                // H. DEFAULT TEXT CELL
                // -------------------------------------------------------------
                using (var defFont = new Font("Segoe UI", 9f))
                using (var defBrush = new SolidBrush(isSelected ? ColorSelectedText : (colName.Contains("email") || colName.Contains("phone") ? ColorTextSec : ColorTextPri)))
                {
                    float dx = e.CellBounds.Left + 12;
                    float dy = e.CellBounds.Top + (e.CellBounds.Height - 16) / 2f;
                    g.DrawString(clean, defFont, defBrush, dx, dy);
                }
                e.Handled = true;
            }
        };

        // =========================================================
        // HOVER TRACKING (does NOT overwrite row.Tag)
        // =========================================================
        grid.CellMouseEnter += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count) return;
            int old = hoveredRowIndex;
            hoveredRowIndex = e.RowIndex;
            if (old >= 0 && old < grid.Rows.Count) grid.InvalidateRow(old);
            grid.InvalidateRow(e.RowIndex);
        };

        grid.CellMouseLeave += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count) return;
            int old = hoveredRowIndex;
            hoveredRowIndex = -1;
            if (old >= 0 && old < grid.Rows.Count) grid.InvalidateRow(old);
        };

        // =========================================================
        // INVALIDATE ON SELECTION CHANGE
        // =========================================================
        grid.SelectionChanged += (_, _) =>
        {
            grid.Invalidate();
        };
    }

    private static string GetInitials(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "—";
        var parts = text.Trim().Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
        if (text.Length >= 2)
            return text[..2].ToUpperInvariant();
        return text.ToUpperInvariant();
    }

    /// <summary>
    /// Map a status/priority/category value to a modern color pair.
    /// </summary>
    public static (Color Bg, Color Fg) GetPillColors(string value)
    {
        var v = value.Trim().ToLowerInvariant();

        return v switch
        {
            // Success — Green
            "completed" or "active" or "approved" or "fullypaid" or "paid"
                or "resolved" or "accepted" or "champion" or "converted"
                or "won" or "depositreceived" or "true" or "online"
                => (Color.FromArgb(220, 252, 231), Color.FromArgb(16, 185, 129)),

            // Warning / In-progress — Amber
            "in progress" or "inprogress" or "pending" or "planning"
                or "promising" or "loyal" or "contacted" or "proposal"
                or "partialpaid" or "partiallypaid" or "review" or "in review"
                => (Color.FromArgb(254, 243, 199), Color.FromArgb(245, 158, 11)),

            // Danger — Pink / Red
            "open" or "rejected" or "critical" or "detractor" or "high"
                or "cancelled" or "expired" or "lost" or "urgent"
                or "severe" or "offline"
                => (Color.FromArgb(254, 226, 226), Color.FromArgb(244, 63, 94)),

            // Primary / Info — Blue
            "issued" or "new" or "medium" or "at risk" or "atrisk"
                or "dormant" or "manager" or "corporate" or "site visit"
                => (Color.FromArgb(219, 234, 254), Color.FromArgb(2, 132, 199)),

            // Purple / Indigo
            "admin" or "superadmin" or "vip" or "commercial"
                => (Color.FromArgb(237, 233, 254), Color.FromArgb(139, 92, 246)),

            // Neutral — Slate
            _ => (Color.FromArgb(241, 245, 249), Color.FromArgb(100, 116, 139))
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

    public static int GetAvatarColumnIndex(DataGridView grid)
    {
        if (grid == null || grid.Columns.Count == 0) return -1;

        // 1. Highest priority: explicit name columns
        string[] highPriority = { "fullname", "customername", "clientname", "designername", "companyname", "name", "customer", "client", "designer" };
        foreach (var p in highPriority)
        {
            for (int i = 0; i < grid.Columns.Count; i++)
            {
                var col = grid.Columns[i];
                if (!col.Visible) continue;
                var cn = col.Name.ToLowerInvariant();
                var ch = col.HeaderText.ToLowerInvariant();
                if (cn == p || ch == p) return i;
            }
        }

        // 2. Secondary priority: contains key words but not ID, Code, Plan, Fee, Date, Count, Status
        foreach (var p in highPriority)
        {
            for (int i = 0; i < grid.Columns.Count; i++)
            {
                var col = grid.Columns[i];
                if (!col.Visible) continue;
                var cn = col.Name.ToLowerInvariant();
                var ch = col.HeaderText.ToLowerInvariant();
                if ((cn.Contains(p) || ch.Contains(p)) &&
                    !cn.Contains("id") && !cn.Contains("code") && !cn.Contains("fee") && !cn.Contains("count") && !cn.Contains("status"))
                    return i;
            }
        }

        // 3. Fallback: first visible column that is not an ID, code, rownum, date, or status
        for (int i = 0; i < grid.Columns.Count; i++)
        {
            var col = grid.Columns[i];
            if (!col.Visible) continue;
            var cn = col.Name.ToLowerInvariant();
            var ch = col.HeaderText.ToLowerInvariant();
            if (cn == "rownum" || cn == "#" || cn.Contains("id") || cn.Contains("code") || cn.Contains("select") ||
                cn.Contains("chk") || cn.Contains("status") || cn.Contains("date") || ch == "#" || ch.Contains("id") || ch.Contains("code"))
                continue;
            return i;
        }

        return -1;
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