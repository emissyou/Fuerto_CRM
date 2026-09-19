using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class ConvertLeadDialog : Form
{
    public string CustomerType { get; private set; } = "Regular";
    public string ProjectType { get; private set; } = "";
    public string Location { get; private set; } = "";
    public string Description { get; private set; } = "";

    private ComboBox _cmbType = null!;
    private TextBox _txtProjectType = null!;
    private TextBox _txtLocation = null!;
    private TextBox _txtDescription = null!;

    public ConvertLeadDialog(string leadName)
    {
        Text = "Convert Lead to Customer & Project";
        Size = new Size(520, 460);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        var lblHeader = new Label
        {
            Text = $"Converting: {leadName}",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, 20)
        };
        Controls.Add(lblHeader);

        var lblInfo = new Label
        {
            Text = "This will create a Customer record AND a new Project.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(24, 50)
        };
        Controls.Add(lblInfo);

        // Customer Type
        AddLabel("Customer Type", 90);
        _cmbType = new ComboBox
        {
            Left = 24,
            Top = 112,
            Width = 456,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbType.Items.AddRange(new[] { "Regular", "VIP", "Corporate", "Walk-in" });
        _cmbType.SelectedIndex = 0;
        Controls.Add(_cmbType);

        // Project Type
        AddLabel("Project Type", 156);
        _txtProjectType = new TextBox
        {
            Left = 24,
            Top = 178,
            Width = 456,
            PlaceholderText = "e.g. Interior Design, Renovation",
            Font = new Font("Segoe UI", 9.5f)
        };
        Controls.Add(_txtProjectType);

        // Location
        AddLabel("Location", 222);
        _txtLocation = new TextBox
        {
            Left = 24,
            Top = 244,
            Width = 456,
            PlaceholderText = "e.g. Makati, BGC",
            Font = new Font("Segoe UI", 9.5f)
        };
        Controls.Add(_txtLocation);

        // Description
        AddLabel("Description", 288);
        _txtDescription = new TextBox
        {
            Left = 24,
            Top = 310,
            Width = 456,
            Height = 70,
            Multiline = true,
            PlaceholderText = "Brief scope or notes",
            Font = new Font("Segoe UI", 9.5f)
        };
        Controls.Add(_txtDescription);

        // Buttons
        var btnOk = new Button
        {
            Text = "Convert",
            Left = 300,
            Top = 392,
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
            Top = 392,
            Width = 82,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f),
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        Controls.Add(btnCancel);

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        btnOk.Click += (_, _) =>
        {
            CustomerType = _cmbType.SelectedItem?.ToString() ?? "Regular";
            ProjectType = _txtProjectType.Text.Trim();
            Location = _txtLocation.Text.Trim();
            Description = _txtDescription.Text.Trim();
        };
    }

    private void AddLabel(string text, int top)
    {
        Controls.Add(new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92),
            AutoSize = true,
            Location = new Point(24, top)
        });
    }

    public object ToPayload() => new
    {
        customerType = CustomerType,
        projectType = ProjectType,
        location = Location,
        description = Description
    };
}