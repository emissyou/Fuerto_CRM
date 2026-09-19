using System.Text;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public partial class LoginForm : Form
{
    private TextBox txtEmail = null!;
    private TextBox txtPassword = null!;
    private Button btnLogin = null!;
    private Label lblStatus = null!;

    // =========================================================
    // CRM API URL
    // =========================================================

    private const string ApiUrl = "http://localhost:5068";

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public LoginForm()
    {
        BuildLoginInterface();
    }

    // =========================================================
    // BUILD LOGIN INTERFACE
    // =========================================================

    private void BuildLoginInterface()
    {
        Text = "Fuerto CRM - Login";

        StartPosition = FormStartPosition.CenterScreen;

        ClientSize = new Size(900, 550);

        MinimumSize = new Size(900, 550);

        BackColor = Color.FromArgb(245, 246, 248);

        FormBorderStyle = FormBorderStyle.FixedSingle;

        MaximizeBox = false;

        Controls.Clear();

        // =====================================================
        // LEFT BRAND PANEL
        // =====================================================

        var brandPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 420,
            BackColor = Color.FromArgb(28, 32, 38)
        };

        Controls.Add(brandPanel);

        // =====================================================
        // FUERTO LOGO
        // =====================================================

        var logo = new Label
        {
            Text = "FUERTO",
            Font = new Font(
                "Segoe UI",
                32,
                FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 168, 0),
            AutoSize = true,
            Location = new Point(55, 150)
        };

        brandPanel.Controls.Add(logo);

        // =====================================================
        // COMPANY SUBTITLE
        // =====================================================

        var subtitle = new Label
        {
            Text = "INTERIOR DESIGN SERVICES",
            Font = new Font(
                "Segoe UI",
                10,
                FontStyle.Regular),
            ForeColor = Color.FromArgb(180, 185, 192),
            AutoSize = true,
            Location = new Point(59, 205)
        };

        brandPanel.Controls.Add(subtitle);

        // =====================================================
        // DESCRIPTION
        // =====================================================

        var description = new Label
        {
            Text = "Customer and Project\nRelationship Management",
            Font = new Font(
                "Segoe UI",
                12,
                FontStyle.Regular),
            ForeColor = Color.FromArgb(220, 223, 228),
            AutoSize = true,
            Location = new Point(59, 260)
        };

        brandPanel.Controls.Add(description);

        // =====================================================
        // LOGIN PANEL
        // =====================================================

        var loginPanel = new Panel
        {
            Left = 420,
            Top = 0,
            Width = 480,
            Height = 550,
            BackColor = Color.White
        };

        Controls.Add(loginPanel);

        // =====================================================
        // WELCOME TITLE
        // =====================================================

        var title = new Label
        {
            Text = "Welcome Back",
            Font = new Font(
                "Segoe UI",
                24,
                FontStyle.Bold),
            ForeColor = Color.FromArgb(35, 38, 43),
            AutoSize = true,
            Location = new Point(65, 100)
        };

        loginPanel.Controls.Add(title);

        // =====================================================
        // INSTRUCTION
        // =====================================================

        var instruction = new Label
        {
            Text = "Sign in to continue to Fuerto CRM",
            Font = new Font(
                "Segoe UI",
                10,
                FontStyle.Regular),
            ForeColor = Color.FromArgb(125, 130, 138),
            AutoSize = true,
            Location = new Point(68, 140)
        };

        loginPanel.Controls.Add(instruction);

        // =====================================================
        // EMAIL LABEL
        // =====================================================

        var emailLabel = new Label
        {
            Text = "Email",
            Font = new Font(
                "Segoe UI",
                9,
                FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 75, 82),
            AutoSize = true,
            Location = new Point(68, 190)
        };

        loginPanel.Controls.Add(emailLabel);

        // =====================================================
        // EMAIL TEXTBOX
        // =====================================================

        txtEmail = new TextBox
        {
            Width = 340,
            Height = 35,
            Location = new Point(65, 215),
            Font = new Font(
                "Segoe UI",
                11,
                FontStyle.Regular)
        };

        loginPanel.Controls.Add(txtEmail);

        // =====================================================
        // PASSWORD LABEL
        // =====================================================

        var passwordLabel = new Label
        {
            Text = "Password",
            Font = new Font(
                "Segoe UI",
                9,
                FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 75, 82),
            AutoSize = true,
            Location = new Point(68, 270)
        };

        loginPanel.Controls.Add(passwordLabel);

        // =====================================================
        // PASSWORD TEXTBOX
        // =====================================================

        txtPassword = new TextBox
        {
            Width = 340,
            Height = 35,
            Location = new Point(65, 295),
            Font = new Font(
                "Segoe UI",
                11,
                FontStyle.Regular),
            UseSystemPasswordChar = true
        };

        loginPanel.Controls.Add(txtPassword);

        // =====================================================
        // LOGIN BUTTON
        // =====================================================

        btnLogin = new Button
        {
            Text = "SIGN IN",
            Width = 340,
            Height = 45,
            Location = new Point(65, 355),
            BackColor = Color.FromArgb(255, 168, 0),
            ForeColor = Color.White,
            Font = new Font(
                "Segoe UI",
                10,
                FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        btnLogin.FlatAppearance.BorderSize = 0;

        btnLogin.Click += BtnLogin_Click;

        loginPanel.Controls.Add(btnLogin);

        // =====================================================
        // STATUS LABEL
        // =====================================================

        lblStatus = new Label
        {
            Text = "",
            Font = new Font(
                "Segoe UI",
                9,
                FontStyle.Regular),
            ForeColor = Color.Firebrick,
            AutoSize = false,
            Width = 340,
            Height = 45,
            Location = new Point(65, 415),
            TextAlign = ContentAlignment.MiddleCenter
        };

        loginPanel.Controls.Add(lblStatus);

        // =====================================================
        // ENTER KEY = SIGN IN
        // =====================================================

        AcceptButton = btnLogin;
    }

    // =========================================================
    // LOGIN BUTTON
    // =========================================================

    private async void BtnLogin_Click(
        object? sender,
        EventArgs e)
    {
        var email = txtEmail.Text.Trim();

        var password = txtPassword.Text;

        // =====================================================
        // VALIDATE EMAIL
        // =====================================================

        if (string.IsNullOrWhiteSpace(email))
        {
            lblStatus.ForeColor = Color.Firebrick;

            lblStatus.Text = "Please enter your email.";

            txtEmail.Focus();

            return;
        }

        // =====================================================
        // VALIDATE PASSWORD
        // =====================================================

        if (string.IsNullOrWhiteSpace(password))
        {
            lblStatus.ForeColor = Color.Firebrick;

            lblStatus.Text = "Please enter your password.";

            txtPassword.Focus();

            return;
        }

        try
        {
            // =================================================
            // DISABLE BUTTON
            // =================================================

            btnLogin.Enabled = false;

            btnLogin.Text = "SIGNING IN...";

            lblStatus.Text = "";

            // =================================================
            // CREATE HTTP CLIENT
            // =================================================

            using var client = new HttpClient();

            // =================================================
            // LOGIN DATA
            // =================================================

            var loginData = new
            {
                email = email,
                password = password
            };

            // =================================================
            // CONVERT TO JSON
            // =================================================

            var json = JsonSerializer.Serialize(loginData);

            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            // =================================================
            // SEND LOGIN REQUEST
            // =================================================

            var response = await client.PostAsync(
                $"{ApiUrl}/login",
                content);

            // =================================================
            // READ API RESPONSE
            // =================================================

            var responseJson =
                await response.Content.ReadAsStringAsync();

            // =================================================
            // LOGIN FAILED
            // =================================================

            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    var error =
                        JsonSerializer.Deserialize<LoginErrorResponse>(
                            responseJson,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                    lblStatus.ForeColor = Color.Firebrick;

                    lblStatus.Text =
                        error?.Message ??
                        "Invalid email or password.";
                }
                catch
                {
                    lblStatus.ForeColor = Color.Firebrick;

                    lblStatus.Text = "Login failed.";
                }

                return;
            }

            // =================================================
            // DESERIALIZE LOGIN RESPONSE
            // =================================================

            var loginResponse =
                JsonSerializer.Deserialize<LoginResponse>(
                    responseJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            // =================================================
            // CHECK TOKEN
            // =================================================

            if (loginResponse == null ||
                string.IsNullOrWhiteSpace(loginResponse.Token))
            {
                lblStatus.ForeColor = Color.Firebrick;

                lblStatus.Text =
                    "Invalid login response.";

                return;
            }

            // =================================================
            // SAVE SESSION INFORMATION
            // =================================================

            Session.Token =
                loginResponse.Token;

            Session.CompanyId =
                loginResponse.CompanyId;

            Session.Email =
                loginResponse.Email;

            Session.Roles =
                loginResponse.Roles ?? [];

          



            // =================================================
            // LOGIN SUCCESS
            // =================================================

            lblStatus.ForeColor =
                Color.FromArgb(40, 140, 80);

            lblStatus.Text =
                "Login successful.";

            // Give the UI a moment to display success
            await Task.Delay(300);

            // =================================================
            // HIDE LOGIN FORM
            // =================================================

            Hide();

            // =================================================
            // OPEN MAIN CRM
            // =================================================

            using (var mainForm = new Form1())
            {
                mainForm.ShowDialog();
            }

            // =================================================
            // CLOSE LOGIN FORM
            // =================================================

            Close();
        }
        catch (HttpRequestException)
        {
            lblStatus.ForeColor =
                Color.Firebrick;

            lblStatus.Text =
                "Cannot connect to CRM API.\n" +
                "Make sure CRM.api is running.";
        }
        catch (TaskCanceledException)
        {
            lblStatus.ForeColor =
                Color.Firebrick;

            lblStatus.Text =
                "The request timed out.";
        }
        catch (Exception ex)
        {
            lblStatus.ForeColor =
                Color.Firebrick;

            lblStatus.Text =
                $"Error: {ex.Message}";
        }
        finally
        {
            // =================================================
            // RESTORE LOGIN BUTTON
            // =================================================

            if (!IsDisposed)
            {
                btnLogin.Enabled = true;

                btnLogin.Text = "SIGN IN";
            }
        }
    }
}

// =============================================================
// LOGIN RESPONSE
// =============================================================

public class LoginResponse
{
    public string? Message { get; set; }

    public string? UserId { get; set; }

    public string? Email { get; set; }

    public int? CompanyId { get; set; }

    public List<string>? Roles { get; set; }

    public string? Token { get; set; }
}

// =============================================================
// LOGIN ERROR RESPONSE
// =============================================================

public class LoginErrorResponse
{
    public string? Message { get; set; }
}

// =============================================================
// SESSION
// =============================================================

public static class Session
{
    public static string? Token { get; set; }

    public static int? CompanyId { get; set; }

    public static string? Email { get; set; }

    public static List<string> Roles { get; set; } = [];
}