using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM_DesignServices.winforms;

/// <summary>
/// Super Admin – Dedicated Platform Terms & Conditions Governance and Live Editor Page.
/// Allows the Super Administrator to freely edit the EULA agreement title, version, and text clauses,
/// preview the user installer experience, mandate tenant re-acceptance, and inspect compliance audits.
/// </summary>
public class SuperAdminTermsManagementPage : Panel
{
    private static readonly Color CBg        = Color.FromArgb(245, 247, 250);
    private static readonly Color CCard      = Color.White;
    private static readonly Color CBorder    = Color.FromArgb(226, 230, 236);
    private static readonly Color CText      = Color.FromArgb(15, 23, 42);
    private static readonly Color CMuted     = Color.FromArgb(100, 116, 139);
    private static readonly Color CAccent    = Color.FromArgb(255, 168, 0);
    private static readonly Color CGreen     = Color.FromArgb(22, 163, 74);
    private static readonly Color CGreenBg   = Color.FromArgb(240, 253, 244);
    private static readonly Color CBlue      = Color.FromArgb(37, 99, 235);
    private static readonly Color CBlueBg    = Color.FromArgb(239, 246, 255);
    private static readonly Color CRed       = Color.FromArgb(220, 38, 38);
    private static readonly Color CRedBg     = Color.FromArgb(254, 242, 242);
    private static readonly Color CAmber     = Color.FromArgb(217, 119, 6);
    private static readonly Color CAmberBg   = Color.FromArgb(254, 243, 199);

    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private Panel _container = null!;
    private TextBox _txtTitle = null!;
    private TextBox _txtVersion = null!;
    private RichTextBox _rtbContent = null!;
    private CheckBox _chkForceReaccept = null!;
    private Button _btnSave = null!;
    private Button _btnPreview = null!;
    private Button _btnReset = null!;
    private Button _btnReload = null!;
    private Button _btnRevokeAll = null!;
    private Label _lblStatus = null!;

    // KPI labels
    private Label _lblKpiVersion = null!;
    private Label _lblKpiUpdated = null!;
    private Label _lblKpiRate = null!;
    private Label _lblKpiTotal = null!;

    private DataGridView _dgvCompliance = null!;

    public SuperAdminTermsManagementPage(string apiUrl = "http://localhost:5068", HttpClient? http = null)
    {
        _apiUrl = apiUrl.TrimEnd('/');
        _http   = http ?? new HttpClient();

        if (!string.IsNullOrWhiteSpace(Session.Token) && !_http.DefaultRequestHeaders.Contains("Authorization"))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
        }

        Dock       = DockStyle.Fill;
        BackColor  = CBg;
        AutoScroll = true;
        Padding    = new Padding(28, 20, 28, 40);

