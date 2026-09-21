using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class EditUserDialog : Form
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;
    private readonly string _userId;

    private TextBox _txtEmail = null!;
    private TextBox _txtFullName = null!;
    private ComboBox _cmbRole = null!;
    private CheckBox _chkActive = null!;
    private Label _lblStatus = null!;
    private Button _btnSave = null!;

    public EditUserDialog(string apiUrl, HttpClient http, JsonElement user)
    {
        _apiUrl = apiUrl;
        _http = http;
        _userId = GetStr(user, "userId");

        Text = "Edit User";
        Size = new Size(520, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        Controls.Add(new Label
        {
            Text = "Edit User",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, 20)
        });

        int y = 60;

        AddLabel("Full Name", y); y += 22;
        _txtFullName = AddText(y); _txtFullName.Text = GetStr(user, "fullName"); y += 46;

        AddLabel("Email", y); y += 22;
        _txtEmail = AddText(y); _txtEmail.Text = GetStr(user, "email"); y += 46;

        AddLabel("Role", y); y += 22;

        var currentRole = GetStr(user, "role");

        _cmbRole = new ComboBox
        {
            Left = 24,
            Top = y,
            Width = 460,
            Height = 28,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };

        // Build the allowed roles for this dialog based on the actor's role
        var actorRoles = Session.Roles ?? new List<string>();
        var actorIsAdmin = actorRoles.Contains("Super Admin") || actorRoles.Contains("Admin");

        if (actorIsAdmin)
        {
            _cmbRole.Items.AddRange(new object[] { "Admin", "Manager", "Staff", "Designer" });
        }
        else
        {
            // Manager can only assign Staff or Designer
            _cmbRole.Items.AddRange(new object[] { "Staff", "Designer" });
        }

        var idx = _cmbRole.Items.IndexOf(currentRole);
        if (idx >= 0) _cmbRole.SelectedIndex = idx;
        else _cmbRole.SelectedIndex = 0;

        Controls.Add(_cmbRole);
        y += 46;

        _chkActive = new CheckBox
        {
            Left = 24,
            Top = y,
            Width = 460,
            Height = 24,
            Text = "User is active (can log in)",
            Font = new Font("Segoe UI", 9.5f),
            Checked = GetBool(user, "isActive")
        };
        Controls.Add(_chkActive);
        y += 46;

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

        _btnSave = new Button
        {
            Text = "Save Changes",
            Left = 340,
            Top = y,
            Width = 144,
            Height = 38,
            BackColor = Color.FromArgb(255, 168, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += async (_, _) => await SaveAsync();
        Controls.Add(_btnSave);

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
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(t);
        return t;
    }

    private async Task SaveAsync()
    {
        _btnSave.Enabled = false;
        _btnSave.Text = "Saving...";
        _lblStatus.Text = "Sending update...";
        _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);

        try
        {
            var body = new
            {
                email = _txtEmail.Text.Trim(),
                fullName = _txtFullName.Text.Trim(),
                role = _cmbRole.SelectedItem?.ToString() ?? "Staff",
                isActive = _chkActive.Checked
            };

            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/users/{_userId}";
            using var req = new HttpRequestMessage(HttpMethod.Put, url);
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

            _lblStatus.Text = "✓ User updated.";
            _lblStatus.ForeColor = Color.FromArgb(34, 140, 78);
            await Task.Delay(600);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "✗ " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
            _btnSave.Enabled = true;
            _btnSave.Text = "Save Changes";
        }
    }

    private static string GetStr(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return "";
        return p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : p.ToString();
    }

    private static bool GetBool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return false;
        return p.ValueKind == JsonValueKind.True;
    }
}