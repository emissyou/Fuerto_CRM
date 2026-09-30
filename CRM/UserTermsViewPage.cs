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
/// Tenant Company User Page – View Platform Terms of Service and Software License Agreement.
/// Displays the active terms configured by the Super Administrator and the tenant company's acceptance status.
/// </summary>
public class UserTermsViewPage : Panel
{
    private static readonly Color CBg        = Color.FromArgb(245, 247, 250);
    private static readonly Color CCard      = Color.White;
    private static readonly Color CBorder    = Color.FromArgb(226, 230, 236);
    private static readonly Color CText      = Color.FromArgb(15, 23, 42);
    private static readonly Color CMuted     = Color.FromArgb(100, 116, 139);
    private static readonly Color CGreen     = Color.FromArgb(22, 163, 74);
    private static readonly Color CGreenBg   = Color.FromArgb(240, 253, 244);
    private static readonly Color CBlue      = Color.FromArgb(37, 99, 235);
    private static readonly Color CAmber     = Color.FromArgb(217, 119, 6);
    private static readonly Color CAmberBg   = Color.FromArgb(254, 243, 199);

    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private Panel _container = null!;
    private Label _lblStatusBadge = null!;
    private Label _lblMeta = null!;
    private RichTextBox _rtbTerms = null!;
    private Button _btnReaccept = null!;
    private Button _btnRefresh = null!;

