using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class AssignDesignerDialog : Form
{
    public string DesignerId { get; private set; } = "";
    public string Notes { get; private set; } = "";

    private ComboBox _cmbDesigner = null!;
    private TextBox _txtNotes = null!;
    private Label _lblStatus = null!;

    private readonly HttpClient _http;
    private readonly string _apiUrl;

    public AssignDesignerDialog(string apiUrl, HttpClient http)
    {
        _http = http;
        _apiUrl = apiUrl;

        Text = "Assign Designer";
        Size = new Size(520, 380);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        Controls.Add(new Label
        {
            Text = "Assign a Designer to this Project",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, 20)
        });

        Controls.Add(new Label
        {
            Text = "Requires 50% deposit received on the accepted quotation.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(24, 50)
        });

        Controls.Add(new Label
        {
            Text = "Designer",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, 90)
        });

        _cmbDesigner = new ComboBox
        {
            Left = 24,
            Top = 112,
            Width = 456,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        Controls.Add(_cmbDesigner);

        _lblStatus = new Label
        {
            Left = 24,
            Top = 146,
            Width = 456,
            Height = 20,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(110, 118, 132),
            Text = "Loading designers..."
        };
        Controls.Add(_lblStatus);

        Controls.Add(new Label
        {
            Text = "Notes (optional)",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, 178)
        });

        _txtNotes = new TextBox
        {
            Left = 24,
            Top = 200,
            Width = 456,
            Height = 60,
            Multiline = true,
            PlaceholderText = "Priority notes for the designer",
            Font = new Font("Segoe UI", 9.5f)
        };
        Controls.Add(_txtNotes);

        var btnOk = new Button
        {
            Text = "Assign",
            Left = 300,
            Top = 284,
            Width = 90,
            Height = 34,
            BackColor = Color.FromArgb(255, 168, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btnOk.FlatAppearance.BorderSize = 0;
        Controls.Add(btnOk);

        var btnCancel = new Button
        {
            Text = "Cancel",
            Left = 398,
            Top = 284,
            Width = 82,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f)
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(btnCancel);

        btnOk.Click += (_, _) =>
        {
            if (_cmbDesigner.SelectedItem == null)
            {
                MessageBox.Show("Please select a designer.", "Assign Designer",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var item = (DesignerItem)_cmbDesigner.SelectedItem;
            DesignerId = item.UserId;
            Notes = _txtNotes.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        };

        Load += async (_, _) => await LoadDesignersAsync();
    }

    private async Task LoadDesignersAsync()
    {
        try
        {
            if (Session.CompanyId == null)
            {
                _lblStatus.Text = "No company in session.";
                _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
                return;
            }

            var url = $"{_apiUrl}/tenant/{Session.CompanyId.Value}/designers";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

            using var res = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _lblStatus.Text = $"Failed: {(int)res.StatusCode} {res.ReasonPhrase}";
                _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
                return;
            }

            using var doc = JsonDocument.Parse(json);
            var designers = new List<DesignerItem>();

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                designers.Add(new DesignerItem
                {
                    UserId = el.GetProperty("userId").GetString() ?? "",
                    FullName = el.TryGetProperty("fullName", out var fn) ? fn.GetString() ?? "" : "",
                    Email = el.TryGetProperty("email", out var em) ? em.GetString() ?? "" : ""
                });
            }

            _cmbDesigner.Items.Clear();
            foreach (var d in designers)
                _cmbDesigner.Items.Add(d);

            _cmbDesigner.DisplayMember = nameof(DesignerItem.Display);

            if (designers.Count == 0)
            {
                _lblStatus.Text = "No designers registered for this company.";
                _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
            }
            else
            {
                _lblStatus.Text = $"{designers.Count} designer(s) available.";
                _lblStatus.ForeColor = Color.FromArgb(34, 140, 78);
                _cmbDesigner.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Error: " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
        }
    }

    public object ToPayload() => new
    {
        designerId = DesignerId,
        notes = Notes
    };

    private class DesignerItem
    {
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Display => $"{FullName}  ({Email})";
    }
}