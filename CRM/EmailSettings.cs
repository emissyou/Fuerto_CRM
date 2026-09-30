using System;
using System.IO;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

/// <summary>
/// Configuration model for SMTP email delivery (Gmail, Outlook, custom SMTP).
/// Persisted per installation in AppData to enable real email sending.
/// </summary>
public class EmailSettings
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;

    public string SenderEmail { get; set; } = string.Empty;
    public string SenderPassword { get; set; } = string.Empty; // 16-character Google App Password
    public string SenderDisplayName { get; set; } = "Fuerto Interior Design Services";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SenderEmail) &&
        !string.IsNullOrWhiteSpace(SenderPassword);

    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FuertoCRM");

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "email_settings.json");

    private static EmailSettings? _current;

    public static EmailSettings Current
    {
        get
        {
            if (_current == null)
            {
                _current = Load();
            }
            return _current;
        }
    }

    public static EmailSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                var loaded = JsonSerializer.Deserialize<EmailSettings>(json);
                if (loaded != null)
                {
                    _current = loaded;
                    return loaded;
                }
            }
        }
        catch
        {
            // Ignore load errors and fallback to fresh default
        }

        _current = new EmailSettings();
        return _current;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
            _current = this;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save email settings: {ex.Message}");
        }
    }
}
