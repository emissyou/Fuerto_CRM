namespace CRM_DesignServices.winforms;

public class ResolveIssueDialog : Form
{
    public string Status { get; private set; } = "Resolved";
    public string ResolutionNotes { get; private set; } = "";

    private ComboBox _cmbStatus = null!;
    private TextBox _txtNotes = null!;

    public ResolveIssueDialog(string issueTitle)
    {
        Text = "Resolve Issue";
        Size = new Size(520, 340);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        Controls.Add(new Label
        {
            Text = "Resolve / Close Issue",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, 20)
        });

        Controls.Add(new Label
        {
            Text = issueTitle,
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(24, 50)
        });

        Controls.Add(new Label
        {
            Text = "New Status",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, 90)
        });

        _cmbStatus = new ComboBox
        {
            Left = 24,
            Top = 112,
            Width = 456,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbStatus.Items.AddRange(new[] { "Resolved", "Closed", "Rejected" });
        _cmbStatus.SelectedIndex = 0;
        Controls.Add(_cmbStatus);

        Controls.Add(new Label
        {
            Text = "Resolution Notes",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, 148)
        });

        _txtNotes = new TextBox
        {
            Left = 24,
            Top = 170,
            Width = 456,
            Height = 70,
            Multiline = true,
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "How was the issue resolved?"
        };
        Controls.Add(_txtNotes);

        var btnOk = new Button
        {
            Text = "Save",
            Left = 300,
            Top = 256,
            Width = 90,
            Height = 34,
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
            Left = 398,
            Top = 256,
            Width = 82,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f),
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        Controls.Add(btnCancel);

        btnOk.Click += (_, _) =>
        {
            Status = _cmbStatus.SelectedItem?.ToString() ?? "Resolved";
            ResolutionNotes = _txtNotes.Text.Trim();
        };
    }

    public object ToPayload() => new
    {
        status = Status,
        resolutionNotes = ResolutionNotes
    };
}