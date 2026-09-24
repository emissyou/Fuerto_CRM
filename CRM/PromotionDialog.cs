using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class PromotionDialog : Form
{
    public bool Saved { get; private set; }

    private readonly string _apiUrl;
    private readonly HttpClient _http;
    private readonly int _editPromotionId;   // 0 = create, >0 = edit
    private readonly JsonElement? _existing;

    // Controls
    private TextBox _txtName = null!;
    private TextBox _txtCode = null!;
    private TextBox _txtDescription = null!;
    private ComboBox _cmbOfferType = null!;
    private TextBox _txtOfferValue = null!;
    private ComboBox _cmbTargetSegment = null!;
    private DateTimePicker _dtFrom = null!;
    private DateTimePicker _dtUntil = null!;
    private CheckBox _chkNoFrom = null!;
    private CheckBox _chkNoUntil = null!;
    private NumericUpDown _numMaxUses = null!;
    private CheckBox _chkUnlimited = null!;
    private CheckBox _chkActive = null!;
    private TextBox _txtNotes = null!;
    private Label _lblStatus = null!;
    private Button _btnSave = null!;

    public PromotionDialog(string apiUrl, HttpClient http, JsonElement? existing = null)
    {
        _apiUrl = apiUrl;
        _http = http;
        _existing = existing;

        if (existing.HasValue && existing.Value.ValueKind == JsonValueKind.Object)
        {
            _editPromotionId = existing.Value.TryGetProperty("promotionId", out var pid)
                ? pid.GetInt32() : 0;
        }

        Text = _editPromotionId > 0 ? "Edit Promotion" : "New Promotion";
        Size = new Size(620, 720);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);

        Controls.Add(new Label
        {
            Text = _editPromotionId > 0 ? "Edit Promotion" : "New Promotion",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, 20)
        });

        Controls.Add(new Label
        {
            Text = "Promotions can be applied from the Retention dialog.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(26, 50)
        });

        int y = 84;

        // ---- Name ----
        AddLabel("Promotion Name *", y); y += 22;
        _txtName = AddText(y); y += 46;

        // ---- Code ----
        AddLabel("Promo Code (optional)", y); y += 22;
        _txtCode = AddText(y); y += 46;

        // ---- Description ----
        AddLabel("Description", y); y += 22;
        _txtDescription = new TextBox
        {
            Left = 24,
            Top = y,
            Width = 560,
            Height = 50,
            Multiline = true,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(_txtDescription);
        y += 62;

        // ---- Offer Type + Value ----
        AddLabel("Offer Type", y);
        AddLabelAt("Offer Value", 300, y);
        y += 22;

        _cmbOfferType = new ComboBox
        {
            Left = 24,
            Top = y,
            Width = 260,
            Height = 30,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbOfferType.Items.AddRange(new object[]
        {
            "% Percentage",
            "₱ Fixed Amount",
            "Free Service",
            "Custom"
        });
        _cmbOfferType.SelectedIndex = 0;
        _cmbOfferType.SelectedIndexChanged += (_, _) => UpdateOfferValueState();
        Controls.Add(_cmbOfferType);

        _txtOfferValue = new TextBox
        {
            Left = 300,
            Top = y,
            Width = 130,
            Height = 30,
            Font = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.FixedSingle,
            Text = "10"
        };
        Controls.Add(_txtOfferValue);
        y += 46;

        // ---- Target Segment ----
        AddLabel("Target Segment", y); y += 22;
        _cmbTargetSegment = new ComboBox
        {
            Left = 24,
            Top = y,
            Width = 260,
            Height = 30,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbTargetSegment.Items.AddRange(new object[]
        {
            "Any", "Champion", "Loyal", "Promising", "Detractor",
            "At Risk", "Dormant", "Lost", "Active"
        });
        _cmbTargetSegment.SelectedIndex = 0;
        Controls.Add(_cmbTargetSegment);
        y += 46;

        // ---- Valid From ----
        AddLabel("Valid From", y);
        AddLabelAt("Valid Until", 300, y);
        y += 22;

        _chkNoFrom = new CheckBox
        {
            Left = 24,
            Top = y + 4,
            Width = 50,
            Height = 24,
            Text = "None",
            Font = new Font("Segoe UI", 8.5f)
        };
        _chkNoFrom.CheckedChanged += (_, _) => _dtFrom.Enabled = !_chkNoFrom.Checked;
        Controls.Add(_chkNoFrom);

        _dtFrom = new DateTimePicker
        {
            Left = 80,
            Top = y,
            Width = 200,
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5f),
            Value = DateTime.Today
        };
        Controls.Add(_dtFrom);

        _chkNoUntil = new CheckBox
        {
            Left = 300,
            Top = y + 4,
            Width = 50,
            Height = 24,
            Text = "None",
            Font = new Font("Segoe UI", 8.5f)
        };
        _chkNoUntil.CheckedChanged += (_, _) => _dtUntil.Enabled = !_chkNoUntil.Checked;
        Controls.Add(_chkNoUntil);

        _dtUntil = new DateTimePicker
        {
            Left = 356,
            Top = y,
            Width = 200,
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5f),
            Value = DateTime.Today.AddMonths(3)
        };
        Controls.Add(_dtUntil);
        y += 46;

        // ---- Max Uses ----
        AddLabel("Max Uses", y); y += 22;

        _chkUnlimited = new CheckBox
        {
            Left = 24,
            Top = y + 4,
            Width = 100,
            Height = 24,
            Text = "Unlimited",
            Checked = true,
            Font = new Font("Segoe UI", 9f)
        };
        _chkUnlimited.CheckedChanged += (_, _) =>
        {
            _numMaxUses.Enabled = !_chkUnlimited.Checked;
        };
        Controls.Add(_chkUnlimited);

        _numMaxUses = new NumericUpDown
        {
            Left = 140,
            Top = y,
            Width = 130,
            Height = 30,
            Font = new Font("Segoe UI", 9.5f),
            Minimum = 1,
            Maximum = 100000,
            Value = 100,
            Enabled = false
        };
        Controls.Add(_numMaxUses);
        y += 46;

        // ---- Active ----
        _chkActive = new CheckBox
        {
            Left = 24,
            Top = y,
            Width = 300,
            Height = 24,
            Text = "Active (available for use)",
            Checked = true,
            Font = new Font("Segoe UI", 9.5f)
        };
        Controls.Add(_chkActive);
        y += 34;

        // ---- Notes ----
        AddLabel("Notes (internal)", y); y += 22;
        _txtNotes = new TextBox
        {
            Left = 24,
            Top = y,
            Width = 560,
            Height = 60,
            Multiline = true,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(_txtNotes);
        y += 72;

        // ---- Status ----
        _lblStatus = new Label
        {
            Left = 24,
            Top = y,
            Width = 560,
            Height = 20,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(110, 118, 132),
            Text = ""
        };
        Controls.Add(_lblStatus);
        y += 26;

        // ---- Buttons ----
        _btnSave = new Button
        {
            Text = _editPromotionId > 0 ? "Save Changes" : "Create Promotion",
            Left = 414,
            Top = y,
            Width = 170,
            Height = 40,
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
            Left = 314,
            Top = y,
            Width = 90,
            Height = 40,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        Controls.Add(btnCancel);

        ClientSize = new Size(608, y + 60);

        // Load existing if editing
        if (_existing.HasValue)
            LoadExisting(_existing.Value);
    }

    private void AddLabel(string text, int y)
    {
        Controls.Add(new Label
        {
            Text = text,
            Left = 24,
            Top = y,
            Width = 260,
            Height = 20,
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92)
        });
    }

    private void AddLabelAt(string text, int x, int y)
    {
        Controls.Add(new Label
        {
            Text = text,
            Left = x,
            Top = y,
            Width = 260,
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
            Width = 560,
            Height = 30,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(t);
        return t;
    }

    private void UpdateOfferValueState()
    {
        var type = _cmbOfferType.SelectedItem?.ToString() ?? "";
        _txtOfferValue.Enabled = type == "% Percentage" || type == "₱ Fixed Amount";

        if (type == "Free Service")
            _txtOfferValue.Text = "0";
    }

    private void LoadExisting(JsonElement promo)
    {
        _txtName.Text = GetStr(promo, "name");
        _txtCode.Text = GetStr(promo, "code");
        _txtDescription.Text = GetStr(promo, "description");

        var offerType = GetStr(promo, "offerType");
        _cmbOfferType.SelectedIndex = offerType switch
        {
            "Percentage" => 0,
            "FixedAmount" => 1,
            "FreeService" => 2,
            "Custom" => 3,
            _ => 0
        };
        _txtOfferValue.Text = GetDecimal(promo, "offerValue")?.ToString("F0") ?? "10";

        var seg = GetStr(promo, "targetSegment");
        var segIdx = _cmbTargetSegment.Items.IndexOf(seg);
        _cmbTargetSegment.SelectedIndex = segIdx >= 0 ? segIdx : 0;

        var validFrom = GetDate(promo, "validFrom");
        if (validFrom.HasValue)
        {
            _dtFrom.Value = validFrom.Value;
            _chkNoFrom.Checked = false;
        }
        else
        {
            _chkNoFrom.Checked = true;
            _dtFrom.Enabled = false;
        }

        var validUntil = GetDate(promo, "validUntil");
        if (validUntil.HasValue)
        {
            _dtUntil.Value = validUntil.Value;
            _chkNoUntil.Checked = false;
        }
        else
        {
            _chkNoUntil.Checked = true;
            _dtUntil.Enabled = false;
        }

        var maxUses = GetInt(promo, "maxUses");
        if (maxUses.HasValue)
        {
            _chkUnlimited.Checked = false;
            _numMaxUses.Value = maxUses.Value;
        }
        else
        {
            _chkUnlimited.Checked = true;
        }

        _chkActive.Checked = GetBool(promo, "isActive");
        _txtNotes.Text = GetStr(promo, "notes");
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            _lblStatus.Text = "✗ Name is required.";
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
            return;
        }

        _btnSave.Enabled = false;
        _btnSave.Text = "Saving...";
        _lblStatus.Text = "Sending request...";
        _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);

        try
        {
            var offerTypeLabel = _cmbOfferType.SelectedItem?.ToString() ?? "% Percentage";
            var offerType = offerTypeLabel switch
            {
                "% Percentage" => "Percentage",
                "₱ Fixed Amount" => "FixedAmount",
                "Free Service" => "FreeService",
                _ => "Custom"
            };

            decimal? offerValue = null;
            if (offerType == "Percentage" || offerType == "FixedAmount")
            {
                if (!decimal.TryParse(_txtOfferValue.Text, out var v) || v <= 0)
                {
                    _lblStatus.Text = "✗ Offer value must be a positive number.";
                    _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
                    _btnSave.Enabled = true;
                    _btnSave.Text = _editPromotionId > 0 ? "Save Changes" : "Create Promotion";
                    return;
                }
                offerValue = v;
            }

            var payload = new
            {
                name = _txtName.Text.Trim(),
                code = _txtCode.Text.Trim(),
                description = _txtDescription.Text.Trim(),
                offerType = offerType,
                offerValue = offerValue,
                targetSegment = _cmbTargetSegment.SelectedItem?.ToString() ?? "Any",
                validFrom = _chkNoFrom.Checked ? (DateTime?)null : _dtFrom.Value,
                validUntil = _chkNoUntil.Checked ? (DateTime?)null : _dtUntil.Value,
                isActive = _chkActive.Checked,
                maxUses = _chkUnlimited.Checked ? (int?)null : (int)_numMaxUses.Value,
                notes = _txtNotes.Text.Trim()
            };

            var url = _editPromotionId > 0
                ? $"{_apiUrl}/tenant/{Session.CompanyId}/promotions/{_editPromotionId}"
                : $"{_apiUrl}/tenant/{Session.CompanyId}/promotions";

            var method = _editPromotionId > 0 ? HttpMethod.Put : HttpMethod.Post;

            using var req = new HttpRequestMessage(method, url);
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

            _lblStatus.Text = _editPromotionId > 0
                ? "✓ Promotion updated."
                : "✓ Promotion created.";
            _lblStatus.ForeColor = Color.FromArgb(34, 140, 78);

            Saved = true;
            await Task.Delay(500);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "✗ " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
            _btnSave.Enabled = true;
            _btnSave.Text = _editPromotionId > 0 ? "Save Changes" : "Create Promotion";
        }
    }

    // ---- JSON helpers ----
    private static string GetStr(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return "";
        return p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : p.ToString();
    }

    private static int? GetInt(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return null;
        if (p.ValueKind == JsonValueKind.Null) return null;
        return p.ValueKind == JsonValueKind.Number ? (int)p.GetDouble() : (int?)null;
    }

    private static decimal? GetDecimal(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return null;
        if (p.ValueKind == JsonValueKind.Null) return null;
        return p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : (decimal?)null;
    }

    private static bool GetBool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return false;
        return p.ValueKind == JsonValueKind.True;
    }

    private static DateTime? GetDate(JsonElement el, string name)
    {
        var s = GetStr(el, name);
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s, out var d) ? d : null;
    }
}