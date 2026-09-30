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
/// Super Admin Dialog to modify Platform Terms & Conditions, versioning,
/// and mandate re-acceptance across all tenant companies.
/// </summary>
public class EditPlatformTermsDialog : Form
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private TextBox _txtTitle = null!;
    private TextBox _txtVersion = null!;
    private RichTextBox _rtbContent = null!;
    private CheckBox _chkForceReaccept = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;
    private Label _lblStatus = null!;

    public event EventHandler? TermsUpdated;

    public EditPlatformTermsDialog(string apiUrl = "http://localhost:5068", HttpClient? http = null)
    {
        _apiUrl = apiUrl;
        _http = http ?? new HttpClient();

        if (!string.IsNullOrWhiteSpace(Session.Token) && !_http.DefaultRequestHeaders.Contains("Authorization"))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
        }

        BuildUI();
        LoadCurrentTermsAsync();
    }

    private void BuildUI()
    {
        Text = "Super Admin – Platform Terms & Conditions Editor";
        Size = new Size(780, 700);
        MinimumSize = new Size(720, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(248, 250, 252);

        // ── Top Header Banner ────────────────────────────────────────────────
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = Color.White
        };
        header.Paint += (_, pe) =>
        {
            pe.Graphics.DrawLine(new Pen(Color.FromArgb(226, 232, 240), 1), 0, header.Height - 1, header.Width, header.Height - 1);
            using var b = new SolidBrush(Color.FromArgb(255, 168, 0));
            pe.Graphics.FillRectangle(b, 0, 0, 6, header.Height);
        };
        Controls.Add(header);

        var lblHeaderTitle = new Label
        {
            Text = "Super Admin: Platform Terms of Service & EULA Governance",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(20, 14),
            AutoSize = true
        };
        header.Controls.Add(lblHeaderTitle);

        var lblHeaderSub = new Label
        {
            Text = "Configure the official terms and conditions that tenant companies must accept on initial login before entering the CRM.",
            Font = new Font("Segoe UI", 8.75f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(20, 40),
            AutoSize = true
        };
        header.Controls.Add(lblHeaderSub);

        // ── Bottom Command Bar ───────────────────────────────────────────────
        var bottomBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(241, 245, 249)
        };
        bottomBar.Paint += (_, pe) =>
        {
            pe.Graphics.DrawLine(new Pen(Color.FromArgb(226, 232, 240), 1), 0, 0, bottomBar.Width, 0);
        };
        Controls.Add(bottomBar);

        _lblStatus = new Label
        {
            Text = "Ready",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(20, 20),
            AutoSize = true
        };
        bottomBar.Controls.Add(_lblStatus);

        _btnSave = new Button
        {
            Text = "💾  Save & Publish Terms",
            Size = new Size(190, 36),
            Location = new Point(bottomBar.Width - 210, 12),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += async (_, _) => await HandleSaveAsync();
        bottomBar.Controls.Add(_btnSave);

        _btnCancel = new Button
        {
            Text = "Cancel",
            Size = new Size(95, 36),
            Location = new Point(bottomBar.Width - 315, 12),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(71, 85, 105),
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand
        };
        _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        _btnCancel.Click += (_, _) => Close();
        bottomBar.Controls.Add(_btnCancel);

        // ── Main Body Panel ──────────────────────────────────────────────────
        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 16, 24, 12),
            BackColor = Color.FromArgb(248, 250, 252)
        };
        Controls.Add(body);
        body.BringToFront();

        int y = 8;

        // Row 1: Title & Version
        var lblTitle = new Label
        {
            Text = "Agreement Title / Heading:",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85),
            Location = new Point(24, y),
            AutoSize = true
        };
        body.Controls.Add(lblTitle);

        var lblVersion = new Label
        {
            Text = "Version Code:",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85),
            Location = new Point(body.Width - 190, y),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = true
        };
        body.Controls.Add(lblVersion);

        y += 20;

        _txtTitle = new TextBox
        {
            Font = new Font("Segoe UI", 9.5f),
            Location = new Point(24, y),
            Width = body.Width - 230,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        body.Controls.Add(_txtTitle);

        _txtVersion = new TextBox
        {
            Font = new Font("Segoe UI", 9.5f),
            Location = new Point(body.Width - 190, y),
            Width = 166,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Text = "v1.1-2026"
        };
        body.Controls.Add(_txtVersion);

        y += 34;

        // Force Re-acceptance checkbox
        _chkForceReaccept = new CheckBox
        {
            Text = "Mandate Re-Acceptance: Require ALL tenant companies to review and re-accept these updated terms upon next login.",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(185, 28, 28), // Red emphasis
            Location = new Point(24, y),
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        body.Controls.Add(_chkForceReaccept);

        y += 28;

        // Row 2: Terms Clauses RichTextBox
        var lblContent = new Label
        {
            Text = "Terms & Conditions Text / Clauses (Displayed inside Desktop Installer EULA wizard):",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85),
            Location = new Point(24, y),
            AutoSize = true
        };
        body.Controls.Add(lblContent);

        y += 20;

        _rtbContent = new RichTextBox
        {
            Location = new Point(24, y),
            Size = new Size(body.Width - 48, body.Height - y - 16),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 9f),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(15, 23, 42),
            BorderStyle = BorderStyle.FixedSingle,
            WordWrap = true,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };
        body.Controls.Add(_rtbContent);
    }

    private async void LoadCurrentTermsAsync()
    {
        _lblStatus.Text = "Loading platform terms from server...";
        try
        {
            var res = await _http.GetAsync($"{_apiUrl}/terms");
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("title", out var t))
                    _txtTitle.Text = t.GetString() ?? "";

                if (root.TryGetProperty("version", out var v))
                    _txtVersion.Text = v.GetString() ?? "v1.0-2026";

                if (root.TryGetProperty("content", out var c))
                    _rtbContent.Text = (c.GetString() ?? "").Replace("\r\n", "\n").Replace("\n", Environment.NewLine);

                _lblStatus.Text = "Terms loaded successfully.";
            }
            else
            {
                _lblStatus.Text = "Using default platform template.";
            }
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Error loading terms: " + ex.Message;
        }
    }

    private async Task HandleSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtTitle.Text))
        {
            MessageBox.Show("Please enter an Agreement Title.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtTitle.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtVersion.Text))
        {
            MessageBox.Show("Please specify a Version string (e.g. v1.1-2026).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        _lblStatus.ForeColor = Color.FromArgb(37, 99, 235);

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
                        ? "All tenant companies will be required to re-accept these terms on their next login."
                        : "New company accounts will review and accept this version upon initial sign-in."),
                    "Terms Published",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                TermsUpdated?.Invoke(this, EventArgs.Empty);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                var err = await res.Content.ReadAsStringAsync();
                MessageBox.Show($"Failed to save terms: {err}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _btnSave.Enabled = true;
                _lblStatus.Text = "Failed to publish terms.";
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Server connection error: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _btnSave.Enabled = true;
            _lblStatus.Text = "Error: " + ex.Message;
        }
    }
}
