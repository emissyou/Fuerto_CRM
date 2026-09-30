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
/// Desktop Application Installer-style EULA / Terms & Conditions Setup Wizard Dialog.
/// Prompts tenant company users on initial login to read, review, and either Accept or Reject
/// the Platform Terms and Conditions established by the Super Administrator.
/// </summary>
public class TermsAndConditionsDialog : Form
{
    private readonly string _apiUrl;
    private readonly string? _token;
    private readonly int? _companyId;
    private readonly string _companyName;
    private readonly string _companyCode;
    private readonly string _userEmail;
    private readonly HttpClient _http;
    private readonly bool _isPreviewMode;

    private RichTextBox _rtbTerms = null!;
    private RadioButton _rbAccept = null!;
    private RadioButton _rbReject = null!;
    private CheckBox _chkAuthorize = null!;
    private Button _btnAccept = null!;
    private Button _btnReject = null!;
    private Label _lblStatus = null!;
    private Label _lblVersionBadge = null!;

    private string _currentVersion = "v1.0-2026";
    private string _currentTitle = "FUERTO CRM ENTERPRISE PLATFORM - SOFTWARE LICENSE & MASTER TERMS OF SERVICE";

    public TermsAndConditionsDialog(
        string apiUrl,
        string? token,
        int? companyId,
        string? companyName,
        string? companyCode,
        string? userEmail,
        bool isPreviewMode = false)
    {
        _apiUrl = apiUrl;
        _token = token;
        _companyId = companyId;
        _companyName = string.IsNullOrWhiteSpace(companyName) ? "Licensed Tenant Organization" : companyName;
        _companyCode = string.IsNullOrWhiteSpace(companyCode) ? "TENANT" : companyCode;
        _userEmail = string.IsNullOrWhiteSpace(userEmail) ? "authorized.user@crm.local" : userEmail;
        _isPreviewMode = isPreviewMode;
        _http = new HttpClient();

        if (!string.IsNullOrWhiteSpace(_token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        InitializeInstallerUI();
        LoadTermsContentAsync();
    }

    private void InitializeInstallerUI()
    {
        Text = _isPreviewMode
            ? "Fuerto CRM Platform - License Agreement Wizard (Super Admin Preview)"
            : "Fuerto CRM Platform Setup - License Agreement & Terms of Service";

        Size = new Size(740, 660);
        MinimumSize = new Size(680, 560);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(248, 249, 250);

        // ── 1. Top Banner Panel (Installer Header) ───────────────────────────
        var topBanner = new Panel
        {
            Dock = DockStyle.Top,
            Height = 84,
            BackColor = Color.White
        };
        topBanner.Paint += (_, pe) =>
        {
            pe.Graphics.DrawLine(new Pen(Color.FromArgb(226, 232, 240), 1), 0, topBanner.Height - 1, topBanner.Width, topBanner.Height - 1);
        };
        Controls.Add(topBanner);

        var lblBannerTitle = new Label
        {
            Text = "Software License & Platform Terms Agreement",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(24, 14),
            AutoSize = true
        };
        topBanner.Controls.Add(lblBannerTitle);

        var lblBannerSubtitle = new Label
        {
            Text = "Please review the license terms carefully before installing/accessing the Fuerto CRM workspace.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(24, 38),
            AutoSize = true
        };
        topBanner.Controls.Add(lblBannerSubtitle);

        var lblBannerHelp = new Label
        {
            Text = "Press Page Down or scroll down in the license agreement box to view the entire document.",
            Font = new Font("Segoe UI", 8f, FontStyle.Italic),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(24, 58),
            AutoSize = true
        };
        topBanner.Controls.Add(lblBannerHelp);

        // Right-side badge
        var pnlBadge = new Panel
        {
            Size = new Size(54, 54),
            Location = new Point(topBanner.Width - 78, 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.Transparent
        };
        pnlBadge.Paint += (_, pe) =>
        {
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var b = new SolidBrush(Color.FromArgb(254, 243, 199)); // amber 100
            pe.Graphics.FillEllipse(b, 2, 2, 50, 50);
            using var pen = new Pen(Color.FromArgb(255, 168, 0), 2f);
            pe.Graphics.DrawEllipse(pen, 2, 2, 50, 50);
            using var textBrush = new SolidBrush(Color.FromArgb(180, 83, 9));
            using var f = new Font("Segoe UI", 16f, FontStyle.Bold);
            pe.Graphics.DrawString("📜", f, textBrush, new PointF(10, 8));
        };
        topBanner.Controls.Add(pnlBadge);

        // ── 2. Bottom Command Bar Panel (Installer Footer) ───────────────────
        var bottomBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 64,
            BackColor = Color.FromArgb(241, 245, 249)
        };
        bottomBar.Paint += (_, pe) =>
        {
            pe.Graphics.DrawLine(new Pen(Color.FromArgb(226, 232, 240), 1), 0, 0, bottomBar.Width, 0);
        };
        Controls.Add(bottomBar);

        _lblStatus = new Label
        {
            Text = $"Tenant: {_companyName} [{_companyCode}]  ·  User: {_userEmail}",
            Font = new Font("Segoe UI", 8.25f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(20, 22),
            AutoSize = true
        };
        bottomBar.Controls.Add(_lblStatus);

        _btnAccept = new Button
        {
            Text = _isPreviewMode ? "Done (Preview)" : "Accept & Continue  >",
            Size = new Size(160, 36),
            Location = new Point(bottomBar.Width - 180, 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(203, 213, 225), // Initially disabled gray
            ForeColor = Color.White,
            Enabled = _isPreviewMode,
            Cursor = Cursors.Hand
        };
        _btnAccept.FlatAppearance.BorderSize = 0;
        _btnAccept.Click += async (_, _) => await HandleAcceptAsync();
        bottomBar.Controls.Add(_btnAccept);

        _btnReject = new Button
        {
            Text = _isPreviewMode ? "Close" : "Reject & Exit",
            Size = new Size(110, 36),
            Location = new Point(bottomBar.Width - 300, 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f),
            BackColor = Color.FromArgb(254, 242, 242),
            ForeColor = Color.FromArgb(220, 38, 38),
            Cursor = Cursors.Hand
        };
        _btnReject.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
        _btnReject.Click += (_, _) => HandleReject();
        bottomBar.Controls.Add(_btnReject);

        // ── 3. Main Body Panel ───────────────────────────────────────────────
        var bodyPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 14, 24, 12),
            BackColor = Color.FromArgb(248, 249, 250)
        };
        Controls.Add(bodyPanel);
        bodyPanel.BringToFront();

        var lblInstruction = new Label
        {
            Text = "To access Fuerto CRM with credentials provisioned by the Super Administrator, you must agree to the platform governance terms below:",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(51, 65, 85),
            Dock = DockStyle.Top,
            Height = 24
        };
        bodyPanel.Controls.Add(lblInstruction);

        var pnlVersionRow = new Panel
        {
            Dock = DockStyle.Top,
            Height = 24,
            BackColor = Color.Transparent
        };
        bodyPanel.Controls.Add(pnlVersionRow);

        _lblVersionBadge = new Label
        {
            Text = "Platform Governance · Release: v1.0-2026 · Super Admin Authority",
            Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
            ForeColor = Color.FromArgb(180, 83, 9),
            Dock = DockStyle.Left,
            AutoSize = true
        };
        pnlVersionRow.Controls.Add(_lblVersionBadge);

        // Agreement Interaction Panel (docked at the bottom of the body panel)
        var pnlAgreementOptions = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 88,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };
        bodyPanel.Controls.Add(pnlAgreementOptions);

        _rbAccept = new RadioButton
        {
            Text = "I &accept the terms of the Software License & Master Platform Agreement",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(4, 6),
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        _rbAccept.CheckedChanged += (_, _) => UpdateAcceptanceState();
        pnlAgreementOptions.Controls.Add(_rbAccept);

        _rbReject = new RadioButton
        {
            Text = "I &do not accept the agreement (Access to Fuerto CRM will be denied)",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(185, 28, 28),
            Location = new Point(4, 32),
            AutoSize = true,
            Checked = !_isPreviewMode,
            Cursor = Cursors.Hand
        };
        _rbReject.CheckedChanged += (_, _) => UpdateAcceptanceState();
        pnlAgreementOptions.Controls.Add(_rbReject);

        _chkAuthorize = new CheckBox
        {
            Text = $"I confirm that I am authorized to bind {_companyName} to these governance terms.",
            Font = new Font("Segoe UI", 8.25f),
            ForeColor = Color.FromArgb(71, 85, 105),
            Location = new Point(24, 58),
            AutoSize = true,
            Checked = _isPreviewMode,
            Cursor = Cursors.Hand
        };
        _chkAuthorize.CheckedChanged += (_, _) => UpdateAcceptanceState();
        pnlAgreementOptions.Controls.Add(_chkAuthorize);

        // RichTextBox for Terms Content
        _rtbTerms = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 41, 59),
            Font = new Font("Segoe UI", 9f),
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Margin = new Padding(0, 4, 0, 8)
        };
        _rtbTerms.Text = "Connecting to platform governance server to retrieve official Terms & Conditions...";
        bodyPanel.Controls.Add(_rtbTerms);
        _rtbTerms.BringToFront();

        if (_isPreviewMode)
        {
            _rbAccept.Checked = true;
            UpdateAcceptanceState();
        }
    }

