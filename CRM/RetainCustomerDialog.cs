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
    private ComboBox _cmbCustomerStatus = null!;
    private ComboBox _cmbOfferType = null!;
    private TextBox _txtOfferValue = null!;
    private TextBox _txtOfferDescription = null!;
    private TextBox _txtNotes = null!;
    private Button _btnLog = null!;
    private Button _btnEmail = null!;
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
            Text = "Offer / Promotion",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92),
            AutoSize = true,
            Location = new Point(24, 408)
        });

        _cmbOfferType = new ComboBox
        {
            Left = 24,
            Top = 430,
            Width = 210,
            Height = 30,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DropDownWidth = 380,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbOfferType.Items.Add(new PromotionOfferItem { Name = "Loading promotions..." });
        _cmbOfferType.SelectedIndex = 0;
        _cmbOfferType.SelectedIndexChanged += (_, _) => UpdateValueField();
        Controls.Add(_cmbOfferType);

        Controls.Add(new Label
        {
            Text = "Value",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92),
            AutoSize = true,
            Location = new Point(244, 408)
        });

        _txtOfferValue = new TextBox
        {
            Left = 244,
            Top = 430,
            Width = 65,
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
            Location = new Point(319, 408)
        });

        _txtOfferDescription = new TextBox
        {
            Left = 319,
            Top = 430,
            Width = 185,
            Height = 30,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "e.g. 15% off next project"
        };
        Controls.Add(_txtOfferDescription);

        Controls.Add(new Label
        {
            Text = "Status / Tier",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92),
            AutoSize = true,
            Location = new Point(514, 408)
        });

        _cmbCustomerStatus = new ComboBox
        {
            Left = 514,
            Top = 430,
            Width = 170,
            Height = 30,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbCustomerStatus.Items.AddRange(new object[]
        {
            "Champion",
            "Loyal",
            "Promising",
            "Active",
            "At Risk",
            "Detractor",
            "Dormant",
            "Lost",
            "VIP",
            "Regular"
        });
        _cmbCustomerStatus.SelectedIndex = 3;
        Controls.Add(_cmbCustomerStatus);

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

        _btnEmail = new Button
        {
            Text = "✉  Send via Email",
            Left = 385,
            Top = 596,
            Width = 155,
            Height = 40,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnEmail.FlatAppearance.BorderSize = 0;
        _btnEmail.Click += async (_, _) => await SendEmailAsync();
        Controls.Add(_btnEmail);

        var btnCancel = new Button
        {
            Text = "Cancel",
            Left = 285,
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
        Load += async (_, _) =>
        {
            await Task.WhenAll(LoadCustomersAsync(), LoadPromotionsAsync());
        };
    }

    private async Task LoadPromotionsAsync()
    {
        try
        {
            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/promotions?activeOnly=true";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

            using var res = await _http.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                PopulateFallbackOffers();
                return;
            }

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            _cmbOfferType.BeginUpdate();
            _cmbOfferType.Items.Clear();

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var elem in doc.RootElement.EnumerateArray())
                {
                    var promoId = elem.TryGetProperty("promotionId", out var idProp) ? idProp.GetInt32() : 0;
                    var name = elem.TryGetProperty("name", out var nProp) ? nProp.GetString() ?? "" : "";
                    var code = elem.TryGetProperty("code", out var cProp) ? cProp.GetString() ?? "" : "";
                    var desc = elem.TryGetProperty("description", out var dProp) ? dProp.GetString() ?? "" : "";
                    var offerType = elem.TryGetProperty("offerType", out var tProp) ? tProp.GetString() ?? "Percentage" : "Percentage";
                    decimal? offerVal = elem.TryGetProperty("offerValue", out var vProp) && vProp.ValueKind == JsonValueKind.Number ? vProp.GetDecimal() : null;
                    var targetSeg = elem.TryGetProperty("targetSegment", out var sProp) ? sProp.GetString() ?? "Any" : "Any";

                    var item = new PromotionOfferItem
                    {
                        PromotionId = promoId,
                        Name = name,
                        Code = code,
                        Description = desc,
                        OfferType = offerType,
                        OfferValue = offerVal,
                        TargetSegment = targetSeg
                    };

                    _cmbOfferType.Items.Add(item);
                }
            }

            // Always provide custom fallbacks
            _cmbOfferType.Items.Add(new PromotionOfferItem { PromotionId = null, Name = "Custom — % Discount", OfferType = "Percentage", OfferValue = 10 });
            _cmbOfferType.Items.Add(new PromotionOfferItem { PromotionId = null, Name = "Custom — ₱ Fixed Amount", OfferType = "FixedAmount", OfferValue = 500 });
            _cmbOfferType.Items.Add(new PromotionOfferItem { PromotionId = null, Name = "Custom — Free Service", OfferType = "FreeService", OfferValue = 0 });
            _cmbOfferType.Items.Add(new PromotionOfferItem { PromotionId = null, Name = "Custom — Other", OfferType = "Custom", OfferValue = null });

            if (_cmbOfferType.Items.Count > 0)
                _cmbOfferType.SelectedIndex = 0;

            _cmbOfferType.EndUpdate();
            UpdateValueField();
        }
        catch
        {
            PopulateFallbackOffers();
        }
    }

    private void PopulateFallbackOffers()
    {
        _cmbOfferType.BeginUpdate();
        _cmbOfferType.Items.Clear();
        _cmbOfferType.Items.Add(new PromotionOfferItem { PromotionId = null, Name = "Custom — % Discount", OfferType = "Percentage", OfferValue = 10 });
        _cmbOfferType.Items.Add(new PromotionOfferItem { PromotionId = null, Name = "Custom — ₱ Fixed Amount", OfferType = "FixedAmount", OfferValue = 500 });
        _cmbOfferType.Items.Add(new PromotionOfferItem { PromotionId = null, Name = "Custom — Free Service", OfferType = "FreeService", OfferValue = 0 });
        _cmbOfferType.Items.Add(new PromotionOfferItem { PromotionId = null, Name = "Custom — Other", OfferType = "Custom" });
        _cmbOfferType.SelectedIndex = 0;
        _cmbOfferType.EndUpdate();
        UpdateValueField();
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

        // Auto-select matching promotion if available
        if (!string.IsNullOrWhiteSpace(segment))
        {
            foreach (var obj in _cmbOfferType.Items)
            {
                if (obj is PromotionOfferItem p && p.PromotionId != null)
                {
                    if (string.Equals(p.TargetSegment, segment, StringComparison.OrdinalIgnoreCase))
                    {
                        _cmbOfferType.SelectedItem = p;
                        break;
                    }
                }
            }

            if (_cmbCustomerStatus != null)
            {
                for (int i = 0; i < _cmbCustomerStatus.Items.Count; i++)
                {
                    if (string.Equals(_cmbCustomerStatus.Items[i].ToString(), segment, StringComparison.OrdinalIgnoreCase))
                    {
                        _cmbCustomerStatus.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
    }

    private void UpdateValueField()
    {
        if (_cmbOfferType.SelectedItem is not PromotionOfferItem item)
            return;

        if (item.PromotionId != null)
        {
            _txtOfferValue.Text = item.OfferValue.HasValue ? item.OfferValue.Value.ToString("0.##") : "";
            _txtOfferValue.Enabled = false;
            _txtOfferDescription.Text = !string.IsNullOrWhiteSpace(item.Description)
                ? item.Description
                : (!string.IsNullOrEmpty(item.Code) ? $"{item.Name} ({item.Code})" : item.Name);
        }
        else
        {
            _txtOfferValue.Enabled = item.OfferType == "Percentage" || item.OfferType == "FixedAmount";
            if (item.OfferType == "FreeService")
                _txtOfferValue.Text = "0";
            else if (string.IsNullOrWhiteSpace(_txtOfferValue.Text))
                _txtOfferValue.Text = item.OfferValue?.ToString("0.##") ?? "10";

            _txtOfferDescription.Text = "";
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
        var selectedPromo = _cmbOfferType.SelectedItem as PromotionOfferItem;
        var offerType = selectedPromo?.OfferType ?? "Percentage";
        int? promotionId = selectedPromo?.PromotionId;

        decimal? offerValue = null;
        if (selectedPromo?.PromotionId != null)
        {
            offerValue = selectedPromo.OfferValue;
        }
        else if (_txtOfferValue.Enabled && decimal.TryParse(_txtOfferValue.Text, out var v))
        {
            offerValue = v;
        }

        var offerDescription = _txtOfferDescription.Text.Trim();
        if (string.IsNullOrWhiteSpace(offerDescription))
        {
            if (selectedPromo?.PromotionId != null)
            {
                offerDescription = !string.IsNullOrEmpty(selectedPromo.Code)
                    ? $"{selectedPromo.Name} ({selectedPromo.Code})"
                    : selectedPromo.Name;
            }
            else if (offerValue.HasValue)
            {
                offerDescription = offerType switch
                {
                    "Percentage" => $"{offerValue}% discount",
                    "FixedAmount" => $"₱{offerValue:N0} discount",
                    "FreeService" => "Free consultation",
                    _ => "Custom offer"
                };
            }
        }

        var customerName = GetStr(customer, "fullName");
        var customerEmail = GetStr(customer, "email");
        var customerPhone = GetStr(customer, "phone");
        var chosenStatus = _cmbCustomerStatus?.SelectedItem?.ToString();
        var segment = !string.IsNullOrWhiteSpace(chosenStatus) ? chosenStatus : GetStr(customer, "segment");
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
                promotionId = promotionId,
                newCustomerStatus = chosenStatus,
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

    private async Task SendEmailAsync()
    {
        if (_selectedCustomer is null)
        {
            MessageBox.Show("Select a customer first.", "Retain Customer");
            return;
        }

        var customer = _selectedCustomer.Value;
        var custName = GetStr(customer, "fullName");
        var custEmail = GetStr(customer, "email");
        var revenue = GetDecimal(customer, "totalRevenue");
        var chosenStatus = _cmbCustomerStatus?.SelectedItem?.ToString() ?? GetStr(customer, "segment");

        string emailToUse = custEmail;
        if (string.IsNullOrWhiteSpace(emailToUse))
        {
            using var prompt = new CrmModalDialog("Customer Email", $"Enter email address for {custName}:", "✉", "Proceed", 420);
            var txtManual = prompt.AddTextField("Email Address *", "name@example.com", "", true);
            if (prompt.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(txtManual.Text))
            {
                return;
            }
            emailToUse = txtManual.Text.Trim();
        }

        string company = Session.CompanyName ?? "Fuerto CRM";
        string offerDesc = _txtOfferDescription.Text.Trim();
        if (string.IsNullOrWhiteSpace(offerDesc))
        {
            offerDesc = _txtOfferValue.Text.Trim() + "% off next service";
        }

        string subject = $"Exclusive Offer for {custName} — {chosenStatus} Appreciation";
        string body = $"Dear {custName},\n\n" +
                      $"As a valued customer ({chosenStatus}), we are excited to extend an exclusive offer: {offerDesc}.\n\n" +
                      $"We truly appreciate your partnership with {company}.\n\n" +
                      $"Warm regards,\n{company}";

        using var emailDlg = new CrmModalDialog(
            "Send Retention Email",
            $"Dispatch retention incentive directly to {custName} ({chosenStatus})",
            "✉",
            "Send Email",
            600);

        var txtTo = emailDlg.AddTextField("Recipient Email", "", emailToUse, true);
        txtTo.ReadOnly = true;
        var txtSubj = emailDlg.AddTextField("Email Subject *", "Subject...", subject, true);
        var txtBody = emailDlg.AddTextAreaField("Email Message Body *", "Enter email content...", 160, body);

        if (!EmailSettings.Current.IsConfigured)
        {
            var promptConfig = MessageBox.Show(
                "To deliver real emails to your customers, your Gmail sender account must be configured.\n\nWould you like to configure your Gmail SMTP settings now?",
                "Email Configuration Required",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (promptConfig == DialogResult.Yes)
            {
                using var configDlg = new EmailSettingsDialog();
                configDlg.ShowDialog(this);
            }

            if (!EmailSettings.Current.IsConfigured)
            {
                return;
            }
        }

        if (emailDlg.ShowDialog(this) == DialogResult.OK)
        {
            Cursor = Cursors.WaitCursor;
            var (success, sendMsg) = await EmailService.SendEmailAsync(emailToUse, txtSubj.Text, txtBody.Text, custName);
            Cursor = Cursors.Default;

            if (success)
            {
                _txtNotes.Text = $"[REAL EMAIL SENT TO {emailToUse}]: {txtSubj.Text}\n" + _txtNotes.Text;
                await LogRetentionAsync();
                MessageBox.Show(
                    $"✅ Retention email successfully delivered to {emailToUse} via Gmail!\n\nRetention activity recorded in CRM database.",
                    "Email Delivered Successfully",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                var askRetry = MessageBox.Show(
                    $"Could not deliver email via Gmail:\n\n{sendMsg}\n\n• Click 'Retry' to open Email Settings and fix your credentials.\n• Click 'Ignore' to record this retention activity offline without sending email.\n• Click 'Abort' to cancel.",
                    "Email Delivery Failed",
                    MessageBoxButtons.AbortRetryIgnore,
                    MessageBoxIcon.Warning);

                if (askRetry == DialogResult.Retry)
                {
                    using var configDlg = new EmailSettingsDialog();
                    if (configDlg.ShowDialog(this) == DialogResult.OK)
                    {
                        // Retry sending with new settings
                        Cursor = Cursors.WaitCursor;
                        var (retrySuccess, retryMsg) = await EmailService.SendEmailAsync(emailToUse, txtSubj.Text, txtBody.Text, custName);
                        Cursor = Cursors.Default;

                        if (retrySuccess)
                        {
                            _txtNotes.Text = $"[REAL EMAIL SENT TO {emailToUse}]: {txtSubj.Text}\n" + _txtNotes.Text;
                            await LogRetentionAsync();
                            MessageBox.Show($"✅ Email successfully delivered to {emailToUse} via Gmail!\nRetention action logged.", "Email Delivered", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        else
                        {
                            MessageBox.Show($"Delivery still failed: {retryMsg}", "Delivery Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else if (askRetry == DialogResult.Ignore)
                {
                    _txtNotes.Text = $"[EMAIL LOGGED OFFLINE FOR {emailToUse}]: {txtSubj.Text}\n" + _txtNotes.Text;
                    await LogRetentionAsync();
                    MessageBox.Show("Retention activity recorded in CRM (email pending or logged offline).", "Action Logged", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
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