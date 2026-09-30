namespace CRM_DesignServices.winforms;

/// <summary>
/// Super Admin – Audit Log viewer.
/// Shows system-level events: logins, config changes, subscription updates, etc.
/// </summary>
public class SuperAdminAuditLogPage : Panel
{
    private DataGridView _grid      = null!;
    private CrmFilterBar _filterBar = null!;

    private static readonly Color CBg     = Color.FromArgb(245, 247, 250);
    private static readonly Color CCard   = Color.White;
    private static readonly Color CBorder = Color.FromArgb(226, 230, 236);
    private static readonly Color CText   = Color.FromArgb(15, 23, 42);
    private static readonly Color CMuted  = Color.FromArgb(100, 116, 139);
    private static readonly Color CAccent = Color.FromArgb(255, 168, 0);
    private static readonly Color CGreen  = Color.FromArgb(22, 163, 74);
    private static readonly Color CBlue   = Color.FromArgb(59, 130, 246);
    private static readonly Color CRed    = Color.FromArgb(220, 38, 38);

    public SuperAdminAuditLogPage()
    {
        Dock      = DockStyle.Fill;
        BackColor = CBg;
        Padding   = new Padding(28, 16, 28, 28);
        BuildUI();
        SeedSampleLogs();
    }

    private void BuildUI()
    {
        Controls.Clear();

        // Info banner
        var banner = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 52,
            BackColor = Color.FromArgb(239, 246, 255),
            Padding   = new Padding(16, 10, 16, 10)
        };
        banner.Paint += (_, pe) => pe.Graphics.DrawRectangle(new Pen(Color.FromArgb(186, 219, 255), 1), 0, 0, banner.Width - 1, banner.Height - 1);
        var lblBanner = new Label
        {
            Text      = "🔒  Audit logs are read-only and tamper-proof. All platform events are automatically captured.",
            Dock      = DockStyle.Fill,
            ForeColor = CBlue,
            Font      = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        banner.Controls.Add(lblBanner);
        Controls.Add(banner);

        // Toolbar
        var bar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent };
        Controls.Add(bar);
        var btnExport = MakeBtn("⬇  Export CSV", 130, false);
        btnExport.Left   = 0;
        btnExport.Click += (_, _) => MessageBox.Show("Export functionality coming soon.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        bar.Controls.Add(btnExport);

        // Filter
        _filterBar = new CrmFilterBar("Search by user, event, or company…");
        _filterBar.FiltersChanged += (_, _) => FilterGrid();
        _filterBar.AddFilter("type",     "Event Type",  "Login",   "Logout",  "Config Change", "Subscription", "User Created", "User Deleted", "Error");
        _filterBar.AddFilter("severity", "Severity",    "Info",    "Warning", "Critical");
        Controls.Add(_filterBar);

        // Grid card
        var card = new Panel { Dock = DockStyle.Fill, BackColor = CCard, Padding = new Padding(14, 10, 14, 10) };
        card.Paint += (_, pe) => pe.Graphics.DrawRectangle(new Pen(CBorder, 1), 0, 0, card.Width - 1, card.Height - 1);
        Controls.Add(card);
        card.BringToFront();

        _grid = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = CCard, BorderStyle = BorderStyle.None };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Timestamp", HeaderText = "TIMESTAMP",  FillWeight = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Severity",  HeaderText = "SEVERITY",   FillWeight = 75  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EventType", HeaderText = "EVENT TYPE", FillWeight = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Company",   HeaderText = "COMPANY",    FillWeight = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "User",      HeaderText = "USER",       FillWeight = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Details",   HeaderText = "DETAILS",    FillWeight = 300 });
        CrmTableStyler.Apply(_grid, "Severity", "EventType");
        card.Controls.Add(_grid);
        _grid.BringToFront();
    }

    private void SeedSampleLogs()
    {
        var now = DateTime.Now;
        var logs = new[]
        {
            (now.AddMinutes(-2),   "Info",     "Login",          "FUERTO",   "admin@fuerto.com",                 "Successful login from 192.168.1.10"),
            (now.AddMinutes(-5),   "Info",     "Login",          "GILBB",    "admin@glibahaybuilds.local",       "Successful login from 192.168.1.22"),
            (now.AddMinutes(-10),  "Info",     "Subscription",   "FUERTO",   "admin@fuerto.local",               "Subscription renewed – Enterprise Plan"),
            (now.AddMinutes(-15),  "Warning",  "Config Change",  "CCDAVAO",  "admin@customcraftersdavao.local",  "Module configuration updated"),
            (now.AddMinutes(-18),  "Info",     "User Created",   "GILBB",    "admin@glibahaybuilds.local",       "New team account created: engineer@glibahaybuilds.local"),
            (now.AddMinutes(-30),  "Info",     "Logout",         "FUERTO",   "admin@fuerto.com",                 "Session ended normally"),
            (now.AddMinutes(-45),  "Info",     "Login",          "CCDAVAO",  "admin@customcraftersdavao.local",  "Successful login from 10.0.0.5"),
            (now.AddHours(-1),     "Warning",  "Login",          "FUERTO",   "unknown@test.com",                 "Failed login attempt – invalid credentials"),
            (now.AddHours(-2),     "Info",     "Subscription",   "GILBB",    "admin@fuerto.local",               "Plan upgraded from Starter to Professional"),
            (now.AddHours(-3),     "Info",     "Config Change",  "FUERTO",   "admin@fuerto.local",               "Branch FUERTO-BGC created"),
            (now.AddHours(-4),     "Critical", "Error",          "CCDAVAO",  "system",                           "Database connection timeout – auto-recovered"),
            (now.AddHours(-5),     "Info",     "User Deleted",   "FUERTO",   "admin@fuerto.local",               "User account deactivated: oldstaff@fuerto.com"),
            (now.AddHours(-6),     "Info",     "Login",          "FUERTO",   "admin@fuerto.local",               "Super Admin login from 127.0.0.1"),
        };

        _grid.Rows.Clear();
        foreach (var (ts, sev, evt, comp, usr, det) in logs)
            _grid.Rows.Add(ts.ToString("yyyy-MM-dd HH:mm:ss"), sev, evt, comp, usr, det);
    }

    private void FilterGrid()
    {
        // Re-seed and re-filter
        SeedSampleLogs();
        string q       = _filterBar.SearchText.ToLowerInvariant();
        string? type   = _filterBar.GetFilterValue("type");
        string? sev    = _filterBar.GetFilterValue("severity");

        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            string rowType = row.Cells["EventType"].Value?.ToString()?.ToLowerInvariant() ?? "";
            string rowSev  = row.Cells["Severity"].Value?.ToString()  ?? "";
            string rowText = string.Join(" ", row.Cells.OfType<DataGridViewCell>().Select(c => c.Value?.ToString()?.ToLowerInvariant() ?? ""));

            bool visible = (string.IsNullOrEmpty(q)    || rowText.Contains(q))
                        && (string.IsNullOrEmpty(type)  || rowType.Equals(type,  StringComparison.OrdinalIgnoreCase))
                        && (string.IsNullOrEmpty(sev)   || rowSev.Equals(sev,    StringComparison.OrdinalIgnoreCase));
            row.Visible = visible;
        }
    }

    private static Button MakeBtn(string text, int w, bool primary)
    {
        var btn = new Button { Text = text, Width = w, Height = 36, Top = 8, FlatStyle = FlatStyle.Flat, BackColor = primary ? CAccent : CCard, ForeColor = primary ? Color.White : Color.FromArgb(55, 65, 81), Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand };
        btn.FlatAppearance.BorderSize = 1; btn.FlatAppearance.BorderColor = CBorder;
        return btn;
    }
}