    private void UpdateAcceptanceState()
    {
        if (_isPreviewMode)
        {
            _btnAccept.Enabled = true;
            _btnAccept.BackColor = Color.FromArgb(37, 99, 235);
            return;
        }

        bool canAccept = _rbAccept.Checked && _chkAuthorize.Checked;
        _btnAccept.Enabled = canAccept;
        _btnAccept.BackColor = canAccept ? Color.FromArgb(22, 163, 74) : Color.FromArgb(203, 213, 225);
    }

    private async void LoadTermsContentAsync()
    {
        try
        {
            var res = await _http.GetAsync($"{_apiUrl}/terms");
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.String)
                    _currentVersion = v.GetString() ?? _currentVersion;

                if (root.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                    _currentTitle = t.GetString() ?? _currentTitle;

                if (root.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String)
                {
                    string content = c.GetString() ?? "";
                    _rtbTerms.Text = content.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
                }

                _lblVersionBadge.Text = $"Platform Governance · Version: {_currentVersion} · Super Admin Authority";
            }
            else
            {
                _rtbTerms.Text = GetFallbackTerms();
            }
        }
        catch
        {
            _rtbTerms.Text = GetFallbackTerms();
        }
    }

    private async Task HandleAcceptAsync()
    {
        if (_isPreviewMode)
        {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        _btnAccept.Enabled = false;
        _btnReject.Enabled = false;
        _lblStatus.Text = "Recording legal acceptance on platform server...";
        _lblStatus.ForeColor = Color.FromArgb(37, 99, 235);

        try
        {
            var res = await _http.PostAsync($"{_apiUrl}/terms/accept", new StringContent("{}", Encoding.UTF8, "application/json"));
            if (res.IsSuccessStatusCode)
            {
                Session.HasAcceptedTerms = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                var err = await res.Content.ReadAsStringAsync();
                string msg = "Unable to record terms acceptance.";
                try
                {
                    using var doc = JsonDocument.Parse(err);
                    if (doc.RootElement.TryGetProperty("message", out var m))
                        msg = m.GetString() ?? msg;
                }
                catch { }

                MessageBox.Show(
                    $"Server returned an error: {msg}\n\nPlease try again or contact the Super Administrator.",
                    "Terms Acceptance Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                _btnAccept.Enabled = true;
                _btnReject.Enabled = true;
                _lblStatus.Text = "Please click Accept & Continue to proceed.";
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
            }
        }
        catch (Exception ex)
        {
            // If offline, allow offline acceptance recording
            Session.HasAcceptedTerms = true;
            MessageBox.Show(
                $"Recorded local terms acceptance: {ex.Message}\nPlatform synchronization will record acceptance once online.",
                "Offline Acceptance",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private void HandleReject()
    {
        if (_isPreviewMode)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        var confirm = MessageBox.Show(
            "Are you sure you want to reject the Software License & Terms Agreement?\n\n" +
            "WARNING: You will not be permitted to access or use the Fuerto CRM system. " +
            "Your login session will be terminated immediately, and you will be returned to the sign-in screen.",
            "Confirm Rejection of Platform Terms",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm == DialogResult.Yes)
        {
            Session.HasAcceptedTerms = false;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    private static string GetFallbackTerms()
    {
        return
@"FUERTO CRM ENTERPRISE PLATFORM
SOFTWARE LICENSE & MASTER TERMS OF SERVICE (EULA)
Platform Licensor: Super Administrator

================================================================================
IMPORTANT NOTICE - SOFTWARE LICENSE & GOVERNANCE COMPLIANCE
================================================================================

1. Grant of License:
Licensor grants the Company a non-exclusive, non-transferable license to access the Fuerto CRM system for legitimate business operations.

2. Super Admin Authority:
The Super Administrator oversees platform governance, subscriptions, and multi-tenant security policies. Company accounts are subject to administrative audit and subscription standing.

3. Multi-Tenant Confidentiality:
All company business data is protected and logically segregated in compliance with Data Privacy Act standards.

4. Hybrid Dual-Tier Data Synchronization:
Data generated in offline workstation mode will synchronize with the central cloud database when an internet connection is established.

5. Rejection & Termination:
Failure or refusal to accept these terms results in immediate denial of workspace access.";
    }
}
