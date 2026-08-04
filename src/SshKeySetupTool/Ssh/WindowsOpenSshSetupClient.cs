using SshKeySetupTool.Domain;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace SshKeySetupTool.Ssh;

public sealed class WindowsOpenSshSetupClient : ISshSetupClient
{
    private static readonly TimeSpan HostDiscoveryConnectTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan HostDiscoveryProcessTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SshTimeout = TimeSpan.FromSeconds(45);

    private readonly Func<string, string, bool> _confirmHostKey;
    private readonly Func<WindowsOpenSshExecutables> _resolveExecutables;
    private readonly IProcessRunner _processRunner;
    private readonly string _askPassExecutable;
    private WindowsOpenSshExecutables? _executables;

    public WindowsOpenSshSetupClient(Func<string, string, bool> confirmHostKey)
        : this(
            confirmHostKey,
            WindowsOpenSshExecutableResolver.Resolve,
            new ProcessRunner(),
            Environment.ProcessPath
                ?? throw new InvalidOperationException(
                    "The application executable path could not be resolved."))
    {
    }

    internal WindowsOpenSshSetupClient(
        Func<string, string, bool> confirmHostKey,
        WindowsOpenSshExecutables executables,
        IProcessRunner processRunner,
        string askPassExecutable)
        : this(
            confirmHostKey,
            () => executables,
            processRunner,
            askPassExecutable)
    {
        ArgumentNullException.ThrowIfNull(executables);
    }

    internal WindowsOpenSshSetupClient(
        Func<string, string, bool> confirmHostKey,
        Func<WindowsOpenSshExecutables> resolveExecutables,
        IProcessRunner processRunner,
        string askPassExecutable)
    {
        _confirmHostKey = confirmHostKey ?? throw new ArgumentNullException(nameof(confirmHostKey));
        _resolveExecutables =
            resolveExecutables ?? throw new ArgumentNullException(nameof(resolveExecutables));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        ArgumentException.ThrowIfNullOrWhiteSpace(askPassExecutable);
        _askPassExecutable = Path.GetFullPath(askPassExecutable);
    }

    public async Task<OpenSshHostKey> ApproveHostKeyAsync(
        SetupRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var knownHostsPath = CreateTemporaryKnownHostsPath();
        try
        {
            var result = await _processRunner.RunAsync(
                CreateHostDiscoveryStartInfo(request, knownHostsPath),
                HostDiscoveryProcessTimeout,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(knownHostsPath))
            {
                throw CreateFailure(
                    SetupFailureKind.None,
                    "Windows OpenSSH host-key discovery failed",
                    result,
                    request.Password);
            }

            var knownHostsContent = await File.ReadAllTextAsync(
                knownHostsPath,
                cancellationToken);
            var hostKey = OpenSshHostKey.ParseKnownHostsOutput(knownHostsContent);
            if (!_confirmHostKey(request.Host, hostKey.Sha256Fingerprint))
            {
                throw new OperationCanceledException("The SSH server host key was not approved.");
            }

            return hostKey;
        }
        finally
        {
            File.Delete(knownHostsPath);
        }
    }

    public async Task InstallPublicKeyAsync(
        SetupRequest request,
        string command,
        OpenSshHostKey approvedHostKey,
        CancellationToken cancellationToken)
    {
        await RunPinnedSshAsync(
            request,
            approvedHostKey,
            knownHostsPath => CreatePasswordStartInfo(
                request,
                command,
                approvedHostKey,
                knownHostsPath),
            SetupFailureKind.PublicKeyInstallation,
            "Windows OpenSSH password login failed",
            cancellationToken);
    }

    public async Task<SshServerConfigurationProbe> InspectServerConfigurationAsync(
        SetupRequest request,
        OpenSshHostKey approvedHostKey,
        CancellationToken cancellationToken)
    {
        var result = await RunPinnedSshAsync(
            request,
            approvedHostKey,
            knownHostsPath => CreatePasswordStartInfo(
                request,
                LinuxSshServerConfigurationCommand.BuildProbe(),
                approvedHostKey,
                knownHostsPath),
            SetupFailureKind.ServerConfigurationInspection,
            "SSH server configuration inspection failed",
            cancellationToken);
        return SshServerConfigurationProbe.Parse(result.StandardOutput);
    }

