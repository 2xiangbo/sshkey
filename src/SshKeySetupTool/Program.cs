namespace SshKeySetupTool;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        var askPassMode = Environment.GetEnvironmentVariable(
            Ssh.WindowsOpenSshPasswordFallback.AskPassModeEnvironmentVariable);
        var password = Environment.GetEnvironmentVariable(Ssh.WindowsOpenSshPasswordFallback.PasswordEnvironmentVariable);
        if (Ssh.WindowsOpenSshPasswordFallback.TryGetPasswordResponse(
            args,
            askPassMode,
            password,
            out var response))
        {
            Ssh.WindowsOpenSshPasswordFallback.WriteAskPassPassword(Console.OpenStandardOutput(), response!);
            return;
        }

        if (string.Equals(
            askPassMode,
            Ssh.WindowsOpenSshPasswordFallback.AskPassModeValue,
            StringComparison.Ordinal))
        {
            Environment.ExitCode = 1;
            return;
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}
