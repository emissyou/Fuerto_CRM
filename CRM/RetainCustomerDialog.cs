using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class RetainCustomerDialog : Form
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    public bool Logged { get; private set; }

    // ---- State ----
    private List<JsonElement> _allCustomers = new();
    private List<JsonElement> _filteredCustomers = new();
    private JsonElement? _selectedCustomer = null;

    // ---- Controls ----
    private TextBox _txtSearch = null!;
    private ListView _lstCustomers = null!;
    private Label _lblSelected = null!;
    private ComboBox _cmbOfferType = null!;
    private TextBox _txtOfferValue = null!;
    private TextBox _txtOfferDescription = null!;
    private TextBox _txtNotes = null!;
    private Button _btnLog = null!;
    private Label _lblStatus = null!;

    public RetainCustomerDialog(string apiUrl, HttpClient http)
    {
        _apiUrl = apiUrl;
        _http = http;

        Text = "Retain Any Customer";
        Size = new Size(720, 720);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);

        // =========================================================
        // Header
        // =========================================================
        Controls.Add(new Label
        {
            Text = "Retain Any Customer",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(20, 16)
        });

        Controls.Add(new Label
        {
            Text = "Offer a discount or service to any customer — even if they weren't flagged.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(22, 46)
        });

        // =========================================================
        // Search
        // =========================================================
        Controls.Add(new Label
        {
            Text = "Search Customer",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92),
            AutoSize = true,
            Location = new Point(24, 80)
        });

        _txtSearch = new TextBox
        {
            Left = 24,
            Top = 102,
            Width = 660,
            Height = 30,
            PlaceholderText = "Search by name, email, or phone...",
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle
        };
        _txtSearch.TextChanged += (_, _) => FilterCustomers();
        Controls.Add(_txtSearch);

        // =========================================================
        // Customer list
        // =========================================================
        _lstCustomers = new ListView
        {
            Left = 24,
            Top = 142,
            Width = 660,
            Height = 220,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9f),
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };

        _lstCustomers.Columns.Add("NAME", 180);
        _lstCustomers.Columns.Add("EMAIL", 200);
        _lstCustomers.Columns.Add("PHONE", 110);
        _lstCustomers.Columns.Add("REVENUE", 90);
        _lstCustomers.Columns.Add("SEGMENT", 80);

        _lstCustomers.SelectedIndexChanged += (_, _) => SelectCustomer();
        _lstCustomers.DoubleClick += (_, _) => SelectCustomer();

        Controls.Add(_lstCustomers);

        // =========================================================
        // Selected customer display
        // =========================================================
        _lblSelected = new Label
        {
            Left = 24,
            Top = 372,
            Width = 660,
            Height = 22,
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            ForeColor = Color.FromArgb(110, 118, 132),
            Text = "No customer selected."
        };
        Controls.Add(_lblSelected);

        // =========================================================
        // Offer section
        // =========================================================
        Controls.Add(new Label
        {
            Text = "Offer Type",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92),
            AutoSize = true,
            Location = new Point(24, 408)
        });

        _cmbOfferType = new ComboBox
        {
            Left = 24,
            Top = 430,
            Width = 200,
            Height = 30,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbOfferType.Items.AddRange(new object[]
        {
            "% Discount",
            "₱ Fixed Amount",
            "Free Service",
            "Custom"
        });
        _cmbOfferType.SelectedIndex = 0;
        _cmbOfferType.SelectedIndexChanged += (_, _) => UpdateValueField();
        Controls.Add(_cmbOfferType);

        Controls.Add(new Label
        {
            Text = "Value",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92),
            AutoSize = true,
            Location = new Point(240, 408)
        });

        _txtOfferValue = new TextBox
        {
            Left = 240,
            Top = 430,
            Width = 130,
            Height = 30,
            Font = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.FixedSingle,
            Text = "10"
        };
        Controls.Add(_txtOfferValue);

        Controls.Add(new Label
        {
            Text = "Description (optional)",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92),
            AutoSize = true,
            Location = new Point(384, 408)
        });

        _txtOfferDescription = new TextBox
        {
            Left = 384,
            Top = 430,
            Width = 300,
            Height = 30,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "e.g. 15% off next project"
        };
        Controls.Add(_txtOfferDescription);

        // =========================================================
        // Notes
        // =========================================================
        Controls.Add(new Label
        {
            Text = "Notes (optional)",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92),
            AutoSize = true,
            Location = new Point(24, 476)
        });

        _txtNotes = new TextBox
        {
            Left = 24,
            Top = 498,
            Width = 660,
            Height = 60,
            Multiline = true,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Any context or follow-up notes"
        };
        Controls.Add(_txtNotes);

        // =========================================================
        // Status
        // =========================================================
        _lblStatus = new Label
        {
            Left = 24,
            Top = 568,
            Width = 660,
            Height = 20,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(110, 118, 132),
            Text = ""
        };
        Controls.Add(_lblStatus);

        // =========================================================
        // Buttons
        // =========================================================
        _btnLog = new Button
        {
            Text = "Log Retention →",
            Left = 550,
            Top = 596,
            Width = 134,
            Height = 40,
            BackColor = Color.FromArgb(255, 168, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnLog.FlatAppearance.BorderSize = 0;
        _btnLog.Click += async (_, _) => await LogRetentionAsync();
        Controls.Add(_btnLog);

        var btnCancel = new Button
        {
            Text = "Cancel",
            Left = 450,
            Top = 596,
            Width = 90,
            Height = 40,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        Controls.Add(btnCancel);

        ClientSize = new Size(708, 660);

        // Initial load
        Load += async (_, _) => await LoadCustomersAsync();
    }

    // =========================================================
    // LOAD CUSTOMERS
    // =========================================================
    private async Task LoadCustomersAsync()
    {
        _lblStatus.Text = "Loading customers...";
        _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);

        try
        {
            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/bi/retention";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
            using var res = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _lblStatus.Text = $"Failed to load customers: {(int)res.StatusCode}";
                _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
                return;
            }

            using var doc = JsonDocument.Parse(json);
            _allCustomers = doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
            _filteredCustomers = _allCustomers.ToList();

            RenderCustomerList();

            _lblStatus.Text = $"{_allCustomers.Count} customers loaded.";
            _lblStatus.ForeColor = Color.FromArgb(34, 140, 78);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Error: " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
        }
    }

    private void FilterCustomers()
    {
        var search = _txtSearch.Text.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(search))
        {
            _filteredCustomers = _allCustomers.ToList();
        }
        else
        {
            _filteredCustomers = _allCustomers.Where(c =>
            {
                var name = GetStr(c, "fullName").ToLowerInvariant();
                var email = GetStr(c, "email").ToLowerInvariant();
                var phone = GetStr(c, "phone").ToLowerInvariant();

                return name.Contains(search)
                    || email.Contains(search)
                    || phone.Contains(search);
            }).ToList();
        }

        RenderCustomerList();
    }

    private void RenderCustomerList()
    {
        _lstCustomers.BeginUpdate();
        _lstCustomers.Items.Clear();

        foreach (var c in _filteredCustomers.Take(50))
        {
            var name = GetStr(c, "fullName");
            var email = GetStr(c, "email");
            var phone = GetStr(c, "phone");
            var revenue = GetDecimal(c, "totalRevenue");
            var segment = GetStr(c, "segment");

            var item = new ListViewItem(name);
            item.SubItems.Add(email);
            item.SubItems.Add(phone);
            item.SubItems.Add($"₱{revenue / 1000:N0}K");
            item.SubItems.Add(segment);
            item.Tag = c;
            _lstCustomers.Items.Add(item);
        }

        _lstCustomers.EndUpdate();
    }

    private void SelectCustomer()
    {
        if (_lstCustomers.SelectedItems.Count == 0) return;

        _selectedCustomer = (JsonElement)_lstCustomers.SelectedItems[0].Tag;
        var name = GetStr(_selectedCustomer.Value, "fullName");
        var segment = GetStr(_selectedCustomer.Value, "segment");

        _lblSelected.Text = $"Selected: {name}  ·  {segment}  ·  " +
                           $"₱{GetDecimal(_selectedCustomer.Value, "totalRevenue"):N0} lifetime";
        _lblSelected.ForeColor = Color.FromArgb(28, 32, 40);
    }

    private void UpdateValueField()
    {
        var type = _cmbOfferType.SelectedItem?.ToString() ?? "";
        _txtOfferValue.Enabled = type == "% Discount" || type == "₱ Fixed Amount";

        if (type == "Free Service")
        {
            _txtOfferValue.Text = "0";
        }
    }

    // =========================================================
    // LOG RETENTION
    // =========================================================
    private async Task LogRetentionAsync()
    {
        if (_selectedCustomer is null)
        {
            MessageBox.Show("Select a customer first.", "Retain Customer");
            return;
        }

        var customer = _selectedCustomer.Value;
        var custId = GetInt(customer, "customerId");

        // Determine offer
        var offerTypeLabel = _cmbOfferType.SelectedItem?.ToString() ?? "% Discount";
        var offerType = offerTypeLabel switch
        {
            "% Discount" => "Percentage",
            "₱ Fixed Amount" => "FixedAmount",
            "Free Service" => "FreeService",
            _ => "Custom"
        };

        decimal? offerValue = null;
        if (_txtOfferValue.Enabled && decimal.TryParse(_txtOfferValue.Text, out var v))
        {
            offerValue = v;
        }

        var offerDescription = _txtOfferDescription.Text.Trim();
        if (string.IsNullOrWhiteSpace(offerDescription))
        {
            offerDescription = offerType switch
            {
                "Percentage" => $"{offerValue}% discount",
                "FixedAmount" => $"₱{offerValue:N0} discount",
                "FreeService" => "Free consultation",
                _ => "Custom offer"
            };
        }

        var customerName = GetStr(customer, "fullName");
        var customerEmail = GetStr(customer, "email");
        var customerPhone = GetStr(customer, "phone");
        var segment = GetStr(customer, "segment");
        var basis = GetStr(customer, "basis");
        var revenue = GetDecimal(customer, "totalRevenue");

        // Pre-fill script
        var script = $"Hi {customerName.Split(' ').FirstOrDefault()},\n\n" +
                    $"As a valued customer, we'd like to offer you {offerDescription}.\n\n" +
                    $"We truly appreciate your {FormatPeso(revenue)} partnership.\n\n" +
                    $"Warm regards,\nFuerto Interior Design Services";

        _btnLog.Enabled = false;
        _btnLog.Text = "Logging...";
        _lblStatus.Text = "Saving retention action...";
        _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);

        try
        {
            var payload = new
            {
                customerId = custId,
                segment = string.IsNullOrWhiteSpace(segment) ? "Manual" : segment,
                actionTaken = offerDescription,
                basis = basis,
                script = script,
                followUpDate = (DateTime?)null,
                offerType = offerType,
                offerValue = offerValue,
                offerDescription = offerDescription,
                notes = _txtNotes.Text.Trim(),
                source = "Manual"
            };

            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/bi/retention/actions";
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

            _lblStatus.Text = "✓ Retention logged successfully.";
            _lblStatus.ForeColor = Color.FromArgb(34, 140, 78);

            Logged = true;

            await Task.Delay(700);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "✗ " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
            _btnLog.Enabled = true;
            _btnLog.Text = "Log Retention →";
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================
    private static string FormatPeso(decimal value) =>
        value >= 1_000_000 ? $"₱{value / 1_000_000m:F1}M"
        : value >= 1_000 ? $"₱{value / 1000:N0}K"
        : $"₱{value:N0}";

    private static string GetStr(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return "";
        return p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : p.ToString();
    }

    private static int GetInt(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return 0;
        return p.ValueKind == JsonValueKind.Number ? (int)p.GetDouble() : 0;
    }

    private static decimal GetDecimal(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return 0m;
        return p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : 0m;
    }
}