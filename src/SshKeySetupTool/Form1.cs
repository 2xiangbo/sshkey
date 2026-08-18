using SshKeySetupTool.Domain;
using SshKeySetupTool.History;
using SshKeySetupTool.Presentation;
using SshKeySetupTool.Security;
using SshKeySetupTool.Services;
using SshKeySetupTool.Ssh;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SshKeySetupTool;

public partial class Form1 : Form
{
    private const int WmNcLButtonDown = 0x00A1;
    private const int HtCaption = 0x0002;
    private static readonly Color WorkingColor = Color.FromArgb(56, 215, 255);
    private static readonly Color SuccessColor = Color.FromArgb(69, 230, 161);
    private static readonly Color ErrorColor = Color.FromArgb(255, 107, 122);

    private readonly IKeySetupService _keySetupService;
    private readonly IOpenSshClientManager _openSshClientManager;
    private readonly IGenerationHistoryStore _generationHistoryStore;
    private Icon? _applicationIcon;
    private CancellationTokenSource? _setupCancellation;
    private Task? _setupTask;
    private bool _closeRequested;
    private bool _allowClose;
    private bool _manualRecoveryRequired;
    private UiLanguage _language = UiLanguage.Chinese;
    private OpenSshClientStatus? _openSshStatus;
    private bool _openSshOperationInProgress = true;
    private Func<UiText, string>? _statusTextFactory;
    private Color _statusColor = Color.FromArgb(127, 149, 163);
    private SetupPhase? _latestSetupPhase;

    public Form1()
        : this(null, useDefaultService: true, null, null)
    {
    }

    internal Form1(IKeySetupService keySetupService)
        : this(keySetupService, useDefaultService: false, null, null)
    {
    }

    internal Form1(
        IKeySetupService keySetupService,
        IOpenSshClientManager openSshClientManager)
        : this(keySetupService, useDefaultService: false, openSshClientManager, null)
    {
    }

    internal Form1(
        IKeySetupService keySetupService,
        IOpenSshClientManager openSshClientManager,
        IGenerationHistoryStore generationHistoryStore)
        : this(keySetupService, useDefaultService: false, openSshClientManager, generationHistoryStore)
    {
    }