    public UserTermsViewPage(string apiUrl = "http://localhost:5068", HttpClient? http = null)
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
        _ = LoadTermsAsync();
    }

    private void BuildUI()
    {
        Controls.Clear();

        _container = new Panel
        {
            Location  = new Point(28, 20),
            Width     = Math.Max(780, Width - 56),
            BackColor = Color.Transparent,
            AutoSize  = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        Controls.Add(_container);

        Resize += (_, _) =>
        {
            int targetW = Math.Max(780, ClientSize.Width - 56);
            if (_container.Width != targetW)
            {
                _container.Width = targetW;
                foreach (Control c in _container.Controls)
                {
                    c.Width = targetW;
                }
            }
        };

        int targetW = Math.Max(780, ClientSize.Width - 56);
        int y = 0;

        // 1. Header Banner
        var banner = new Panel
        {
            Width     = targetW,
            Height    = 84,
            BackColor = CCard
        };
        banner.Paint += (_, pe) =>
        {
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var grad = new LinearGradientBrush(new Rectangle(0, 0, banner.Width, 4), CBlue, Color.FromArgb(16, 185, 129), LinearGradientMode.Horizontal);
            g.FillRectangle(grad, 0, 0, banner.Width, 4);
            g.DrawRectangle(new Pen(CBorder, 1), 0, 0, banner.Width - 1, banner.Height - 1);
        };

        var lblTitle = new Label
        {
            Text      = "📜  Software License & Platform Terms of Service",
            Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = CText,
            AutoSize  = true,
            Location  = new Point(20, 16)
        };
        banner.Controls.Add(lblTitle);

        var lblSub = new Label
        {
            Text      = "Governed by the Platform Super Administrator · Applicable to all tenant companies and authorized users.",
            Font      = new Font("Segoe UI", 9f),
            ForeColor = CMuted,
            AutoSize  = true,
            Location  = new Point(22, 46)
        };
        banner.Controls.Add(lblSub);

        banner.Location = new Point(0, y);
        _container.Controls.Add(banner);
        y += banner.Height + 16;

        // 2. Status Badge Card
        var statusCard = new Panel
        {
            Width     = targetW,
            Height    = 70,
            BackColor = CCard
        };
        statusCard.Paint += (_, pe) =>
        {
            pe.Graphics.DrawRectangle(new Pen(CBorder, 1), 0, 0, statusCard.Width - 1, statusCard.Height - 1);
        };

        _lblStatusBadge = new Label
        {
            Text      = "Checking compliance status...",
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = CText,
            Location  = new Point(20, 14),
            AutoSize  = true
        };
        statusCard.Controls.Add(_lblStatusBadge);

        _lblMeta = new Label
        {
            Text      = $"Tenant Organization: {Session.CompanyName} ({Session.CompanyCode})",
            Font      = new Font("Segoe UI", 8.75f),
            ForeColor = CMuted,
            Location  = new Point(20, 38),
            AutoSize  = true
        };
        statusCard.Controls.Add(_lblMeta);

        _btnReaccept = new Button
        {
            Text = "Review & Accept EULA",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            BackColor = CBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Height = 34,
            Width = 190,
            Location = new Point(targetW - 210, 18),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Visible = false,
            Cursor = Cursors.Hand
        };
        _btnReaccept.FlatAppearance.BorderSize = 0;
        _btnReaccept.Click += (_, _) => OpenAcceptanceWizard();
        statusCard.Controls.Add(_btnReaccept);

        _btnRefresh = new Button
        {
            Text = "🔄 Refresh",
            Font = new Font("Segoe UI", 8.75f),
            BackColor = Color.White,
            ForeColor = CMuted,
            FlatStyle = FlatStyle.Flat,
            Height = 34,
            Width = 90,
            Location = new Point(targetW - 110, 18),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        _btnRefresh.FlatAppearance.BorderColor = CBorder;
        _btnRefresh.Click += async (_, _) => await LoadTermsAsync();
        statusCard.Controls.Add(_btnRefresh);

        statusCard.Location = new Point(0, y);
        _container.Controls.Add(statusCard);
        y += statusCard.Height + 16;

        // 3. Agreement Terms Text Card
        var termsCard = new Panel
        {
            Width     = targetW,
            BackColor = CCard
        };

        var cardHdr = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.FromArgb(248, 250, 252)
        };
        cardHdr.Paint += (_, pe) =>
        {
            pe.Graphics.DrawLine(new Pen(CBorder, 1), 0, cardHdr.Height - 1, cardHdr.Width, cardHdr.Height - 1);
        };
        cardHdr.Controls.Add(new Label
        {
            Text = "📄  Active Platform Agreement Terms & Conditions",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = CText,
            Location = new Point(18, 12),
            AutoSize = true
        });
        termsCard.Controls.Add(cardHdr);

        _rtbTerms = new RichTextBox
        {
            Location = new Point(18, 56),
            Width = targetW - 36,
            Height = 460,
            Font = new Font("Segoe UI", 9.25f),
            ForeColor = Color.FromArgb(15, 23, 42),
            BackColor = Color.FromArgb(250, 250, 250),
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            WordWrap = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        termsCard.Controls.Add(_rtbTerms);

        termsCard.Height = 56 + _rtbTerms.Height + 20;
        termsCard.Paint += (_, pe) =>
        {
            pe.Graphics.DrawRectangle(new Pen(CBorder, 1), 0, 0, termsCard.Width - 1, termsCard.Height - 1);
        };

        termsCard.Location = new Point(0, y);
        _container.Controls.Add(termsCard);
        y += termsCard.Height + 24;
    }

    public async Task LoadTermsAsync()
    {
        try
        {
            // 1. Load active terms
            var tRes = await _http.GetAsync($"{_apiUrl}/terms");
            if (tRes.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await tRes.Content.ReadAsStringAsync());
                var root = doc.RootElement;
                if (root.TryGetProperty("content", out var c))
                {
                    _rtbTerms.Text = (c.GetString() ?? "").Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
                }
            }

            // 2. Load acceptance status
            var sRes = await _http.GetAsync($"{_apiUrl}/terms/status");
            if (sRes.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await sRes.Content.ReadAsStringAsync());
                var root = doc.RootElement;

                bool hasAccepted = root.TryGetProperty("hasAccepted", out var ha) && ha.GetBoolean();
                string ver = root.TryGetProperty("currentVersion", out var v) ? v.GetString() ?? "v1.0" : "v1.0";

                string acceptedAt = "";
                if (root.TryGetProperty("acceptedAt", out var at) && at.ValueKind == JsonValueKind.String)
                {
                    if (DateTime.TryParse(at.GetString(), out var dt))
                        acceptedAt = dt.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt");
                }
                string acceptedBy = root.TryGetProperty("acceptedBy", out var by) ? by.GetString() ?? "" : "";

                if (hasAccepted)
                {
                    _lblStatusBadge.Text = $"✅ Active Agreement: Accepted (Version: {ver})";
                    _lblStatusBadge.ForeColor = CGreen;
                    _lblMeta.Text = $"Tenant: {Session.CompanyName} ({Session.CompanyCode}) · Accepted by: {acceptedBy} on {acceptedAt}";
                    _btnReaccept.Visible = false;
                }
                else
                {
                    _lblStatusBadge.Text = $"⚠️ Pending Acceptance Required (Version: {ver})";
                    _lblStatusBadge.ForeColor = CAmber;
                    _lblMeta.Text = $"The Super Administrator has updated the Platform Terms. Please review and accept to keep your account in good standing.";
                    _btnReaccept.Visible = true;
                }
            }
        }
        catch (Exception ex)
        {
            _lblStatusBadge.Text = "Terms loaded (offline or local mode)";
            _lblMeta.Text = ex.Message;
        }
    }

    private void OpenAcceptanceWizard()
    {
        using var dlg = new TermsAndConditionsDialog(
            _apiUrl,
            Session.Token,
            Session.CompanyId,
            Session.CompanyName,
            Session.CompanyCode,
            Session.Email);

        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            Session.HasAcceptedTerms = true;
            _ = LoadTermsAsync();
        }
    }
}
