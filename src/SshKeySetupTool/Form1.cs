using SshKeySetupTool.Domain;
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
    private Icon? _applicationIcon;
    private CancellationTokenSource? _setupCancellation;
    private Task? _setupTask;
    private bool _closeRequested;
    private bool _allowClose;
    private UiLanguage _language = UiLanguage.Chinese;
    private OpenSshClientStatus? _openSshStatus;
    private bool _openSshOperationInProgress = true;
    private Func<UiText, string>? _statusTextFactory;
    private Color _statusColor = Color.FromArgb(127, 149, 163);

    public Form1()
        : this(null, useDefaultService: true)
    {
    }

    internal Form1(IKeySetupService keySetupService)
        : this(keySetupService, useDefaultService: false, null)
    {
    }

    internal Form1(
        IKeySetupService keySetupService,
        IOpenSshClientManager openSshClientManager)
        : this(keySetupService, useDefaultService: false, openSshClientManager)
    {
    }

    private Form1(
        IKeySetupService? keySetupService,
        bool useDefaultService,
        IOpenSshClientManager? openSshClientManager = null)
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
        SetLocalizedStatus(text => text.Working, WorkingColor);

        try
        {
            var request = BuildRequest();
            var result = await _keySetupService.RunAsync(request, cancellationToken);
            if (result.Succeeded)
            {
                var connectionDetails = CodexConnectionDetails.Format(request, result.PrivateKeyPath!);
                connectionDetailsTextBox.Text = connectionDetails;
                SetLocalizedStatus(
                    TryCopyToClipboard(connectionDetails)
                        ? text => text.CompletedCopied
                        : text => text.CompletedNotCopied,
                    SuccessColor);
            }
            else
            {
                SetStatus(result.Message, ErrorColor);
            }
        }
        catch (OperationCanceledException)
        {
            SetLocalizedStatus(text => text.Cancelled, Color.FromArgb(127, 149, 163));
        }
        catch (Exception exception)
        {
            SetLocalizedStatus(text => text.FailedPrefix + exception.Message, ErrorColor);
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

    private Domain.SetupRequest BuildRequest() => SetupFormInput.BuildRequest(
        hostTextBox.Text,
        portTextBox.Text,
        usernameTextBox.Text,
        passwordTextBox.Text,
        privateKeyPathTextBox.Text);

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
}