    private Form1(
        IKeySetupService? keySetupService,
        bool useDefaultService,
        IOpenSshClientManager? openSshClientManager,
        IGenerationHistoryStore? generationHistoryStore)
    {
        InitializeComponent();
        _applicationIcon = AppIcon.Load();
        Icon = _applicationIcon;
        WireWindowChrome();
        privateKeyPathTextBox.Text = SetupFormInput.GetSuggestedPrivateKeyPath(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        _keySetupService = useDefaultService
            ? new KeySetupService(
                new OpenSshKeyMaterialFactory(),
                new WindowsOpenSshSetupClient(ConfirmHostKey))
            : keySetupService ?? throw new ArgumentNullException(nameof(keySetupService));
        _openSshClientManager = openSshClientManager ?? new WindowsOpenSshClientManager();
        _generationHistoryStore = generationHistoryStore ?? JsonGenerationHistoryStore.CreateDefault();
        languageComboBox.SelectedItem = UiTextCatalog.For(_language).LanguageChoice;
        ApplyLanguage();
    }

    private async void generateButton_Click(object? sender, EventArgs e)
    {
        if (_setupTask is not null)
        {
            return;
        }

        var cancellation = new CancellationTokenSource();
        _setupCancellation = cancellation;
        var setupTask = RunSetupAsync(cancellation.Token);
        _setupTask = setupTask;
        try
        {
            await setupTask;
        }
        finally
        {
            if (ReferenceEquals(_setupCancellation, cancellation))
            {
                _setupCancellation = null;
                _setupTask = null;
            }

            cancellation.Dispose();
        }
    }

    private async Task RunSetupAsync(CancellationToken cancellationToken)
    {
        generateButton.Enabled = false;
        connectionDetailsTextBox.Clear();
        _latestSetupPhase = null;
        _manualRecoveryRequired = false;
        SetLocalizedStatus(text => text.Working, WorkingColor);
        var request = BuildRequest();

        try
        {
            var progress = new Progress<SetupProgress>(HandleSetupProgress);
            var result = await _keySetupService.RunAsync(
                request,
                ConfirmServerConfiguration,
                progress,
                cancellationToken);
            if (result.Succeeded)
            {
                var connectionDetails = CodexConnectionDetails.Format(request, result.PrivateKeyPath!);
                connectionDetailsTextBox.Text = connectionDetails;
                SetLocalizedStatus(
                    TryCopyToClipboard(connectionDetails)
                        ? text => text.CompletedCopied
                        : text => text.CompletedNotCopied,
                    SuccessColor);
                RecordHistory(request, GenerationHistoryOutcome.Succeeded, "Completed");
            }
            else
            {
                _manualRecoveryRequired = result.FailureKind == SetupFailureKind.Rollback;
                SetLocalizedStatus(
                    text => FormatFailure(text, result),
                    ErrorColor);
                RecordHistory(request, GenerationHistoryOutcome.Failed, "Failed");
            }
        }
        catch (OperationCanceledException)
        {
            SetLocalizedStatus(text => text.Cancelled, Color.FromArgb(127, 149, 163));
            RecordHistory(request, GenerationHistoryOutcome.Cancelled, "Cancelled");
        }
        catch (Exception exception)
        {
            SetLocalizedStatus(text => text.FailedPrefix + exception.Message, ErrorColor);
            RecordHistory(request, GenerationHistoryOutcome.Failed, "Failed");
        }
        finally
        {
            passwordTextBox.Clear();
            if (!_closeRequested && !IsDisposed)
            {
                generateButton.Enabled = true;
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(
        IntPtr windowHandle,
        int message,
        int wordParameter,
        int longParameter);

    private void WireWindowChrome()
    {
        titleBarPanel.MouseDown += titleBar_MouseDown;
        headerTitleLabel.MouseDown += titleBar_MouseDown;
        minimizeButton.Click += minimizeButton_Click;
        closeButton.Click += closeButton_Click;
        FormClosing += Form1_FormClosing;
        Shown += Form1_Shown;
        languageComboBox.SelectedIndexChanged += languageComboBox_SelectedIndexChanged;
        openSshButton.Click += openSshButton_Click;
        browsePrivateKeyPathButton.Click += browsePrivateKeyPathButton_Click;
        generationHistoryButton.Click += generationHistoryButton_Click;
        projectLinkLabel.LinkClicked += externalLinkLabel_LinkClicked;
        xxCodexLinkLabel.LinkClicked += externalLinkLabel_LinkClicked;
    }

    private void titleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        ReleaseCapture();
        SendMessage(Handle, WmNcLButtonDown, HtCaption, 0);
    }

    private void minimizeButton_Click(object? sender, EventArgs e) =>
        WindowState = FormWindowState.Minimized;

    private void closeButton_Click(object? sender, EventArgs e) => Close();

    private async void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        var activeTask = _setupTask;
        if (activeTask is null || activeTask.IsCompleted)
        {
            return;
        }

        e.Cancel = true;
        if (_closeRequested)
        {
            return;
        }

        _closeRequested = true;
        generateButton.Enabled = false;
        _setupCancellation?.Cancel();
        try
        {
            await activeTask;
        }
        catch (OperationCanceledException)
        {
        }

        if (_manualRecoveryRequired)
        {
            _closeRequested = false;
            generateButton.Enabled = true;
            return;
        }

        _allowClose = true;
        Close();
    }

    private void SetStatus(string text, Color color)
    {
        _statusTextFactory = null;
        _statusColor = color;
        statusTextBox.Text = text;
        statusTextBox.ForeColor = color;
    }

    private void SetLocalizedStatus(Func<UiText, string> textFactory, Color color)
    {
        _statusTextFactory = textFactory;
        _statusColor = color;
        statusTextBox.Text = textFactory(CurrentText);
        statusTextBox.ForeColor = color;
    }

    private UiText CurrentText => UiTextCatalog.For(_language);

    private string FormatFailure(UiText text, SetupResult result)
    {
        var label = text.FailureLabel(result.FailureKind);
        return string.IsNullOrWhiteSpace(result.Message)
            ? label
            : $"{label}\r\n{result.Message}";
    }

    private void HandleSetupProgress(SetupProgress progress)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => HandleSetupProgress(progress)));
            return;
        }

        _latestSetupPhase = progress.Phase;
        var isRollback = progress.Phase == SetupPhase.RollingBackServerConfiguration;
        SetLocalizedStatus(
            text => GetPhaseText(text, _latestSetupPhase ?? progress.Phase),
            isRollback ? ErrorColor : WorkingColor);
    }

    private static string GetPhaseText(UiText text, SetupPhase phase) => phase switch
    {
        SetupPhase.GeneratingKey => text.Working,
        SetupPhase.DiscoveringServer => text.Working,
        SetupPhase.CheckingServerConfiguration => text.CheckingServerConfiguration,
        SetupPhase.WaitingForServerConfigurationConsent => text.WaitingForServerConfigurationConsent,
        SetupPhase.EnablingServerConfiguration => text.EnablingServerConfiguration,
        SetupPhase.InstallingPublicKey => text.InstallingPublicKey,
        SetupPhase.VerifyingPrivateKey => text.VerifyingPrivateKey,
        SetupPhase.RollingBackServerConfiguration => text.RollingBackServerConfiguration,
        _ => text.Working
    };

    private void ApplyLanguage()
    {
        var text = CurrentText;
        Text = text.Title;
        headerTitleLabel.Text = text.Title;
        hostLabel.Text = text.Host;
        portLabel.Text = text.Port;
        usernameLabel.Text = text.Username;
        passwordLabel.Text = text.Password;
        privateKeyPathLabel.Text = text.PrivateKeyPath;
        statusLabel.Text = text.Status;
        connectionDetailsLabel.Text = text.ConnectionDetails;
        generateButton.Text = text.GenerateAndInstall;
        browsePrivateKeyPathButton.Text = text.BrowsePrivateKeyPath;
        generationHistoryButton.Text = text.GenerationHistory;

        if (_statusTextFactory is null)
        {
            SetLocalizedStatus(current => current.Ready, Color.FromArgb(127, 149, 163));
        }
        else
        {
            statusTextBox.Text = _statusTextFactory(text);
            statusTextBox.ForeColor = _statusColor;
        }

        RenderOpenSshState();
    }

    private async void Form1_Shown(object? sender, EventArgs e) =>
        await RefreshOpenSshStatusAsync();

    private void languageComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _language = string.Equals(languageComboBox.SelectedItem as string, "EN", StringComparison.Ordinal)
            ? UiLanguage.English
            : UiLanguage.Chinese;
        ApplyLanguage();
    }

    private async void openSshButton_Click(object? sender, EventArgs e)
    {
        if (_openSshOperationInProgress || _openSshStatus == OpenSshClientStatus.Installed)
        {
            return;
        }

        if (_openSshStatus is not (OpenSshClientStatus.Missing or OpenSshClientStatus.InstallFailed or OpenSshClientStatus.InstallCancelled))
        {
            return;
        }

        _openSshOperationInProgress = true;
        RenderOpenSshState();
        try
        {
            _openSshStatus = await _openSshClientManager.InstallAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            _openSshStatus = OpenSshClientStatus.InstallCancelled;
        }
        catch
        {
            _openSshStatus = OpenSshClientStatus.InstallFailed;
        }
        finally
        {
            _openSshOperationInProgress = false;
            if (!IsDisposed)
            {
                RenderOpenSshState();
            }
        }
    }

    private async Task RefreshOpenSshStatusAsync()
    {
        _openSshOperationInProgress = true;
        RenderOpenSshState();
        try
        {
            _openSshStatus = await _openSshClientManager.CheckAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            _openSshStatus = OpenSshClientStatus.InstallCancelled;
        }
        catch
        {
            _openSshStatus = OpenSshClientStatus.CheckFailed;
        }
        finally
        {
            _openSshOperationInProgress = false;
            if (!IsDisposed)
            {
                RenderOpenSshState();
            }
        }
    }

    private void RenderOpenSshState()
    {
        var text = CurrentText;
        if (_openSshOperationInProgress)
        {
            openSshButton.Enabled = false;
            openSshButton.BackColor = Color.FromArgb(38, 55, 71);
            openSshButton.ForeColor = Color.FromArgb(233, 245, 250);
            openSshButton.Text = text.CheckingOpenSsh;
            return;
        }

        if (_openSshStatus == OpenSshClientStatus.Installed)
        {
            openSshButton.Enabled = false;
            openSshButton.BackColor = SuccessColor;
            openSshButton.ForeColor = Color.FromArgb(4, 25, 34);
            openSshButton.Text = text.OpenSshInstalled;
            return;
        }

        if (_openSshStatus == OpenSshClientStatus.CheckFailed)
        {
            openSshButton.Enabled = false;
            openSshButton.BackColor = Color.FromArgb(38, 55, 71);
            openSshButton.ForeColor = ErrorColor;
            openSshButton.Text = text.OpenSshCheckFailed;
            return;
        }

        openSshButton.Enabled = true;
        openSshButton.ForeColor = Color.FromArgb(4, 25, 34);
        openSshButton.BackColor = _openSshStatus == OpenSshClientStatus.Missing
            ? WorkingColor
            : ErrorColor;
        openSshButton.Text = _openSshStatus == OpenSshClientStatus.InstallCancelled
            ? text.OpenSshInstallCancelled
            : _openSshStatus == OpenSshClientStatus.InstallFailed
                ? text.OpenSshInstallFailed
                : text.InstallOpenSsh;
    }

    private void externalLinkLabel_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        if (sender is not LinkLabel { Tag: string url })
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            SetLocalizedStatus(text => text.FailedPrefix + exception.Message, ErrorColor);
        }
    }

    private void browsePrivateKeyPathButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            UseDescriptionForTitle = true,
            Description = CurrentText.PrivateKeyPath
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            privateKeyPathTextBox.Text = SetupFormInput.GetSuggestedPrivateKeyPathInDirectory(
                dialog.SelectedPath);
        }
    }

    private void generationHistoryButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new GenerationHistoryForm(_generationHistoryStore, _language);
        dialog.ShowDialog(this);
    }

    private Domain.SetupRequest BuildRequest() => SetupFormInput.BuildRequest(
        hostTextBox.Text,
        portTextBox.Text,
        usernameTextBox.Text,
        passwordTextBox.Text,
        privateKeyPathTextBox.Text);

    private void RecordHistory(
        SetupRequest request,
        GenerationHistoryOutcome outcome,
        string message)
    {
        try
        {
            _generationHistoryStore.Append(new GenerationHistoryEntry(
                DateTimeOffset.UtcNow,
                request.Host,
                request.Port,
                request.Username,
                request.PrivateKeyPath,
                outcome,
                message));
        }
        catch
        {
            // History must never interfere with an SSH setup result.
        }
    }

    private static bool TryCopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    private bool ConfirmHostKey(string host, string fingerprint)
    {
        if (InvokeRequired)
        {
            return (bool)Invoke(new Func<bool>(() => ConfirmHostKey(host, fingerprint)));
        }

        var text = CurrentText;
        var message = string.Format(text.ConfirmServerMessageFormat, host, fingerprint);
        return MessageBox.Show(this, message, text.ConfirmServerTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
    }

    private bool ConfirmServerConfiguration(
        SetupRequest request,
        SshServerConfigurationProbe probe)
    {
        if (InvokeRequired)
        {
            return (bool)Invoke(new Func<bool>(
                () => ConfirmServerConfiguration(request, probe)));
        }

        var text = CurrentText;
        return MessageBox.Show(
            this,
            string.Format(
                text.ConfirmServerConfigurationMessageFormat,
                request.Host,
                probe.RawOutput.Trim()),
            text.ConfirmServerConfigurationTitle,
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) == DialogResult.OK;
    }
}
