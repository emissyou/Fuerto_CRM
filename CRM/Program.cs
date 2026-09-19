namespace CRM_DesignServices.winforms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.Run(new LoginForm());
    }
}