namespace CRM_DesignServices.winforms;

public class UpdateProgressDialog : Form
{
    public int ProgressPercentage { get; private set; }
    public string DesignNotes { get; private set; } = "";

    private TrackBar _slider = null!;
    private Label _lblValue = null!;
    private TextBox _txtNotes = null!;

    public UpdateProgressDialog(int currentProgress, string currentNotes)
    {
        Text = "Update Design Progress";
        Size = new Size(520, 380);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        Controls.Add(new Label
        {
            Text = "Update Design Progress",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, 20)
        });

        Controls.Add(new Label
        {
            Text = $"Current progress: {currentProgress}%",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(24, 50)
        });

        Controls.Add(new Label
        {
            Text = "New Progress",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, 90)
        });

        _slider = new TrackBar
        {
            Left = 24,
            Top = 110,
            Width = 400,
            Minimum = 0,
            Maximum = 100,
            TickFrequency = 10,
            Value = Math.Max(currentProgress, 0)
        };
        _slider.ValueChanged += (_, _) => _lblValue.Text = $"{_slider.Value}%";
        Controls.Add(_slider);

        _lblValue = new Label
        {
            Left = 430,
            Top = 118,
            Width = 60,
            Height = 30,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 168, 0),
            Text = $"{_slider.Value}%",
            TextAlign = ContentAlignment.MiddleLeft
        };
        Controls.Add(_lblValue);

        Controls.Add(new Label
        {
            Text = "Design Notes",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, 170)
        });

        _txtNotes = new TextBox
        {
            Left = 24,
            Top = 192,
            Width = 456,
            Height = 80,
            Multiline = true,
            Text = currentNotes ?? "",
            PlaceholderText = "Describe what was updated",
            Font = new Font("Segoe UI", 9.5f)
        };
        Controls.Add(_txtNotes);

        var btnOk = new Button
        {
            Text = "Save",
            Left = 300,
            Top = 292,
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
            Top = 292,
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
            ProgressPercentage = _slider.Value;
            DesignNotes = _txtNotes.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        };
    }
}