    public async Task<SshServerConfigurationChange> EnablePublicKeyAuthenticationAsync(
        SetupRequest request,
        OpenSshHostKey approvedHostKey,
        string operationId,
        CancellationToken cancellationToken)
    {
        var result = await RunPinnedSshAsync(
            request,
            approvedHostKey,
            knownHostsPath => CreatePasswordStartInfo(
                request,
                LinuxSshServerConfigurationCommand.BuildApply(operationId),
                approvedHostKey,
                knownHostsPath),
            SetupFailureKind.ServerConfigurationApply,
            "SSH public-key authentication repair failed",
            cancellationToken);
        return LinuxSshServerConfigurationCommand.ParseApplyResult(
            operationId,
            result.StandardOutput);
    }

    public async Task RecoverServerConfigurationAsync(
        SetupRequest request,
        OpenSshHostKey approvedHostKey,
        string operationId,
        CancellationToken cancellationToken)
    {
        await RunPinnedSshAsync(
            request,
            approvedHostKey,
            knownHostsPath => CreatePasswordStartInfo(
                request,
                LinuxSshServerConfigurationCommand.BuildRecovery(operationId),
                approvedHostKey,
                knownHostsPath),
            SetupFailureKind.Rollback,
            "SSH server configuration recovery failed",
            cancellationToken);
    }

    public async Task CommitServerConfigurationAsync(
        SetupRequest request,
        OpenSshHostKey approvedHostKey,
        SshServerConfigurationChange change,
        CancellationToken cancellationToken)
    {
        await RunPinnedSshAsync(
            request,
            approvedHostKey,
            knownHostsPath => CreatePasswordStartInfo(
                request,
                LinuxSshServerConfigurationCommand.BuildCommit(change),
                approvedHostKey,
                knownHostsPath),
            SetupFailureKind.ServerConfigurationApply,
            "SSH server configuration commit failed",
            cancellationToken);
    }

    public async Task RollbackServerConfigurationAsync(
        SetupRequest request,
        OpenSshHostKey approvedHostKey,
        SshServerConfigurationChange change,
        CancellationToken cancellationToken)
    {
        await RunPinnedSshAsync(
            request,
            approvedHostKey,
            knownHostsPath => CreatePasswordStartInfo(
                request,
                LinuxSshServerConfigurationCommand.BuildRollback(change),
                approvedHostKey,
                knownHostsPath),
            SetupFailureKind.Rollback,
            "SSH server configuration rollback failed",
            cancellationToken);
    }

    public async Task VerifyPrivateKeyAsync(
        SetupRequest request,
        string privateKeyPath,
        OpenSshHostKey approvedHostKey,
        CancellationToken cancellationToken)
    {
        await RunPinnedSshAsync(
            request,
            approvedHostKey,
            knownHostsPath => CreatePrivateKeyStartInfo(
                request,
                privateKeyPath,
                approvedHostKey,
                knownHostsPath),
            SetupFailureKind.PrivateKeyVerification,
            "Windows OpenSSH private-key verification failed",
            cancellationToken);
    }

    private async Task<ProcessResult> RunPinnedSshAsync(
        SetupRequest request,
        OpenSshHostKey approvedHostKey,
        Func<string, ProcessStartInfo> createStartInfo,
        SetupFailureKind failureKind,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var knownHostsPath = CreateTemporaryKnownHostsPath();
        try
        {
            await File.WriteAllTextAsync(
                knownHostsPath,
                approvedHostKey.KnownHostsLine + "\n",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                cancellationToken);
            var result = await _processRunner.RunAsync(
                createStartInfo(knownHostsPath),
                SshTimeout,
                cancellationToken);
            if (result.ExitCode != 0)
            {
                throw CreateFailure(
                    failureKind,
                    failureMessage,
                    result,
                    request.Password);
            }

            return result;
        }
        finally
        {
            File.Delete(knownHostsPath);
        }
    }

    private ProcessStartInfo CreateHostDiscoveryStartInfo(
        SetupRequest request,
        string knownHostsPath)
    {
        var startInfo = CreateBaseStartInfo(ResolveExecutables().SshPath);
        startInfo.ArgumentList.Add("-F");
        startInfo.ArgumentList.Add("none");
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(request.Port.ToString(CultureInfo.InvariantCulture));
        AddOption(startInfo, "StrictHostKeyChecking=accept-new");
        AddOption(startInfo, $"UserKnownHostsFile={knownHostsPath}");
        AddOption(startInfo, "GlobalKnownHostsFile=NUL");
        AddOption(startInfo, "HashKnownHosts=no");
        AddOption(startInfo, "BatchMode=yes");
        AddOption(startInfo, "NumberOfPasswordPrompts=0");
        AddOption(startInfo, "PasswordAuthentication=no");
        AddOption(startInfo, "KbdInteractiveAuthentication=no");
        AddOption(startInfo, "PubkeyAuthentication=no");
        AddOption(
            startInfo,
            $"ConnectTimeout={(int)HostDiscoveryConnectTimeout.TotalSeconds}");
        AddOption(startInfo, "LogLevel=ERROR");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add($"{request.Username}@{request.Host}");
        startInfo.ArgumentList.Add("true");
        return startInfo;
    }

