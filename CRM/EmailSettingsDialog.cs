using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CRM_DesignServices.winforms;

/// <summary>
/// Polished modal dialog allowing users and admins to configure and test Gmail SMTP credentials.
/// Includes inline instructions, password toggle, live test verification, and immediate persistence.
/// </summary>
public class EmailSettingsDialog : Form
{
    private readonly TextBox _txtEmail;
    private readonly TextBox _txtPassword;
    private readonly TextBox _txtDisplayName;
    private readonly TextBox _txtHost;
    private readonly NumericUpDown _numPort;
    private readonly CheckBox _chkSsl;
    private readonly Button _btnTest;
    private readonly Button _btnSave;
    private readonly Button _btnCancel;
    private readonly Label _lblStatus;

    public EmailSettingsDialog()
    {
        Text            = "Email Configuration (Gmail SMTP)";
        Size            = new Size(580, 680);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ShowInTaskbar   = false;
        BackColor       = Color.White;
        Font            = new Font("Segoe UI", 9.5f);

        var current = EmailSettings.Load();

        // =========================================================
        // HEADER BANNER
        // =========================================================
        var pnlHeader = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 76,
            BackColor = Color.White,
            Padding   = new Padding(24, 16, 24, 14)
        };
        pnlHeader.Paint += (_, pe) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
            pe.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };
        Controls.Add(pnlHeader);

        var lblTitle = new Label
        {
            Text      = "Email & SMTP Settings",
            Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location  = new Point(24, 14),
            AutoSize  = true
        };
        var lblSub = new Label
        {
            Text      = "Configure your Gmail sender account to dispatch real emails directly to customers",
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location  = new Point(24, 40),
            AutoSize  = true
        };
        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

        // =========================================================
        // FOOTER BAR
        // =========================================================
        var pnlFooter = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 64,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding   = new Padding(24, 12, 24, 12)
        };
        pnlFooter.Paint += (_, pe) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
            pe.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
        };
        Controls.Add(pnlFooter);

        _btnSave = new Button
        {
            Text      = "Save Configuration",
            Width     = 160,
            Height    = 38,
            Anchor    = AnchorStyles.Bottom | AnchorStyles.Right,
            Location  = new Point(pnlFooter.Width - 184, 13),
            BackColor = Color.FromArgb(2, 132, 199),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor    = Cursors.Hand
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += (_, _) => SaveSettings();

        _btnCancel = new Button
        {
            Text      = "Cancel",
            Width     = 90,
            Height    = 38,
            Anchor    = AnchorStyles.Bottom | AnchorStyles.Right,
            Location  = new Point(pnlFooter.Width - 284, 13),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(71, 85, 105),
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand
        };
        _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        _btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        pnlFooter.Controls.AddRange(new Control[] { _btnSave, _btnCancel });

        // =========================================================
        // BODY CONTENT
        // =========================================================
        var pnlBody = new Panel
        {
            Dock      = DockStyle.Fill,
            AutoScroll= true,
            Padding   = new Padding(24, 16, 24, 16)
        };
        Controls.Add(pnlBody);
        pnlBody.BringToFront();

        int y = 14;

        // INSTRUCTION BANNER
        var banner = new Panel
        {
            Left      = 24,
            Top       = y,
            Width     = 512,
            Height    = 88,
            BackColor = Color.FromArgb(240, 249, 255)
        };
        banner.Paint += (_, pe) =>
        {
            using var borderPen = new Pen(Color.FromArgb(186, 230, 253), 1);
            pe.Graphics.DrawRectangle(borderPen, 0, 0, banner.Width - 1, banner.Height - 1);
        };
        var lblBanner = new Label
        {
            Left      = 14,
            Top       = 10,
            Width     = 484,
            Height    = 68,
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(12, 74, 110),
            Text      = "ℹ Google Gmail Requirement:\n" +
                        "Google requires a 16-character App Password (not your regular Gmail password).\n" +
                        "1. Go to: myaccount.google.com -> Security\n" +
                        "2. Enable '2-Step Verification', then click 'App Passwords'\n" +
                        "3. Create a new App Password (name it 'Fuerto CRM') and copy the 16 letters below."
        };
        banner.Controls.Add(lblBanner);
        pnlBody.Controls.Add(banner);
        y += 100;

        // 1. GMAIL ADDRESS
        AddLabel(pnlBody, "Sender Gmail Address *", 24, y);
        y += 22;
        _txtEmail = AddTextBox(pnlBody, current.SenderEmail, "yourname@gmail.com", 24, y, 512);
        y += 46;

        // 2. GOOGLE APP PASSWORD
        AddLabel(pnlBody, "Google App Password (16 characters) *", 24, y);
        y += 22;
        _txtPassword = AddTextBox(pnlBody, current.SenderPassword, "xxxx xxxx xxxx xxxx", 24, y, 420, true);
        
        var btnTogglePwd = new Button
        {
            Text      = "👁 Show",
            Left      = 452,
            Top       = y,
            Width     = 84,
            Height    = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(71, 85, 105),
            Cursor    = Cursors.Hand
        };
        btnTogglePwd.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        btnTogglePwd.Click += (_, _) =>
        {
            _txtPassword.UseSystemPasswordChar = !_txtPassword.UseSystemPasswordChar;
            btnTogglePwd.Text = _txtPassword.UseSystemPasswordChar ? "👁 Show" : "🙈 Hide";
        };
        pnlBody.Controls.Add(btnTogglePwd);
        y += 46;

        // 3. SENDER DISPLAY NAME
        AddLabel(pnlBody, "Sender Display Name (Shown to Customers)", 24, y);
        y += 22;
        _txtDisplayName = AddTextBox(pnlBody, current.SenderDisplayName, "Company or Designer Name", 24, y, 512);
        y += 46;

        // 4. SMTP HOST & PORT (ADVANCED)
        AddLabel(pnlBody, "SMTP Host", 24, y);
        AddLabel(pnlBody, "Port", 380, y);
        y += 22;
        _txtHost = AddTextBox(pnlBody, current.SmtpHost, "smtp.gmail.com", 24, y, 344);
        
        _numPort = new NumericUpDown
        {
            Left      = 380,
            Top       = y,
            Width     = 90,
            Height    = 34,
            Minimum   = 1,
            Maximum   = 65535,
            Value     = current.SmtpPort
        };
        pnlBody.Controls.Add(_numPort);

        _chkSsl = new CheckBox
        {
            Text      = "SSL/TLS",
            Left      = 478,
            Top       = y + 4,
            Width     = 70,
            Checked   = current.EnableSsl,
            ForeColor = Color.FromArgb(71, 85, 105)
        };
        pnlBody.Controls.Add(_chkSsl);
        y += 54;

        // 5. TEST CONNECTION BUTTON
        _btnTest = new Button
        {
            Text      = "⚡ Send Test Email to Verify",
            Left      = 24,
            Top       = y,
            Width     = 240,
            Height    = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(30, 41, 59),
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor    = Cursors.Hand
        };
        _btnTest.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        _btnTest.Click += async (_, _) => await RunTestConnectionAsync();
        pnlBody.Controls.Add(_btnTest);

        _lblStatus = new Label
        {
            Left      = 24,
            Top       = y + 42,
            Width     = 512,
            Height    = 44,
            ForeColor = Color.FromArgb(100, 116, 139),
            Text      = current.IsConfigured 
                ? "✔ Credentials currently configured and ready." 
                : "Enter your Gmail address and 16-character App Password, then click 'Send Test Email'."
        };
        pnlBody.Controls.Add(_lblStatus);
    }

    private async Task RunTestConnectionAsync()
    {
        string email = _txtEmail.Text.Trim();
        string pwd   = _txtPassword.Text.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pwd))
        {
            MessageBox.Show("Please enter your Gmail address and App Password first.", "Test Email", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnTest.Enabled = false;
        _btnTest.Text = "⏳ Testing Connection...";
        _lblStatus.ForeColor = Color.FromArgb(2, 132, 199);
        _lblStatus.Text = $"Connecting to {_txtHost.Text}:{_numPort.Value} and sending test message to {email}...";
        Cursor = Cursors.WaitCursor;

        var testSettings = new EmailSettings
        {
            SenderEmail       = email,
            SenderPassword    = pwd,
            SenderDisplayName = _txtDisplayName.Text.Trim(),
            SmtpHost          = _txtHost.Text.Trim(),
            SmtpPort          = (int)_numPort.Value,
            EnableSsl         = _chkSsl.Checked
        };

        var (success, msg) = await EmailService.SendTestEmailAsync(testSettings, email);

        Cursor = Cursors.Default;
        _btnTest.Enabled = true;
        _btnTest.Text = "⚡ Send Test Email to Verify";

        if (success)
        {
            _lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
            _lblStatus.Text = $"✔ Success! Test email was delivered to {email}. Check your Gmail inbox!";
            MessageBox.Show($"Connection Successful!\n\nA real test email was delivered to {email}.\nCheck your Gmail inbox (or Spam folder).", "Test Passed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
            _lblStatus.Text = $"✖ Delivery failed: {msg}";
            MessageBox.Show($"Email connection test failed:\n\n{msg}", "Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveSettings()
    {
        string email = _txtEmail.Text.Trim();
        string pwd   = _txtPassword.Text.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pwd))
        {
            MessageBox.Show("Please provide both your Gmail address and 16-character App Password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var settings = EmailSettings.Load();
        settings.SenderEmail       = email;
        settings.SenderPassword    = pwd;
        settings.SenderDisplayName = string.IsNullOrWhiteSpace(_txtDisplayName.Text) ? "Fuerto CRM" : _txtDisplayName.Text.Trim();
        settings.SmtpHost          = string.IsNullOrWhiteSpace(_txtHost.Text) ? "smtp.gmail.com" : _txtHost.Text.Trim();
        settings.SmtpPort          = (int)_numPort.Value;
        settings.EnableSsl         = _chkSsl.Checked;

        settings.Save();

        MessageBox.Show("Email configuration saved successfully! You can now dispatch real emails to customers.", "Configuration Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        DialogResult = DialogResult.OK;
        Close();
    }

    private static void AddLabel(Panel parent, string text, int x, int y)
    {
        parent.Controls.Add(new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85),
            Location  = new Point(x, y),
            AutoSize  = true
        });
    }

    private static TextBox AddTextBox(Panel parent, string value, string placeholder, int x, int y, int width, bool isPassword = false)
    {
        var txt = new TextBox
        {
            Text      = value,
            Left      = x,
            Top       = y,
            Width     = width,
            Height    = 34,
            UseSystemPasswordChar = isPassword,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(248, 250, 252)
        };
        parent.Controls.Add(txt);
        return txt;
    }
}
