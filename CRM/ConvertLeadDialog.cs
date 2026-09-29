using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class ConvertLeadDialog : CrmModalDialog
{
    public string CustomerType { get; private set; } = "Regular";
    public string ProjectType { get; private set; } = "";
    public string Location { get; private set; } = "";
    public string Description { get; private set; } = "";

    private readonly ComboBox _cmbType;
    private readonly TextBox _txtProjectType;
    private readonly TextBox _txtLocation;
    private readonly TextBox _txtDescription;

    public ConvertLeadDialog(string leadName)
        : base("Convert Lead", $"Converting '{leadName}' to Customer & Project record", "🔄", "Convert Lead", 580)
    {
        _cmbType = AddDropdownField("Customer Type *", new[] { "Regular", "VIP", "Corporate", "Walk-in" }, "Regular", true);
        AddTwoTextFields("Project / Service Type *", "e.g. Interior Design, Package", out _txtProjectType,
                         "Location *", "e.g. Makati, BGC, Main Branch", out _txtLocation, true, true);
        _txtDescription = AddTextAreaField("Description / Scope Notes", "Brief scope, requirements, or client preferences...", 75);

        SubmitButton.Click += (_, _) =>
        {
            CustomerType = _cmbType.SelectedItem?.ToString() ?? "Regular";
            ProjectType = _txtProjectType.Text.Trim();
            Location = _txtLocation.Text.Trim();
            Description = _txtDescription.Text.Trim();
        };
    }

    public object ToPayload() => new
    {
        customerType = CustomerType,
        projectType = ProjectType,
        location = Location,
        description = Description
    };
}