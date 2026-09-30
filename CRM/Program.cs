namespace CRM_DesignServices.winforms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) =>
        {
            // Network drops or background HTTP exceptions should never force exit the app
            System.Diagnostics.Debug.WriteLine($"WinForms UI Thread Exception handled: {e.Exception.Message}");
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AppDomain Unhandled Exception handled: {ex.Message}");
            }
        };

        Application.Run(new LoginForm());
    }
}