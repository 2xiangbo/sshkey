using SshKeySetupTool.Domain;
using SshKeySetupTool.History;
using SshKeySetupTool.Security;
using SshKeySetupTool.Services;
using SshKeySetupTool.Ssh;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;

namespace SshKeySetupTool.Tests.Presentation;

public sealed class FormLifecycleTests
{
    [Fact]
    public void EachSetupClickReceivesADistinctCancelableToken()
    {
        RunInSta(() =>
        {
            var setupService = new ImmediateSetupService();
            using var form = new Form1(setupService);
            PopulateRequiredFields(form);
            form.ShowInTaskbar = false;
            form.Show();
            var generate = Find<Button>(form, "generateButton");

            generate.PerformClick();
            Application.DoEvents();
            generate.PerformClick();
            Application.DoEvents();

            Assert.Equal(2, setupService.Tokens.Count);
            Assert.All(setupService.Tokens, token => Assert.True(token.CanBeCanceled));
            Assert.NotEqual(setupService.Tokens[0], setupService.Tokens[1]);
        });
    }

    [Fact]
    public void ClosingDuringSetupCancelsAndWaitsForActiveWork()
    {
        RunInSta(() =>
        {
            var setupService = new BlockingSetupService();
            using var form = new Form1(setupService);
            PopulateRequiredFields(form);
            form.ShowInTaskbar = false;
            form.Show();
            Find<Button>(form, "generateButton").PerformClick();
            PumpUntil(() => setupService.Started);

            Find<Button>(form, "closeButton").PerformClick();
            PumpUntil(() => setupService.CancellationObserved);

            Assert.False(form.IsDisposed);
            setupService.Release();
            PumpUntil(() => form.IsDisposed);
            Assert.True(setupService.Finished);
        });
    }

    [Fact]
    public void MissingOpenSshIsShownInStatusAfterGenerateClick()
    {
        RunInSta(() =>
        {
            var client = new WindowsOpenSshSetupClient(
                (_, _) => true,
                () => throw new FileNotFoundException(
                    "Windows OpenSSH Client is required. Enable it in Windows Optional Features, then retry."),
                new UnexpectedProcessRunner(),
                @"C:\tool\SshKeySetupTool.exe");
            using var form = new Form1(
                new KeySetupService(new FakeKeyMaterialFactory(), client));
            PopulateRequiredFields(form);
            form.ShowInTaskbar = false;
            form.Show();

            Find<Button>(form, "generateButton").PerformClick();
            var statusTextBox = Find<TextBox>(form, "statusTextBox");
            PumpUntil(() => statusTextBox.Text.Contains(
                "Windows OpenSSH Client is required",
                StringComparison.Ordinal));

            var status = statusTextBox.Text;
            Assert.Contains("Windows OpenSSH Client is required", status);
            Assert.Contains("Optional Features", status);
            Assert.Empty(Find<TextBox>(form, "passwordTextBox").Text);
        });
    }

    [Fact]
    public void FailedInitialOpenSshCheck_DoesNotOfferInstallation()
    {
        RunInSta(() =>
        {
            using var form = new Form1(new ImmediateSetupService(), new FailingOpenSshManager());
            form.ShowInTaskbar = false;
            form.Show();
            var openSsh = Find<Button>(form, "openSshButton");

            PumpUntil(() => !openSsh.Text.StartsWith("检测 OpenSSH", StringComparison.Ordinal));

            Assert.False(openSsh.Enabled);
            Assert.Contains("检测失败", openSsh.Text);
        });
    }

    [Fact]
    public void ClosingAfterRollbackFailureKeepsRecoveryDetailsVisible()
    {
        RunInSta(() =>
        {
            var setupService = new RollbackFailureAfterCancellationSetupService();
            using var form = new Form1(setupService);
            PopulateRequiredFields(form);
            form.ShowInTaskbar = false;
            form.Show();
            Find<Button>(form, "generateButton").PerformClick();
            PumpUntil(() => setupService.Started);

            Find<Button>(form, "closeButton").PerformClick();
            PumpUntil(() => setupService.Finished);

            Assert.False(form.IsDisposed);
            var status = Find<TextBox>(form, "statusTextBox").Text;
            Assert.Contains("Remote backup", status, StringComparison.Ordinal);
            Assert.Contains("sshd_config.sshkey-setup-operation.bak", status, StringComparison.Ordinal);

            Find<Button>(form, "closeButton").PerformClick();
            PumpUntil(() => form.IsDisposed);
        });
    }

