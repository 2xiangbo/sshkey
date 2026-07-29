using SshKeySetupTool.Domain;
using SshKeySetupTool.Security;
using SshKeySetupTool.Services;
using SshKeySetupTool.Ssh;
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
    private Icon? _applicationIcon;
    private CancellationTokenSource? _setupCancellation;
    private Task? _setupTask;
    private bool _closeRequested;
    private bool _allowClose;

    public Form1()
        : this(null, useDefaultService: true)
    {
    }

    internal Form1(IKeySetupService keySetupService)
        : this(keySetupService, useDefaultService: false)
    {
    }

    private Form1(IKeySetupService? keySetupService, bool useDefaultService)
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
        SetStatus("正在生成本地密钥并连接服务器...", WorkingColor);

        try
        {
            var request = BuildRequest();
            var result = await _keySetupService.RunAsync(request, cancellationToken);
            if (result.Succeeded)
            {
                var connectionDetails = CodexConnectionDetails.Format(request, result.PrivateKeyPath!);
                connectionDetailsTextBox.Text = connectionDetails;
                SetStatus(
                    TryCopyToClipboard(connectionDetails)
                        ? "\u5b8c\u6210\uff0cCodex \u8fde\u63a5\u4fe1\u606f\u5df2\u663e\u793a\u5728\u4e0b\u65b9\u5e76\u590d\u5236\u5230\u526a\u8d34\u677f\u3002"
                        : "\u5b8c\u6210\uff0cCodex \u8fde\u63a5\u4fe1\u606f\u5df2\u663e\u793a\u5728\u4e0b\u65b9\uff0c\u8bf7\u624b\u52a8\u590d\u5236\u3002",
                    SuccessColor);
            }
            else
            {
                SetStatus(result.Message, ErrorColor);
            }
        }
        catch (OperationCanceledException)
        {
            SetStatus("操作已取消。", Color.FromArgb(127, 149, 163));
        }
        catch (Exception exception)
        {
            SetStatus($"失败：{exception.Message}", ErrorColor);
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
        statusTextBox.Text = text;
        statusTextBox.ForeColor = color;
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

        var message = $"首次连接服务器 {host}。\r\n\r\n服务器 SHA-256 密钥指纹：\r\n{fingerprint}\r\n\r\n是否仅在本次操作中信任此服务器？";
        return MessageBox.Show(this, message, "确认服务器", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
    }
}
