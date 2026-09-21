using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class ResetPasswordDialog : Form
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;
    private readonly string _userId;
    private readonly string _email;

    private TextBox _txtNewPassword = null!;
    private TextBox _txtConfirmPassword = null!;
    private Label _lblStatus = null!;
    private Button _btnReset = null!;

    public ResetPasswordDialog(string apiUrl, HttpClient http, JsonElement user)
    {
        _apiUrl = apiUrl;
        _http = http;
        _userId = GetStr(user, "userId");
        _email = GetStr(user, "email");

        Text = "Reset Password";
        Size = new Size(520, 380);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        Controls.Add(new Label
        {
            Text = "Reset Password",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, 20)
        });

        Controls.Add(new Label
        {
            Text = $"User: {_email}",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(24, 50)
        });

        int y = 90;

        AddLabel("New Password (min 6 characters)", y); y += 22;
        _txtNewPassword = AddText(y); y += 46;

        AddLabel("Confirm New Password", y); y += 22;
        _txtConfirmPassword = AddText(y); y += 46;

        _lblStatus = new Label
        {
            Left = 24,
            Top = y,
            Width = 460,
            Height = 20,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(110, 118, 132),
            Text = ""
        };
        Controls.Add(_lblStatus);
        y += 30;

        _btnReset = new Button
        {
            Text = "Reset Password",
            Left = 340,
            Top = y,
            Width = 144,
            Height = 38,
            BackColor = Color.FromArgb(200, 55, 55),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnReset.FlatAppearance.BorderSize = 0;
        _btnReset.Click += async (_, _) => await ResetAsync();
        Controls.Add(_btnReset);

        var btnCancel = new Button
        {
            Text = "Cancel",
            Left = 240,
            Top = y,
            Width = 90,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        Controls.Add(btnCancel);

        ClientSize = new Size(508, y + 60);
    }

    private void AddLabel(string text, int y)
    {
        Controls.Add(new Label
        {
            Text = text,
            Left = 24,
            Top = y,
            Width = 460,
            Height = 20,
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92)
        });
    }

    private TextBox AddText(int y)
    {
        var t = new TextBox
        {
            Left = 24,
            Top = y,
            Width = 460,
            Height = 28,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle,
            UseSystemPasswordChar = true
        };
        Controls.Add(t);
        return t;
    }

    private async Task ResetAsync()
    {
        if (_txtNewPassword.Text.Length < 6)
        {
            _lblStatus.Text = "✗ Password must be at least 6 characters.";
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
            return;
        }

        if (_txtNewPassword.Text != _txtConfirmPassword.Text)
        {
            _lblStatus.Text = "✗ Passwords do not match.";
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
            return;
        }

        _btnReset.Enabled = false;
        _btnReset.Text = "Resetting...";
        _lblStatus.Text = "Sending request...";
        _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);

        try
        {
            var body = new { newPassword = _txtNewPassword.Text };

            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/users/{_userId}/reset-password";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
            req.Content = new StringContent(
                JsonSerializer.Serialize(body),
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

            MessageBox.Show("Password reset successfully.", "Success");
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "✗ " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
            _btnReset.Enabled = true;
            _btnReset.Text = "Reset Password";
        }
    }

    private static string GetStr(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return "";
        return p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : p.ToString();
    }
}