    private ProcessStartInfo CreatePasswordStartInfo(
        SetupRequest request,
        string command,
        OpenSshHostKey approvedHostKey,
        string knownHostsPath)
    {
        var startInfo = CreateSshStartInfo(request, approvedHostKey, knownHostsPath);
        AddOption(startInfo, "BatchMode=no");
        AddOption(startInfo, "NumberOfPasswordPrompts=1");
        AddOption(startInfo, "PreferredAuthentications=password,keyboard-interactive");
        AddOption(startInfo, "PubkeyAuthentication=no");
        AddOption(startInfo, "KbdInteractiveAuthentication=yes");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add($"{request.Username}@{request.Host}");
        startInfo.ArgumentList.Add(command);
        startInfo.Environment["SSH_ASKPASS"] = _askPassExecutable;
        startInfo.Environment["SSH_ASKPASS_REQUIRE"] = "force";
        startInfo.Environment[WindowsOpenSshPasswordFallback.AskPassModeEnvironmentVariable] =
            WindowsOpenSshPasswordFallback.AskPassModeValue;
        startInfo.Environment[WindowsOpenSshPasswordFallback.PasswordEnvironmentVariable] =
            request.Password;
        return startInfo;
    }

    private ProcessStartInfo CreatePrivateKeyStartInfo(
        SetupRequest request,
        string privateKeyPath,
        OpenSshHostKey approvedHostKey,
        string knownHostsPath)
    {
        var startInfo = CreateSshStartInfo(request, approvedHostKey, knownHostsPath);
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(privateKeyPath);
        AddOption(startInfo, "IdentitiesOnly=yes");
        AddOption(startInfo, "BatchMode=yes");
        AddOption(startInfo, "PasswordAuthentication=no");
        AddOption(startInfo, "KbdInteractiveAuthentication=no");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add($"{request.Username}@{request.Host}");
        startInfo.ArgumentList.Add("true");
        return startInfo;
    }

    private ProcessStartInfo CreateSshStartInfo(
        SetupRequest request,
        OpenSshHostKey approvedHostKey,
        string knownHostsPath)
    {
        var startInfo = CreateBaseStartInfo(ResolveExecutables().SshPath);
        startInfo.ArgumentList.Add("-F");
        startInfo.ArgumentList.Add("none");
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(request.Port.ToString(CultureInfo.InvariantCulture));
        AddOption(startInfo, "StrictHostKeyChecking=yes");
        AddOption(startInfo, $"UserKnownHostsFile={knownHostsPath}");
        AddOption(startInfo, "GlobalKnownHostsFile=NUL");
        AddOption(
            startInfo,
            $"HostKeyAlgorithms={GetHostKeyAlgorithms(approvedHostKey.KeyType)}");
        AddOption(startInfo, "LogLevel=ERROR");
        return startInfo;
    }

    private WindowsOpenSshExecutables ResolveExecutables() =>
        _executables ??= _resolveExecutables()
            ?? throw new InvalidOperationException(
                "The Windows OpenSSH executable resolver returned no result.");

    private static string CreateTemporaryKnownHostsPath() =>
        Path.Combine(
            Path.GetTempPath(),
            $"ssh-key-setup-{Guid.NewGuid():N}.known_hosts");

    private static string GetHostKeyAlgorithms(string keyType) =>
        string.Equals(keyType, "ssh-rsa", StringComparison.Ordinal)
            ? "rsa-sha2-512,rsa-sha2-256,ssh-rsa"
            : keyType;

    private static ProcessStartInfo CreateBaseStartInfo(string executablePath) =>
        new(executablePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

    private static void AddOption(ProcessStartInfo startInfo, string option)
    {
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(option);
    }

    private static SshSetupOperationException CreateFailure(
        SetupFailureKind failureKind,
        string message,
        ProcessResult result,
        string password)
    {
        var details = string.Concat(result.StandardError, result.StandardOutput).Trim();
        if (!string.IsNullOrEmpty(password))
        {
            details = details.Replace(password, "[redacted]", StringComparison.Ordinal);
        }

        return new SshSetupOperationException(
            failureKind,
            string.IsNullOrEmpty(details)
                ? $"{message} (exit code {result.ExitCode})."
                : $"{message}: {details}");
    }
}
