using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class ActionTemplateDialog : Form
{
    public bool LoggedActivity { get; private set; }

    private readonly int _customerId;
    private readonly string _customerName;
    private readonly string _email;
    private readonly string _segment;
    private readonly string _action;
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private TextBox _scriptBox = null!;
    private ComboBox _cmbOfferType = null!;
    private TextBox _txtOfferValue = null!;
    private TextBox _txtOfferDescription = null!;
    private TextBox _txtNotes = null!;
    private Button _btnLog = null!;
    private Button _btnEmail = null!;
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
        _customerName = customerName;
        _email = email;
        _segment = segment;
        _action = action;

        Text = $"Retention Action — {segment}";
        Size = new Size(680, 780);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);

        // =========================================================
        // HEADER
        // =========================================================
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

        // =========================================================
        // BASIS card
        // =========================================================
        var basisPanel = new Panel
        {
            Left = 24,
            Top = 116,
            Width = 610,
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

        // =========================================================
        // RECOMMENDED ACTION
        // =========================================================
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

        // =========================================================
        // OFFER SECTION (NEW)
        // =========================================================
        Controls.Add(new Label
        {
            Text = "OFFER",
            Font = new Font("Segoe UI", 7.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(24, 244)
        });

        // Offer type
        Controls.Add(new Label
        {
            Text = "Offer / Promotion",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92),
            AutoSize = true,
            Location = new Point(24, 266)
        });

        _cmbOfferType = new ComboBox
        {
            Left = 24,
            Top = 286,
            Width = 260,
            Height = 30,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DropDownWidth = 380,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbOfferType.Items.Add(new PromotionOfferItem { Name = "Loading promotions..." });
        _cmbOfferType.SelectedIndex = 0;
        _cmbOfferType.SelectedIndexChanged += (_, _) => UpdateOfferValueState();
        Controls.Add(_cmbOfferType);

        // Offer value
        Controls.Add(new Label
        {
            Text = "Value",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92),
            AutoSize = true,
            Location = new Point(296, 266)
        });

        _txtOfferValue = new TextBox
        {
            Left = 296,
            Top = 286,
            Width = 80,
            Height = 30,
            Font = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.FixedSingle,
            Text = "10"
        };
        Controls.Add(_txtOfferValue);

        // Offer description
        Controls.Add(new Label
        {
            Text = "Description (optional)",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92),
            AutoSize = true,
            Location = new Point(388, 266)
        });

        _txtOfferDescription = new TextBox
        {
            Left = 388,
            Top = 286,
            Width = 246,
            Height = 30,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "e.g. 15% off next project"
        };
        Controls.Add(_txtOfferDescription);

        // =========================================================
        // SCRIPT
        // =========================================================
        Controls.Add(new Label
        {
            Text = "SCRIPT (editable)",
            Font = new Font("Segoe UI", 7.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(24, 328)
        });

        _scriptBox = new TextBox
        {
            Left = 24,
            Top = 348,
            Width = 610,
            Height = 140,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 9.5f),
            Text = BuildScript(customerName, segment, revenue),
            BackColor = Color.FromArgb(250, 251, 253)
        };
        Controls.Add(_scriptBox);

        // =========================================================
        // NOTES
        // =========================================================
        Controls.Add(new Label
        {
            Text = "NOTES (internal)",
            Font = new Font("Segoe UI", 7.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(24, 498)
        });

        _txtNotes = new TextBox
        {
            Left = 24,
            Top = 518,
            Width = 610,
            Height = 50,
            Multiline = true,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Any internal notes (not shown to customer)"
        };
        Controls.Add(_txtNotes);

        // =========================================================
        // STATUS
        // =========================================================
        _lblLogStatus = new Label
        {
            Left = 24,
            Top = 578,
            Width = 610,
            Height = 20,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(110, 118, 132),
            Text = ""
        };
        Controls.Add(_lblLogStatus);

        // =========================================================
        // BUTTONS
        // =========================================================
        var btnCopy = new Button
        {
            Text = "📋  Copy Script",
            Left = 24,
            Top = 608,
            Width = 140,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(28, 32, 40),
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
            Text = "✓  Log Retention",
            Left = 160,
            Top = 608,
            Width = 145,
            Height = 38,
            BackColor = Color.FromArgb(255, 168, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnLog.FlatAppearance.BorderSize = 0;
        _btnLog.Click += async (_, _) => await LogActivityAsync();
        Controls.Add(_btnLog);

        _btnEmail = new Button
        {
            Text = "✉  Send via Email",
            Left = 315,
            Top = 608,
            Width = 160,
            Height = 38,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnEmail.FlatAppearance.BorderSize = 0;
        _btnEmail.Click += async (_, _) => await SendEmailAsync();
        Controls.Add(_btnEmail);

        var btnClose = new Button
        {
            Text = "Close",
            Left = 485,
            Top = 608,
            Width = 90,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(28, 32, 40),
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel
        };
        btnClose.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        Controls.Add(btnClose);

        ClientSize = new Size(668, 662);

        Shown += async (_, _) => await LoadPromotionsAsync();
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

            PromotionOfferItem? bestMatch = null;

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

                    if (bestMatch == null)
                    {
                        if (string.Equals(targetSeg, _segment, StringComparison.OrdinalIgnoreCase))
                            bestMatch = item;
                        else if (string.Equals(targetSeg, "Any", StringComparison.OrdinalIgnoreCase))
                            bestMatch = item;
                    }
                    else if (!string.Equals(bestMatch.TargetSegment, _segment, StringComparison.OrdinalIgnoreCase)
                             && string.Equals(targetSeg, _segment, StringComparison.OrdinalIgnoreCase))
                    {
                        bestMatch = item;
                    }
                }
            }

            // Always provide custom fallbacks
            _cmbOfferType.Items.Add(new PromotionOfferItem { PromotionId = null, Name = "Custom — % Discount", OfferType = "Percentage", OfferValue = 10 });
            _cmbOfferType.Items.Add(new PromotionOfferItem { PromotionId = null, Name = "Custom — ₱ Fixed Amount", OfferType = "FixedAmount", OfferValue = 500 });
            _cmbOfferType.Items.Add(new PromotionOfferItem { PromotionId = null, Name = "Custom — Free Service", OfferType = "FreeService", OfferValue = 0 });
            _cmbOfferType.Items.Add(new PromotionOfferItem { PromotionId = null, Name = "Custom — Other", OfferType = "Custom", OfferValue = null });

            if (bestMatch != null)
                _cmbOfferType.SelectedItem = bestMatch;
            else if (_cmbOfferType.Items.Count > 0)
                _cmbOfferType.SelectedIndex = 0;

            _cmbOfferType.EndUpdate();
            UpdateOfferValueState();
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
        UpdateOfferValueState();
    }

    private void UpdateOfferValueState()
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
    // LOG
    // =========================================================
    private async Task LogActivityAsync()
    {
        _btnLog.Enabled = false;
        _btnLog.Text = "Logging...";
        _lblLogStatus.Text = "Saving retention action...";
        _lblLogStatus.ForeColor = Color.FromArgb(110, 118, 132);

        try
        {
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

            var payload = new
            {
                customerId = _customerId,
                segment = _segment,
                actionTaken = string.IsNullOrWhiteSpace(offerDescription) ? _action : offerDescription,
                basis = "",
                script = _scriptBox.Text,
                followUpDate = (DateTime?)null,

                // ---- Offer fields ----
                offerType = offerType,
                offerValue = offerValue,
                offerDescription = offerDescription,
                promotionId = promotionId,

                // ---- Notes ----
                notes = _txtNotes.Text.Trim(),

                // ---- Source ----
                source = "Automated"
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

            LoggedActivity = true;
            _lblLogStatus.Text = "✓ Retention logged successfully.";
            _lblLogStatus.ForeColor = Color.FromArgb(34, 140, 78);
            _btnLog.Text = "✓ Logged";

            await Task.Delay(800);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblLogStatus.Text = "✗ " + ex.Message;
            _lblLogStatus.ForeColor = Color.FromArgb(200, 55, 55);
            _btnLog.Enabled = true;
            _btnLog.Text = "✓  Log Retention";
        }
    }

    private async Task SendEmailAsync()
    {
        if (string.IsNullOrWhiteSpace(_email))
        {
            MessageBox.Show("Customer has no email address on file.", "Cannot Send Email", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string company = Session.CompanyName ?? "Fuerto CRM";
        string subject = $"Exclusive Offer for {_customerName} — {_segment} Customer Appreciation";
        string offerDesc = _txtOfferDescription.Text.Trim();
        string body = $"Dear {_customerName},\n\n{_scriptBox.Text}\n\n[PROMOTIONAL OFFER]: {offerDesc}\n\nWarm regards,\n{company}";

        using var emailDlg = new CrmModalDialog(
            "Send Retention Email",
            $"Dispatch retention incentive directly to {_customerName}",
            "✉",
            "Send Email",
            600);

        var txtTo = emailDlg.AddTextField("Recipient Email", "", _email, true);
        txtTo.ReadOnly = true;
        var txtSubj = emailDlg.AddTextField("Email Subject *", "Subject...", subject, true);
        var txtBody = emailDlg.AddTextAreaField("Email Message Body *", "Enter email content...", 160, body);

        if (emailDlg.ShowDialog(this) == DialogResult.OK)
        {
            _txtNotes.Text = $"[EMAIL SENT TO {_email}]: {txtSubj.Text}\n" + _txtNotes.Text;
            await LogActivityAsync();
            MessageBox.Show($"Retention email successfully dispatched to {_email}!\nRetention action logged.", "Email Sent Successfully", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================
    private static Color SegmentColor(string segment) => segment switch
    {
        "Champion" => Color.FromArgb(34, 140, 78),
        "Loyal" => Color.FromArgb(80, 140, 200),
        "Promising" => Color.FromArgb(160, 130, 60),
        "Detractor" => Color.FromArgb(200, 55, 55),
        "At Risk" => Color.FromArgb(220, 120, 30),
        "Dormant" => Color.FromArgb(140, 140, 140),
        "Lost" => Color.FromArgb(110, 110, 110),
        "Manual" => Color.FromArgb(140, 80, 190),
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

            "Manual" => $"Hi {firstName},\n\nWe hope all is well! As a valued customer, we'd like to offer you a special discount on your next project.{rev}\n\nWe truly appreciate your continued support and would love to work with you again.\n\nWarm regards,\nFuerto Interior Design Services",

            _ => $"Hi {firstName},\n\nWe hope all is well.{rev}\n\nWhenever you're ready for your next project, we're here to help.\n\nBest regards,\nFuerto Interior Design Services"
        };
    }
}