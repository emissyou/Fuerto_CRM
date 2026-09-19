namespace CRM_DesignServices.winforms;

public class NewIssueDialog : Form
{
    public string IssueType { get; private set; } = "Complaint";
    public string Severity { get; private set; } = "Medium";
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string RequestedAction { get; private set; } = "";
    public decimal? DisputedAmount { get; private set; }
    public string? PaymentReference { get; private set; }
    public DateTime? TargetResolutionDate { get; private set; }

    private ComboBox _cmbType = null!;
    private ComboBox _cmbSeverity = null!;
    private TextBox _txtTitle = null!;
    private TextBox _txtDescription = null!;
    private TextBox _txtRequestedAction = null!;
    private TextBox _txtDisputedAmount = null!;
    private TextBox _txtPaymentRef = null!;
    private DateTimePicker _dtTarget = null!;
    private CheckBox _chkHasTarget = null!;
    private Panel _paymentPanel = null!;

    public NewIssueDialog(string projectName)
    {
        Text = "Report Issue / Adjustment";
        Size = new Size(600, 700);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        var y = 20;

        Controls.Add(new Label
        {
            Text = $"Report for: {projectName}",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, y)
        });
        y += 40;

        Controls.Add(new Label
        {
            Text = "Issue Type",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, y)
        });
        y += 22;
        _cmbType = new ComboBox
        {
            Left = 24,
            Top = y,
            Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbType.Items.AddRange(new[]
        {
            "Complaint", "Adjustment", "PaymentDispute", "Rework", "Other"
        });
        _cmbType.SelectedIndex = 0;
        Controls.Add(_cmbType);

        Controls.Add(new Label
        {
            Text = "Severity",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(300, y - 22)
        });
        _cmbSeverity = new ComboBox
        {
            Left = 300,
            Top = y,
            Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbSeverity.Items.AddRange(new[] { "Low", "Medium", "High", "Critical" });
        _cmbSeverity.SelectedIndex = 1;
        Controls.Add(_cmbSeverity);
        y += 46;

        Controls.Add(new Label
        {
            Text = "Title *",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, y)
        });
        y += 22;
        _txtTitle = new TextBox
        {
            Left = 24,
            Top = y,
            Width = 536,
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "Short summary of the issue"
        };
        Controls.Add(_txtTitle);
        y += 42;

        Controls.Add(new Label
        {
            Text = "Description",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, y)
        });
        y += 22;
        _txtDescription = new TextBox
        {
            Left = 24,
            Top = y,
            Width = 536,
            Height = 70,
            Multiline = true,
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "What happened? What's wrong?"
        };
        Controls.Add(_txtDescription);
        y += 82;

        Controls.Add(new Label
        {
            Text = "Requested Action",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, y)
        });
        y += 22;
        _txtRequestedAction = new TextBox
        {
            Left = 24,
            Top = y,
            Width = 536,
            Height = 60,
            Multiline = true,
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "What should we do? (Optional)"
        };
        Controls.Add(_txtRequestedAction);
        y += 74;

        // ---- Payment dispute panel (only shows for PaymentDispute type) ----
        _paymentPanel = new Panel
        {
            Left = 0,
            Top = y,
            Width = 600,
            Height = 100,
            BackColor = Color.FromArgb(252, 250, 245),
            Visible = false
        };
        Controls.Add(_paymentPanel);

        _paymentPanel.Controls.Add(new Label
        {
            Text = "Disputed Amount (₱)",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, 12)
        });
        _txtDisputedAmount = new TextBox
        {
            Left = 24,
            Top = 34,
            Width = 260,
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "0.00"
        };
        _paymentPanel.Controls.Add(_txtDisputedAmount);

        _paymentPanel.Controls.Add(new Label
        {
            Text = "Payment Reference",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(300, 12)
        });
        _txtPaymentRef = new TextBox
        {
            Left = 300,
            Top = 34,
            Width = 260,
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "OR#, TXN ID"
        };
        _paymentPanel.Controls.Add(_txtPaymentRef);
        y += 110;

        _cmbType.SelectedIndexChanged += (_, _) =>
        {
            _paymentPanel.Visible = _cmbType.SelectedItem?.ToString() == "PaymentDispute";
        };

        _chkHasTarget = new CheckBox
        {
            Left = 24,
            Top = y,
            Width = 260,
            Text = "Set target resolution date",
            Font = new Font("Segoe UI", 9.25f)
        };
        Controls.Add(_chkHasTarget);

        _dtTarget = new DateTimePicker
        {
            Left = 300,
            Top = y,
            Width = 260,
            Format = DateTimePickerFormat.Short,
            Enabled = false,
            Value = DateTime.Today.AddDays(7)
        };
        Controls.Add(_dtTarget);
        _chkHasTarget.CheckedChanged += (_, _) => _dtTarget.Enabled = _chkHasTarget.Checked;
        y += 46;

        var btnOk = new Button
        {
            Text = "Submit",
            Left = 420,
            Top = y,
            Width = 110,
            Height = 36,
            BackColor = Color.FromArgb(255, 168, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            DialogResult = DialogResult.OK
        };
        btnOk.FlatAppearance.BorderSize = 0;
        Controls.Add(btnOk);

        var btnCancel = new Button
        {
            Text = "Cancel",
            Left = 538,
            Top = y,
            Width = 42,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        Controls.Add(btnCancel);

        ClientSize = new Size(600, y + 60);

        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtTitle.Text))
            {
                MessageBox.Show("Title is required.", "Report Issue");
                DialogResult = DialogResult.None;
                return;
            }

            IssueType = _cmbType.SelectedItem?.ToString() ?? "Complaint";
            Severity = _cmbSeverity.SelectedItem?.ToString() ?? "Medium";
            Title = _txtTitle.Text.Trim();
            Description = _txtDescription.Text.Trim();
            RequestedAction = _txtRequestedAction.Text.Trim();

            if (IssueType == "PaymentDispute")
            {
                if (decimal.TryParse(_txtDisputedAmount.Text, out var amt))
                    DisputedAmount = amt;
                PaymentReference = string.IsNullOrWhiteSpace(_txtPaymentRef.Text)
                    ? null : _txtPaymentRef.Text.Trim();
            }

            if (_chkHasTarget.Checked)
                TargetResolutionDate = _dtTarget.Value;
        };
    }

    public object ToPayload(int projectId) => new
    {
        projectId,
        issueType = IssueType,
        severity = Severity,
        title = Title,
        description = Description,
        requestedAction = RequestedAction,
        disputedAmount = DisputedAmount,
        paymentReference = PaymentReference,
        targetResolutionDate = TargetResolutionDate
    };
}