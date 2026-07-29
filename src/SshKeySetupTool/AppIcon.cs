namespace SshKeySetupTool;

internal static class AppIcon
{
    private const string ResourceName =
        "SshKeySetupTool.Assets.ssh-key-tool-icon.ico";

    public static Icon Load()
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded application icon '{ResourceName}' was not found.");
        using var source = new Icon(stream);
        return (Icon)source.Clone();
    }
}
