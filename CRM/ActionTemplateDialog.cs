using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class ActionTemplateDialog : Form
{
    public bool LoggedActivity { get; private set; }

    private readonly int _customerId;
    private readonly string _segment;
    private readonly string _action;
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private TextBox _scriptBox = null!;
    private Button _btnLog = null!;
    private Label _lblLogStatus = null!;

    public ActionTemplateDialog(
        string apiUrl,
        HttpClient http,
        int customerId,
        string customerName,
        string segment,
        string action,
        string basis,
        string email,
        string phone,
        decimal revenue)
    {
        _apiUrl = apiUrl;
        _http = http;
        _customerId = customerId;
        _segment = segment;
        _action = action;

        Text = $"Retention Action — {segment}";
        Size = new Size(640, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        // ---- Header ----
        Controls.Add(new Label
        {
            Text = customerName,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, 20)
        });

        var badge = new Label
        {
            Text = segment.ToUpperInvariant(),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = SegmentColor(segment),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(24, 52),
            Size = new Size(segment.Length * 9 + 20, 22)
        };
        Controls.Add(badge);

        Controls.Add(new Label
        {
            Text = $"{email}   ·   {phone}",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(24, 84)
        });

        // ---- Basis card ----
        var basisPanel = new Panel
        {
            Left = 24,
            Top = 116,
            Width = 570,
            Height = 60,
            BackColor = Color.FromArgb(252, 250, 245)
        };
        Controls.Add(basisPanel);

        basisPanel.Controls.Add(new Label
        {
            Text = "BASIS FOR ACTION",
            Font = new Font("Segoe UI", 7.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 95, 0),
            AutoSize = true,
            Location = new Point(12, 8)
        });
        basisPanel.Controls.Add(new Label
        {
            Text = basis,
            Font = new Font("Segoe UI", 9.25f),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(12, 28)
        });

        // ---- Recommended action ----
        Controls.Add(new Label
        {
            Text = "RECOMMENDED ACTION",
            Font = new Font("Segoe UI", 7.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(24, 192)
        });

        Controls.Add(new Label
        {
            Text = action,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, 210)
        });

        // ---- Editable script ----
        Controls.Add(new Label
        {
            Text = "SCRIPT (editable)",
            Font = new Font("Segoe UI", 7.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(24, 246)
        });

        _scriptBox = new TextBox
        {
            Left = 24,
            Top = 266,
            Width = 570,
            Height = 200,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 9.5f),
            Text = BuildScript(customerName, segment, revenue),
            BackColor = Color.FromArgb(250, 251, 253)
        };
        Controls.Add(_scriptBox);

        // ---- Log status ----
        _lblLogStatus = new Label
        {
            Left = 24,
            Top = 476,
            Width = 570,
            Height = 20,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(110, 118, 132),
            Text = ""
        };
        Controls.Add(_lblLogStatus);

        // ---- Action buttons ----
        var btnCopy = new Button
        {
            Text = "📋  Copy Script",
            Left = 24,
            Top = 502,
            Width = 130,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCopy.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        btnCopy.Click += (_, _) =>
        {
            Clipboard.SetText(_scriptBox.Text);
            _lblLogStatus.Text = "Script copied to clipboard.";
            _lblLogStatus.ForeColor = Color.FromArgb(110, 118, 132);
        };
        Controls.Add(btnCopy);

        _btnLog = new Button
        {
            Text = "✓  Log Activity",
            Left = 164,
            Top = 502,
            Width = 160,
            Height = 36,
            BackColor = Color.FromArgb(255, 168, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnLog.FlatAppearance.BorderSize = 0;
        _btnLog.Click += async (_, _) => await LogActivityAsync();
        Controls.Add(_btnLog);

        var btnClose = new Button
        {
            Text = "Close",
            Left = 520,
            Top = 502,
            Width = 74,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f),
            DialogResult = DialogResult.Cancel
        };
        btnClose.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        Controls.Add(btnClose);
    }

    private async Task LogActivityAsync()
    {
        _btnLog.Enabled = false;
        _btnLog.Text = "Logging...";
        _lblLogStatus.Text = "Saving activity to database...";
        _lblLogStatus.ForeColor = Color.FromArgb(110, 118, 132);

        try
        {
            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/bi/retention/actions";

            var payload = new
            {
                customerId = _customerId,
                segment = _segment,
                actionTaken = _action,
                basis = "",
                script = _scriptBox.Text,
                followUpDate = (DateTime?)null
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
            req.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json");

            using var res = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                string msg = json;
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("message", out var m))
                        msg = m.GetString() ?? json;
                }
                catch { }
                throw new HttpRequestException(msg);
            }

            LoggedActivity = true;
            _lblLogStatus.Text = "✓ Activity logged successfully.";
            _lblLogStatus.ForeColor = Color.FromArgb(34, 140, 78);
            _btnLog.Text = "✓ Logged";

            await Task.Delay(800);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblLogStatus.Text = "✗ Failed: " + ex.Message;
            _lblLogStatus.ForeColor = Color.FromArgb(200, 55, 55);
            _btnLog.Enabled = true;
            _btnLog.Text = "✓  Log Activity";
        }
    }

    private static Color SegmentColor(string segment) => segment switch
    {
        "Champion" => Color.FromArgb(34, 140, 78),
        "Loyal" => Color.FromArgb(80, 140, 200),
        "Promising" => Color.FromArgb(160, 130, 60),
        "Detractor" => Color.FromArgb(200, 55, 55),
        "At Risk" => Color.FromArgb(220, 120, 30),
        "Dormant" => Color.FromArgb(140, 140, 140),
        "Lost" => Color.FromArgb(110, 110, 110),
        _ => Color.FromArgb(110, 118, 132)
    };

    private static string BuildScript(string name, string segment, decimal revenue)
    {
        var firstName = name.Split(' ').FirstOrDefault() ?? name;
        var rev = revenue > 0 ? $"\n\nWe truly value your ₱{revenue:N0} partnership." : "";

        return segment switch
        {
            "Champion" => $"Hi {firstName},\n\nThank you for being one of our most valued clients! We loved working with you and we're always thinking of you.{rev}\n\nWe'd love to help with your next project. As a token of our appreciation, we'd like to offer you 15% off your next consultation.\n\nWarm regards,\nFuerto Interior Design Services",

            "Loyal" => $"Hi {firstName},\n\nWe hope you're enjoying your recently completed project! It's been a pleasure serving you.{rev}\n\nAs a loyal client, we'd like to offer you a 10% loyalty discount on your next project. Whenever you're ready, just reply to this message.\n\nBest regards,\nFuerto Interior Design Services",

            "Promising" => $"Hi {firstName},\n\nWe hope you're loving your new space! We'd love to hear how it's working for you.{rev}\n\nAre there any other rooms or spaces you're thinking about refreshing? We're here to help with ideas and a quick consultation.\n\nBest regards,\nFuerto Interior Design Services",

            "Detractor" => $"Hi {firstName},\n\nWe're sorry to hear that your experience didn't meet expectations.{rev}\n\nYour feedback is important to us. We'd like to make things right — can we schedule a brief call to discuss how we can improve?\n\nSincerely,\nFuerto Interior Design Services",

            "At Risk" => $"Hi {firstName},\n\nIt's been a while! We were just thinking about your project and wanted to say hello.{rev}\n\nWe've been working on some exciting new design approaches and we'd love to share them with you. As a warm welcome back, please enjoy 15% off your next project.\n\nWarm regards,\nFuerto Interior Design Services",

            "Dormant" => $"Hi {firstName},\n\nWe hope all is well with you.{rev}\n\nWe wanted to reach out one last time — if there's anything we can help with, we're here. If you'd prefer not to hear from us again, just let us know.\n\nBest regards,\nFuerto Interior Design Services",

            _ => $"Hi {firstName},\n\nWe hope all is well.{rev}\n\nWhenever you're ready for your next project, we're here to help.\n\nBest regards,\nFuerto Interior Design Services"
        };
    }
}