        BuildUI();
        _ = LoadTermsAndAuditAsync();
    }

    private void BuildUI()
    {
        Controls.Clear();

        _container = new Panel
        {
            Location  = new Point(28, 20),
            Width     = Math.Max(840, Width - 56),
            BackColor = Color.Transparent,
            AutoSize  = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        Controls.Add(_container);

        Resize += (_, _) =>
        {
            int targetW = Math.Max(840, ClientSize.Width - 56);
            if (_container.Width != targetW)
            {
                _container.Width = targetW;
                foreach (Control c in _container.Controls)
                {
                    c.Width = targetW;
                }
            }
        };

        int targetW = Math.Max(840, ClientSize.Width - 56);
        int y = 0;

        // 1. Header Banner
        var banner = BuildBanner(targetW);
        banner.Location = new Point(0, y);
        _container.Controls.Add(banner);
        y += banner.Height + 16;

        // 2. KPI Cards Row
        var kpiRow = BuildKpiRow(targetW);
        kpiRow.Location = new Point(0, y);
        _container.Controls.Add(kpiRow);
        y += kpiRow.Height + 16;

        // 3. Editor Card
        var editorCard = BuildEditorCard(targetW);
        editorCard.Location = new Point(0, y);
        _container.Controls.Add(editorCard);
        y += editorCard.Height + 16;

        // 4. Compliance Audit Card
        var auditCard = BuildAuditCard(targetW);
        auditCard.Location = new Point(0, y);
        _container.Controls.Add(auditCard);
        y += auditCard.Height + 24;
    }

    // ── 1. Header Banner ─────────────────────────────────────────────────────
    private Panel BuildBanner(int width)
    {
        var pnl = new Panel
        {
            Width     = width,
            Height    = 88,
            BackColor = CCard
        };
        pnl.Paint += (_, pe) =>
        {
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var grad = new LinearGradientBrush(new Rectangle(0, 0, pnl.Width, 5), Color.FromArgb(37, 99, 235), Color.FromArgb(255, 168, 0), LinearGradientMode.Horizontal);
            g.FillRectangle(grad, 0, 0, pnl.Width, 5);
            g.DrawRectangle(new Pen(CBorder, 1), 0, 0, pnl.Width - 1, pnl.Height - 1);
        };

        var lblTitle = new Label
        {
            Text      = "📜  Platform Terms & Conditions (EULA) Governance",
            Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            AutoSize  = true,
            Location  = new Point(20, 16)
        };
        pnl.Controls.Add(lblTitle);

        var lblSub = new Label
        {
            Text      = "Super Administrator Authority: Freely edit agreement clauses, manage version codes, enforce tenant re-acceptance, and inspect compliance audits in real time.",
            Font      = new Font("Segoe UI", 9f),
            ForeColor = CMuted,
            AutoSize  = true,
            Location  = new Point(22, 46)
        };
        pnl.Controls.Add(lblSub);

        return pnl;
    }

    // ── 2. KPI Cards ─────────────────────────────────────────────────────────
    private Panel BuildKpiRow(int width)
    {
        var pnl = new Panel
        {
            Width     = width,
            Height    = 88,
            BackColor = Color.Transparent
        };

        int cardW = (width - 36) / 4;

        pnl.Controls.Add(MakeKpiCard("ACTIVE EULA VERSION", "Loading...", CBlue, CBlueBg, 0, cardW, out _lblKpiVersion));
        pnl.Controls.Add(MakeKpiCard("LAST MODIFIED", "Loading...", Color.FromArgb(109, 40, 217), Color.FromArgb(245, 243, 255), cardW + 12, cardW, out _lblKpiUpdated));
        pnl.Controls.Add(MakeKpiCard("REGISTERED TENANTS", "Loading...", Color.FromArgb(3, 105, 161), Color.FromArgb(240, 249, 255), (cardW + 12) * 2, cardW, out _lblKpiTotal));
        pnl.Controls.Add(MakeKpiCard("TENANT ACCEPTANCE RATE", "Loading...", CGreen, CGreenBg, (cardW + 12) * 3, cardW, out _lblKpiRate));

        return pnl;
    }

    private static Panel MakeKpiCard(string title, string value, Color accent, Color bg, int left, int width, out Label valLabel)
    {
        var card = new Panel
        {
            Location  = new Point(left, 0),
            Width     = width,
            Height    = 88,
            BackColor = CCard
        };
        card.Paint += (_, pe) =>
        {
            pe.Graphics.DrawRectangle(new Pen(CBorder, 1), 0, 0, card.Width - 1, card.Height - 1);
            using var b = new SolidBrush(accent);
            pe.Graphics.FillRectangle(b, 0, 0, 4, card.Height);
        };

        var lblT = new Label
        {
            Text      = title,
            Font      = new Font("Segoe UI", 7.75f, FontStyle.Bold),
            ForeColor = CMuted,
            Location  = new Point(14, 12),
            AutoSize  = true
        };
        card.Controls.Add(lblT);

        valLabel = new Label
        {
            Text      = value,
            Font      = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = accent,
            Location  = new Point(14, 34),
            AutoSize  = true
        };
        card.Controls.Add(valLabel);

        return card;
    }

    // ── 3. Terms Editor Card ─────────────────────────────────────────────────
    private Panel BuildEditorCard(int width)
    {
        var card = new Panel
        {
            Width     = width,
            BackColor = CCard,
            Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };

        var hdr = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 46,
            BackColor = Color.FromArgb(248, 250, 252)
        };
        hdr.Paint += (_, pe) =>
        {
            pe.Graphics.DrawLine(new Pen(CBorder, 1), 0, hdr.Height - 1, hdr.Width, hdr.Height - 1);
            using var b = new SolidBrush(CBlue);
            pe.Graphics.FillRectangle(b, 0, 0, 5, hdr.Height);
        };
        hdr.Controls.Add(new Label
        {
            Text      = "✏️  Live Agreement Editor – Edit Title, Version, and Full Clauses",
            Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            AutoSize  = true,
            Location  = new Point(16, 12)
        });
        card.Controls.Add(hdr);

        int curY = hdr.Bottom + 16;

        // Row 1: Title & Version
        var lblTitle = new Label
        {
            Text = "Agreement Title / Heading:",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = CText,
            Location = new Point(18, curY),
            AutoSize = true
        };
        card.Controls.Add(lblTitle);

        var lblVer = new Label
        {
            Text = "Version Code:",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = CText,
            Location = new Point(width - 270, curY),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = true
        };
        card.Controls.Add(lblVer);

        curY += 22;

        _txtTitle = new TextBox
        {
            Location = new Point(18, curY),
            Width = width - 310,
            Font = new Font("Segoe UI", 9.5f),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Text = "FUERTO CRM ENTERPRISE PLATFORM - SOFTWARE LICENSE & MASTER TERMS OF SERVICE"
        };
        card.Controls.Add(_txtTitle);

        _txtVersion = new TextBox
        {
            Location = new Point(width - 270, curY),
            Width = 130,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Text = "v1.1-2026"
        };
        card.Controls.Add(_txtVersion);

        var btnIncVer = new Button
        {
            Text = "➕ +0.1",
            Location = new Point(width - 132, curY - 1),
            Width = 114,
            Height = 28,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(51, 65, 85),
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        btnIncVer.FlatAppearance.BorderColor = CBorder;
        btnIncVer.Click += (_, _) => IncrementVersion();
        card.Controls.Add(btnIncVer);

        curY += 38;

        // Quick Clause Injection Toolbar
        var flowTools = new FlowLayoutPanel
        {
            Location = new Point(18, curY),
            Width = width - 36,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        card.Controls.Add(flowTools);

        flowTools.Controls.Add(MakeQuickButton("+ Add Privacy Clause (RA 10173)", (_, _) => InsertClause(GetPrivacyClause())));
        flowTools.Controls.Add(MakeQuickButton("+ Add 99.9% SLA Guarantee", (_, _) => InsertClause(GetSlaClause())));
        flowTools.Controls.Add(MakeQuickButton("+ Add Hybrid Offline/Cloud Sync Policy", (_, _) => InsertClause(GetOfflineClause())));
        flowTools.Controls.Add(MakeQuickButton("+ Add Super Admin Governance Authority", (_, _) => InsertClause(GetGovernanceClause())));

        _btnReset = MakeQuickButton("↺ Reset to Platform Default", async (_, _) => await HandleResetDefaultAsync());
        _btnReset.ForeColor = CRed;
        flowTools.Controls.Add(_btnReset);

        curY += flowTools.Height + 10;

        // RichTextBox editor
        var lblContent = new Label
        {
            Text = "Agreement Clauses & Legal Body Text (What tenant companies read and accept):",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = CText,
            Location = new Point(18, curY),
            AutoSize = true
        };
        card.Controls.Add(lblContent);
        curY += 22;

        _rtbContent = new RichTextBox
        {
            Location = new Point(18, curY),
            Width = width - 36,
            Height = 320,
            Font = new Font("Segoe UI", 9.25f),
            ForeColor = Color.FromArgb(15, 23, 42),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            WordWrap = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        card.Controls.Add(_rtbContent);
        curY += _rtbContent.Height + 14;

        // Force Re-acceptance checkbox
        _chkForceReaccept = new CheckBox
        {
            Text = "Mandate Tenant Re-Acceptance: Require ALL tenant companies to review and re-accept these updated terms upon next login.",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(185, 28, 28),
            Location = new Point(18, curY),
            Width = width - 36,
            Checked = true, // Default to true so changes cascade!
            AutoSize = true,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        card.Controls.Add(_chkForceReaccept);
        curY += _chkForceReaccept.Height + 16;

        // Command Bar (Save, Preview, Reload)
        var pnlActions = new Panel
        {
            Location = new Point(18, curY),
            Width = width - 36,
            Height = 44,
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        card.Controls.Add(pnlActions);

        _btnSave = new Button
        {
            Text = "💾  Save & Publish Updated Terms",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = CGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Height = 38,
            Width = 260,
            Location = new Point(0, 0),
            Cursor = Cursors.Hand
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += async (_, _) => await HandleSaveTermsAsync();
        pnlActions.Controls.Add(_btnSave);

        _btnPreview = new Button
        {
            Text = "👁  Preview Tenant Installer EULA",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            BackColor = CBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Height = 38,
            Width = 240,
            Location = new Point(270, 0),
            Cursor = Cursors.Hand
        };
        _btnPreview.FlatAppearance.BorderSize = 0;
        _btnPreview.Click += (_, _) => HandlePreview();
        pnlActions.Controls.Add(_btnPreview);

        _btnReload = new Button
        {
            Text = "🔄  Discard & Reload",
            Font = new Font("Segoe UI", 9f),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(71, 85, 105),
            FlatStyle = FlatStyle.Flat,
            Height = 38,
            Width = 150,
            Location = new Point(520, 0),
            Cursor = Cursors.Hand
        };
        _btnReload.FlatAppearance.BorderColor = CBorder;
        _btnReload.Click += async (_, _) => await LoadTermsAndAuditAsync();
        pnlActions.Controls.Add(_btnReload);

        _lblStatus = new Label
        {
            Text = "Ready",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = CMuted,
            Location = new Point(685, 12),
            AutoSize = true
        };
        pnlActions.Controls.Add(_lblStatus);

        curY += pnlActions.Height + 16;
        card.Height = curY;

        card.Paint += (_, pe) =>
        {
            pe.Graphics.DrawRectangle(new Pen(CBorder, 1), 0, 0, card.Width - 1, card.Height - 1);
        };

        return card;
    }

    private static Button MakeQuickButton(string text, EventHandler onClick)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = Color.FromArgb(71, 85, 105),
            FlatStyle = FlatStyle.Flat,
            Height = 28,
            AutoSize = true,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0)
        };
        btn.FlatAppearance.BorderColor = CBorder;
        btn.Click += onClick;
        return btn;
    }

    // ── 4. Compliance Audit Card ─────────────────────────────────────────────
    private Panel BuildAuditCard(int width)
    {
        var card = new Panel
        {
            Width     = width,
            BackColor = CCard,
            Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };

        var hdr = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 46,
            BackColor = Color.FromArgb(248, 250, 252)
        };
        hdr.Paint += (_, pe) =>
        {
            pe.Graphics.DrawLine(new Pen(CBorder, 1), 0, hdr.Height - 1, hdr.Width, hdr.Height - 1);
            using var b = new SolidBrush(CAccent);
            pe.Graphics.FillRectangle(b, 0, 0, 5, hdr.Height);
        };
        hdr.Controls.Add(new Label
        {
            Text      = "📋  Tenant Company Acceptance & Compliance Audit",
            Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            AutoSize  = true,
            Location  = new Point(16, 12)
        });

        _btnRevokeAll = new Button
        {
            Text = "⚡ Force ALL Tenants to Re-Accept",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            BackColor = CRedBg,
            ForeColor = CRed,
            FlatStyle = FlatStyle.Flat,
            Height = 28,
            Width = 230,
            Location = new Point(width - 250, 9),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        _btnRevokeAll.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
        _btnRevokeAll.Click += async (_, _) => await HandleRevokeAllAsync();
        hdr.Controls.Add(_btnRevokeAll);

        card.Controls.Add(hdr);

        int curY = hdr.Bottom + 16;

        var lblNote = new Label
        {
            Text = "Live audit trail: Displays whether each registered tenant company has accepted the current terms version. Revoking terms requires the tenant to accept the terms again on their next login.",
            Font = new Font("Segoe UI", 8.75f),
            ForeColor = CMuted,
            Location = new Point(18, curY),
            Width = width - 36,
            Height = 24,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        card.Controls.Add(lblNote);
        curY += lblNote.Height + 8;

        _dgvCompliance = new DataGridView
        {
            Location = new Point(18, curY),
            Width = width - 36,
            Height = 240,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 9f)
        };

        _dgvCompliance.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyId", HeaderText = "ID", FillWeight = 25, Visible = false });
        _dgvCompliance.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyName", HeaderText = "Tenant Company", FillWeight = 110 });
        _dgvCompliance.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyCode", HeaderText = "Code", FillWeight = 45 });
        _dgvCompliance.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "EULA Status", FillWeight = 75 });
        _dgvCompliance.Columns.Add(new DataGridViewTextBoxColumn { Name = "Version", HeaderText = "Accepted Version", FillWeight = 50 });
        _dgvCompliance.Columns.Add(new DataGridViewTextBoxColumn { Name = "AcceptedAt", HeaderText = "Accepted Timestamp", FillWeight = 85 });
        _dgvCompliance.Columns.Add(new DataGridViewTextBoxColumn { Name = "AcceptedBy", HeaderText = "Accepted By User", FillWeight = 95 });

        var colRevoke = new DataGridViewButtonColumn
        {
            Name = "Action",
            HeaderText = "Governance Action",
            Text = "Require Re-Acceptance",
            UseColumnTextForButtonValue = true,
            FillWeight = 80
        };
        _dgvCompliance.Columns.Add(colRevoke);

        _dgvCompliance.CellContentClick += async (s, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == _dgvCompliance.Columns["Action"].Index)
            {
                int cid = Convert.ToInt32(_dgvCompliance.Rows[e.RowIndex].Cells["CompanyId"].Value);
                string cname = _dgvCompliance.Rows[e.RowIndex].Cells["CompanyName"].Value?.ToString() ?? "Company";

                var confirm = MessageBox.Show(
                    $"Require '{cname}' to re-accept the Platform Terms & Conditions on their next login?",
                    "Revoke Acceptance",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    await RevokeCompanyTermsAsync(cid, cname);
                }
            }
        };

        card.Controls.Add(_dgvCompliance);
        curY += _dgvCompliance.Height + 16;

        card.Height = curY;
        card.Paint += (_, pe) =>
        {
            pe.Graphics.DrawRectangle(new Pen(CBorder, 1), 0, 0, card.Width - 1, card.Height - 1);
        };

        return card;
    }

    // ── Data Loading & API Calls ─────────────────────────────────────────────
    public async Task LoadTermsAndAuditAsync()
    {
        _lblStatus.Text = "Loading platform terms...";
        _lblStatus.ForeColor = CBlue;

        try
        {
            // 1. Fetch Terms
            var tRes = await _http.GetAsync($"{_apiUrl}/terms");
            if (tRes.IsSuccessStatusCode)
            {
                var json = await tRes.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("title", out var t))
                    _txtTitle.Text = t.GetString() ?? "";

                string ver = "v1.0-2026";
                if (root.TryGetProperty("version", out var v))
                {
                    ver = v.GetString() ?? ver;
                    _txtVersion.Text = ver;
                }
                _lblKpiVersion.Text = ver;

                if (root.TryGetProperty("lastUpdated", out var lu) && DateTime.TryParse(lu.GetString(), out var dt))
                {
                    string by = root.TryGetProperty("updatedBy", out var ub) ? ub.GetString() ?? "Super Admin" : "Super Admin";
                    _lblKpiUpdated.Text = $"{dt.ToLocalTime():yyyy-MM-dd} ({by})";
                }

                if (root.TryGetProperty("content", out var c))
                {
                    string content = c.GetString() ?? "";
                    _rtbContent.Text = content.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
                }
            }

            // 2. Fetch Companies & Acceptance Records
            var cRes = await _http.GetAsync($"{_apiUrl}/companies");
            var aRes = await _http.GetAsync($"{_apiUrl}/superadmin/terms/acceptances");

            if (!cRes.IsSuccessStatusCode)
            {
                _lblStatus.Text = "Terms loaded (companies list unavailable).";
                return;
            }

            using var cDoc = JsonDocument.Parse(await cRes.Content.ReadAsStringAsync());
            JsonDocument? aDoc = null;
            if (aRes.IsSuccessStatusCode)
            {
                aDoc = JsonDocument.Parse(await aRes.Content.ReadAsStringAsync());
            }

            _dgvCompliance.Rows.Clear();

            int totalCompanies = 0;
            int acceptedCompanies = 0;
            string activeVer = _txtVersion.Text.Trim();

            foreach (var comp in cDoc.RootElement.EnumerateArray())
            {
                totalCompanies++;
                int cid = comp.GetProperty("companyId").GetInt32();
                string cname = comp.GetProperty("companyName").GetString() ?? "";
                string ccode = comp.GetProperty("companyCode").GetString() ?? "";

                bool hasAccepted = false;
                string acceptedAt = "-";
                string acceptedBy = "-";
                string termsVer = "-";

                if (aDoc != null && aDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var acc in aDoc.RootElement.EnumerateArray())
                    {
                        if (acc.GetProperty("companyId").GetInt32() == cid)
                        {
                            if (acc.TryGetProperty("termsVersion", out var tv))
                                termsVer = tv.GetString() ?? "";

                            // Must match active version
                            if (string.Equals(termsVer, activeVer, StringComparison.OrdinalIgnoreCase))
                            {
                                hasAccepted = true;
                                acceptedCompanies++;
                            }

                            if (acc.TryGetProperty("acceptedAt", out var at) && at.ValueKind == JsonValueKind.String)
                            {
                                if (DateTime.TryParse(at.GetString(), out var dt))
                                    acceptedAt = dt.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt");
                            }
                            if (acc.TryGetProperty("acceptedByName", out var by))
                                acceptedBy = by.GetString() ?? "";
                            break;
                        }
                    }
                }

                int rowIdx = _dgvCompliance.Rows.Add(
                    cid,
                    cname,
                    ccode,
                    hasAccepted ? "✅ Accepted" : "⏳ Pending Acceptance",
                    termsVer,
                    acceptedAt,
                    acceptedBy
                );

                if (!hasAccepted)
                {
                    _dgvCompliance.Rows[rowIdx].Cells["Status"].Style.ForeColor = CAmber;
                    _dgvCompliance.Rows[rowIdx].Cells["Status"].Style.BackColor = CAmberBg;
                }
                else
                {
                    _dgvCompliance.Rows[rowIdx].Cells["Status"].Style.ForeColor = CGreen;
                    _dgvCompliance.Rows[rowIdx].Cells["Status"].Style.BackColor = CGreenBg;
                }
            }

            _lblKpiTotal.Text = $"{totalCompanies} Organizations";
            int pct = totalCompanies > 0 ? (int)Math.Round((double)acceptedCompanies / totalCompanies * 100) : 0;
            _lblKpiRate.Text = $"{pct}% ({acceptedCompanies}/{totalCompanies})";

            _lblStatus.Text = "Terms & compliance records up to date.";
            _lblStatus.ForeColor = CGreen;
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Error: " + ex.Message;
            _lblStatus.ForeColor = CRed;
        }
    }

    private async Task HandleSaveTermsAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtTitle.Text))
        {
            MessageBox.Show("Please enter an Agreement Title.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtTitle.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtVersion.Text))
        {
            MessageBox.Show("Please enter a Version code (e.g. v1.1-2026).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtVersion.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_rtbContent.Text))
        {
            MessageBox.Show("Terms content cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _rtbContent.Focus();
            return;
        }

        _btnSave.Enabled = false;
        _lblStatus.Text = "Publishing updated terms to server...";
        _lblStatus.ForeColor = CBlue;

        try
        {
            var payload = new
            {
                title = _txtTitle.Text.Trim(),
                version = _txtVersion.Text.Trim(),
                content = _rtbContent.Text,
                forceReacceptance = _chkForceReaccept.Checked
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var res = await _http.PutAsync($"{_apiUrl}/superadmin/terms", content);
            if (res.IsSuccessStatusCode)
            {
                MessageBox.Show(
                    "Platform Terms & Conditions published successfully!\n\n" +
                    (_chkForceReaccept.Checked
                        ? "All tenant companies will be required to re-accept these updated terms on their next login."
                        : "New company accounts will review and accept this version upon initial sign-in."),
                    "Terms Published & Active",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                await LoadTermsAndAuditAsync();
            }
            else
            {
                var err = await res.Content.ReadAsStringAsync();
                MessageBox.Show($"Failed to save terms: {err}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _lblStatus.Text = "Publish failed.";
                _lblStatus.ForeColor = CRed;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Server error: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _lblStatus.Text = "Error: " + ex.Message;
            _lblStatus.ForeColor = CRed;
        }
        finally
        {
            _btnSave.Enabled = true;
        }
    }

    private void HandlePreview()
    {
        // Launch TermsAndConditionsDialog with currently edited text
        using var previewDlg = new TermsAndConditionsDialog(
            _apiUrl,
            Session.Token,
            Session.CompanyId ?? 1,
            "Acme Design Studio (Tenant Preview)",
            "ACME",
            "company.admin@acmedesign.local",
            isPreviewMode: true);

        previewDlg.ShowDialog(FindForm());
    }

    private async Task HandleRevokeAllAsync()
    {
        var confirm = MessageBox.Show(
            "Are you sure you want to revoke terms acceptance for ALL tenant companies?\n\n" +
            "This will require EVERY tenant organization to review and re-accept the Platform Terms & Conditions upon their next login.",
            "Confirm Revoke for ALL Companies",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm == DialogResult.Yes)
        {
            try
            {
                var res = await _http.PostAsync($"{_apiUrl}/superadmin/terms/revoke-all", null);
                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show("All company acceptances have been revoked. All tenants must re-accept on next login.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadTermsAndAuditAsync();
                }
                else
                {
                    MessageBox.Show("Failed to revoke acceptances.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task RevokeCompanyTermsAsync(int companyId, string companyName)
    {
        try
        {
            var json = JsonSerializer.Serialize(new { companyId });
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var res = await _http.PostAsync($"{_apiUrl}/superadmin/terms/revoke", content);
            if (res.IsSuccessStatusCode)
            {
                MessageBox.Show($"Terms acceptance revoked for {companyName}. They will be prompted on next sign-in.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadTermsAndAuditAsync();
            }
            else
            {
                MessageBox.Show("Failed to revoke company terms.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task HandleResetDefaultAsync()
    {
        var confirm = MessageBox.Show(
            "Reset the Platform Terms & Conditions back to standard platform default template?\n\n" +
            "This will overwrite any custom clauses and reset the version to v1.0-2026.",
            "Confirm Reset to Default",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm == DialogResult.Yes)
        {
            try
            {
                var res = await _http.PostAsync($"{_apiUrl}/superadmin/terms/reset-default", null);
                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show("Platform Terms & Conditions have been reset to platform default template.", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadTermsAndAuditAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void IncrementVersion()
    {
        string cur = _txtVersion.Text.Trim();
        // E.g. v1.0-2026 -> v1.1-2026
        try
        {
            var parts = cur.Split('-');
            string prefix = parts[0].TrimStart('v', 'V');
            if (double.TryParse(prefix, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
            {
                v += 0.1;
                string suffix = parts.Length > 1 ? "-" + parts[1] : "-2026";
                _txtVersion.Text = $"v{v:0.0}{suffix}";
            }
            else
            {
                _txtVersion.Text = cur + ".1";
            }
        }
        catch
        {
            _txtVersion.Text = "v1.1-2026";
        }
    }

    private void InsertClause(string clause)
    {
        if (string.IsNullOrWhiteSpace(_rtbContent.Text))
        {
            _rtbContent.Text = clause;
        }
        else
        {
            _rtbContent.AppendText(Environment.NewLine + Environment.NewLine + clause);
        }
        _rtbContent.SelectionStart = _rtbContent.Text.Length;
        _rtbContent.ScrollToCaret();
    }

    private static string GetPrivacyClause() =>
@"--------------------------------------------------------------------------------
CLAUSE: DATA PRIVACY & REGULATORY COMPLIANCE (REPUBLIC ACT NO. 10173)
--------------------------------------------------------------------------------
1. Compliance with the Data Privacy Act of 2012:
Company and Platform Administrator agree to process all personal information, customer contacts, and client design files in full compliance with RA 10173 and National Privacy Commission regulations.

2. Confidentiality & Tenant Isolation:
All company business transactions and leads remain the exclusive intellectual property and confidential data of the tenant organization. Unauthorized dissemination, cross-tenant leakages, or commercial distribution of private customer records is strictly prohibited.";

    private static string GetSlaClause() =>
@"--------------------------------------------------------------------------------
CLAUSE: 99.9% UPTIME SERVICE LEVEL AGREEMENT & SCHEDULED MAINTENANCE
--------------------------------------------------------------------------------
1. System Availability Commitment:
The Platform Super Administrator targets a ninety-nine and nine-tenths percent (99.9%) system availability SLA for cloud services, excluding pre-announced routine maintenance windows.

2. Scheduled Maintenance Windows:
Routine maintenance and database indexing will be scheduled during off-peak hours (Sundays 01:00 - 05:00 UTC+8). Tenant administrators will receive at least forty-eight (48) hours prior notification before any service interruption.";

    private static string GetOfflineClause() =>
@"--------------------------------------------------------------------------------
CLAUSE: HYBRID DUAL-TIER LOCAL AND CLOUD DATA SYNCHRONIZATION
--------------------------------------------------------------------------------
1. Offline Workstation Operations:
In the event of network disruption or offline workstation operation, data modifications are secured within local encrypted cache files. 

2. Automatic Cloud Convergence:
Upon re-establishment of an active internet connection, local updates automatically synchronize with the central cloud repository. In the event of conflicting simultaneous records, the platform timestamp resolver ensures data integrity without data loss.";

    private static string GetGovernanceClause() =>
@"--------------------------------------------------------------------------------
CLAUSE: SUPER ADMINISTRATOR PLATFORM GOVERNANCE AUTHORITY
--------------------------------------------------------------------------------
1. Administrative Oversight:
The Super Administrator possesses supreme administrative authority over platform multi-tenant databases, global security policies, subscription provisioning, and account lifecycle management.

2. Account Standing & Audit:
The Super Administrator reserves the right to audit tenant subscription standing, disable delinquent company accounts, and mandate re-acceptance of platform terms upon system version upgrades.";
}