    [Fact]
    public void SetupProgressIsLocalizedAgainWhenLanguageChanges()
    {
        RunInSta(() =>
        {
            var setupService = new ProgressReportingSetupService();
            using var form = new Form1(setupService);
            PopulateRequiredFields(form);
            form.ShowInTaskbar = false;
            form.Show();

            Find<Button>(form, "generateButton").PerformClick();
            var status = Find<TextBox>(form, "statusTextBox");
            PumpUntil(() => status.Text.Contains("服务器 SSH 配置", StringComparison.Ordinal));

            var language = Find<ComboBox>(form, "languageComboBox");
            language.SelectedItem = "EN";
            PumpUntil(() => status.Text.Contains("Checking server SSH configuration", StringComparison.Ordinal));

            setupService.Release();
            PumpUntil(() => setupService.Finished);
        });
    }

    [Fact]
    public void SuccessfulSetup_PersistsTheSameConnectionDetailsShownInTheForm()
    {
        RunInSta(() =>
        {
            var history = new InMemoryHistoryStore();
            using var form = new Form1(
                new SuccessfulSetupService(),
                new InstalledOpenSshManager(),
                history);
            PopulateRequiredFields(form);
            form.ShowInTaskbar = false;
            form.Show();

            Find<Button>(form, "generateButton").PerformClick();
            PumpUntil(() => history.Entries.Count == 1);

            var shownDetails = Find<TextBox>(form, "connectionDetailsTextBox").Text;
            var entry = Assert.Single(history.Entries);
            Assert.Equal(shownDetails, entry.ConnectionDetails);
            Assert.Equal("203.0.113.10", entry.Host);
            Assert.DoesNotContain(
                typeof(GenerationHistoryEntry).GetProperties(),
                property => property.Name.Contains("password", StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public void FailedSetup_DoesNotPersistHistory()
    {
        RunInSta(() =>
        {
            var history = new InMemoryHistoryStore();
            using var form = new Form1(
                new ImmediateSetupService(),
                new InstalledOpenSshManager(),
                history);
            PopulateRequiredFields(form);
            form.ShowInTaskbar = false;
            form.Show();

            Find<Button>(form, "generateButton").PerformClick();
            PumpUntil(() => Find<Button>(form, "generateButton").Enabled);

            Assert.Empty(history.Entries);
        });
    }

    private static void PopulateRequiredFields(Form form)
    {
        Find<TextBox>(form, "hostTextBox").Text = "203.0.113.10";
        Find<TextBox>(form, "portTextBox").Text = "22";
        Find<TextBox>(form, "usernameTextBox").Text = "root";
        Find<TextBox>(form, "passwordTextBox").Text = "secret";
        Find<TextBox>(form, "privateKeyPathTextBox").Text = @"C:\keys\id_ed25519";
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }

        Assert.True(condition());
    }

    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception caught)
            {
                exception = caught;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    private static TControl Find<TControl>(Control root, string name)
        where TControl : Control
    {
        var control = Assert.Single(root.Controls.Find(name, searchAllChildren: true));
        return Assert.IsType<TControl>(control);
    }

    private sealed class ImmediateSetupService : IKeySetupService
    {
        public List<CancellationToken> Tokens { get; } = [];

        public Task<SetupResult> RunAsync(
            SetupRequest request,
            Func<SetupRequest, SshServerConfigurationProbe, bool> confirmServerConfiguration,
            IProgress<SetupProgress>? progress,
            CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            return Task.FromResult(new SetupResult(false, "test"));
        }
    }

    private sealed class BlockingSetupService : IKeySetupService
    {
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool Started { get; private set; }
        public bool CancellationObserved { get; private set; }
        public bool Finished { get; private set; }

        public async Task<SetupResult> RunAsync(
            SetupRequest request,
            Func<SetupRequest, SshServerConfigurationProbe, bool> confirmServerConfiguration,
            IProgress<SetupProgress>? progress,
            CancellationToken cancellationToken)
        {
            Started = true;
            using var registration = cancellationToken.Register(
                () => CancellationObserved = true);
            try
            {
                await _release.Task;
                cancellationToken.ThrowIfCancellationRequested();
                return new SetupResult(true, "test", request.PrivateKeyPath);
            }
            finally
            {
                Finished = true;
            }
        }

        public void Release() => _release.TrySetResult();
    }

    private sealed class RollbackFailureAfterCancellationSetupService : IKeySetupService
    {
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool Started { get; private set; }
        public bool Finished { get; private set; }

        public async Task<SetupResult> RunAsync(
            SetupRequest request,
            Func<SetupRequest, SshServerConfigurationProbe, bool> confirmServerConfiguration,
            IProgress<SetupProgress>? progress,
            CancellationToken cancellationToken)
        {
            Started = true;
            using var registration = cancellationToken.Register(() => _release.TrySetResult());
            try
            {
                await _release.Task;
                return new SetupResult(
                    false,
                    "Remote backup: /etc/ssh/sshd_config.sshkey-setup-operation.bak",
                    FailureKind: SetupFailureKind.Rollback);
            }
            finally
            {
                Finished = true;
            }
        }
    }

    private sealed class ProgressReportingSetupService : IKeySetupService
    {
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool Finished { get; private set; }

        public async Task<SetupResult> RunAsync(
            SetupRequest request,
            Func<SetupRequest, SshServerConfigurationProbe, bool> confirmServerConfiguration,
            IProgress<SetupProgress>? progress,
            CancellationToken cancellationToken)
        {
            progress?.Report(new(SetupPhase.CheckingServerConfiguration));
            try
            {
                await _release.Task;
                return new SetupResult(false, "test");
            }
            finally
            {
                Finished = true;
            }
        }

        public void Release() => _release.TrySetResult();
    }

    private sealed class SuccessfulSetupService : IKeySetupService
    {
        public Task<SetupResult> RunAsync(
            SetupRequest request,
            Func<SetupRequest, SshServerConfigurationProbe, bool> confirmServerConfiguration,
            IProgress<SetupProgress>? progress,
            CancellationToken cancellationToken) =>
            Task.FromResult(new SetupResult(true, "test", request.PrivateKeyPath));
    }

    private sealed class FakeKeyMaterialFactory : IKeyMaterialFactory
    {
        public KeyMaterial Create(string privateKeyPath) =>
            new(privateKeyPath, privateKeyPath + ".pub", "ssh-ed25519 test");
    }

    private sealed class UnexpectedProcessRunner : IProcessRunner
    {
        public Task<ProcessResult> RunAsync(
            ProcessStartInfo startInfo,
            TimeSpan timeout,
            CancellationToken cancellationToken) =>
            throw new Xunit.Sdk.XunitException(
                "A process must not start when Windows OpenSSH is missing.");
    }

    private sealed class FailingOpenSshManager : IOpenSshClientManager
    {
        public Task<OpenSshClientStatus> CheckAsync(CancellationToken cancellationToken) =>
            Task.FromException<OpenSshClientStatus>(new InvalidOperationException("check failed"));

        public Task<OpenSshClientStatus> InstallAsync(CancellationToken cancellationToken) =>
            throw new Xunit.Sdk.XunitException("Installation must not be offered after a check failure.");
    }

    private sealed class InstalledOpenSshManager : IOpenSshClientManager
    {
        public Task<OpenSshClientStatus> CheckAsync(CancellationToken cancellationToken) =>
            Task.FromResult(OpenSshClientStatus.Installed);

        public Task<OpenSshClientStatus> InstallAsync(CancellationToken cancellationToken) =>
            Task.FromResult(OpenSshClientStatus.Installed);
    }

    private sealed class InMemoryHistoryStore : IGenerationHistoryStore
    {
        public List<GenerationHistoryEntry> Entries { get; } = [];

        public IReadOnlyList<GenerationHistoryEntry> Read() => Entries;

        public void Append(GenerationHistoryEntry entry) => Entries.Add(entry);

        public void Clear() => Entries.Clear();
    }
}
