using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class NewUserDialog : Form
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private TextBox _txtEmail = null!;
    private TextBox _txtPassword = null!;
    private TextBox _txtFullName = null!;
    private ComboBox _cmbRole = null!;
    private Label _lblStatus = null!;
    private Button _btnCreate = null!;

    public NewUserDialog(string apiUrl, HttpClient http)
    {
        _apiUrl = apiUrl;
        _http = http;

        Text = "Create New User";
        Size = new Size(520, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        Controls.Add(new Label
        {
            Text = "Create New User",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, 20)
        });

        int y = 60;

        AddLabel("Full Name");
        _txtFullName = AddText(y); y += 46;
        AddLabel("Email");
        _txtEmail = AddText(y); y += 46;
        AddLabel("Password (min 6 characters)");
        _txtPassword = new TextBox
        {
            Left = 24,
            Top = y,
            Width = 460,
            Height = 28,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle,
            UseSystemPasswordChar = true
        };
        Controls.Add(_txtPassword);
        y += 46;

        AddLabel("Role");
        _cmbRole = new ComboBox
        {
            Left = 24,
            Top = y,
            Width = 460,
            Height = 28,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        // Manager can only assign Staff/Designer
        // Admin can assign Manager/Staff/Designer
        var roles = Session.Roles ?? new List<string>();
        if (roles.Contains("Super Admin") || roles.Contains("Admin"))
            _cmbRole.Items.AddRange(new object[] { "Manager", "Staff", "Designer" });
        else
            _cmbRole.Items.AddRange(new object[] { "Staff", "Designer" });

        _cmbRole.SelectedIndex = 0;
        Controls.Add(_cmbRole);
        y += 60;

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
        y += 26;

        _btnCreate = new Button
        {
            Text = "Create User",
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
        _btnCreate.FlatAppearance.BorderSize = 0;
        _btnCreate.Click += async (_, _) => await CreateAsync();
        Controls.Add(_btnCreate);

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

    private void AddLabel(string text)
    {
        int y = Controls.OfType<TextBox>().Select(t => t.Bottom).DefaultIfEmpty(56).Max() + 6;
        var lastCtrl = Controls.OfType<Control>().LastOrDefault();
        if (lastCtrl != null) y = lastCtrl.Bottom + 8;

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

    private async Task CreateAsync()
    {
        _btnCreate.Enabled = false;
        _btnCreate.Text = "Creating...";
        _lblStatus.Text = "Sending request...";
        _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);

        try
        {
            var body = new
            {
                email = _txtEmail.Text.Trim(),
                password = _txtPassword.Text,
                fullName = _txtFullName.Text.Trim(),
                role = _cmbRole.SelectedItem?.ToString() ?? "Staff"
            };

            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/users";
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

            _lblStatus.Text = "✓ User created.";
            _lblStatus.ForeColor = Color.FromArgb(34, 140, 78);
            await Task.Delay(600);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "✗ " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
            _btnCreate.Enabled = true;
            _btnCreate.Text = "Create User";
        }
    }
}