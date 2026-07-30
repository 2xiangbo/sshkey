using System.Drawing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using SshKeySetupTool;

namespace SshKeySetupTool.Tests.Presentation;

public sealed class FormLayoutTests
{
    [Fact]
    public void Form1_UsesTwoInputRowsAndKeepsOutputBelowStatus()
    {
        RunInSta(() =>
        {
            using var form = new Form1();
            var host = Find<TextBox>(form, "hostTextBox");
            var status = Find<TextBox>(form, "statusTextBox");
            var connectionDetails = Find<TextBox>(form, "connectionDetailsTextBox");
            var connectionDetailsLabel = Find<Label>(form, "connectionDetailsLabel");
            var generate = Find<Button>(form, "generateButton");
            var titleBar = Find<Panel>(form, "titleBarPanel");
            var minimize = Find<Button>(form, "minimizeButton");
            var close = Find<Button>(form, "closeButton");
            var headerTitle = Find<Label>(form, "headerTitleLabel");
            var language = Find<ComboBox>(form, "languageComboBox");
            var project = Find<LinkLabel>(form, "projectLinkLabel");
            var xxCodex = Find<LinkLabel>(form, "xxCodexLinkLabel");
            var hostInput = Find<Panel>(form, "hostInputPanel");
            var portInput = Find<Panel>(form, "portInputPanel");
            var openSshInput = Find<Panel>(form, "openSshInputPanel");
            var usernameInput = Find<Panel>(form, "usernameInputPanel");
            var passwordInput = Find<Panel>(form, "passwordInputPanel");
            var privateKeyPathInput = Find<Panel>(form, "privateKeyPathInputPanel");
            var statusInput = Find<Panel>(form, "statusInputPanel");

            Assert.Equal(new Size(680, 520), form.ClientSize);
            Assert.Equal("SSHKEY   //   SSH密钥设置", form.Text);
            Assert.Equal(form.Text, headerTitle.Text);
            Assert.Equal("中文", language.SelectedItem);
            Assert.Equal("https://github.com/2xiangbo/sshkey", project.Tag);
            Assert.Equal("https://xxcodex.com", xxCodex.Tag);
            Assert.Equal(FormBorderStyle.None, form.FormBorderStyle);
            Assert.Equal(42, titleBar.Height);
            Assert.Equal(Color.FromArgb(11, 17, 24), form.BackColor);
            Assert.Equal(Color.FromArgb(56, 215, 255), generate.BackColor);
            Assert.Equal(FlatStyle.Flat, minimize.FlatStyle);
            Assert.Equal(FlatStyle.Flat, close.FlatStyle);
            Assert.True(form.Height < 560);
            Assert.False(status.Multiline);
            Assert.Equal(Color.FromArgb(14, 24, 34), host.BackColor);
            Assert.Equal(Color.FromArgb(14, 24, 34), connectionDetails.BackColor);
            Assert.Equal(hostInput.Top, portInput.Top);
            Assert.Equal(portInput.Top, openSshInput.Top);
            Assert.True(hostInput.Left < portInput.Left);
            Assert.Equal(302, hostInput.Width);
            Assert.Equal(76, portInput.Width);
            Assert.True(hostInput.Right < portInput.Left);
            Assert.True(portInput.Right < openSshInput.Left);
            Assert.True(openSshInput.Right <= form.ClientSize.Width - 20);
            Assert.Equal(BorderStyle.None, host.BorderStyle);
            Assert.Equal(1, hostInput.Padding.Left);
            Assert.Equal(usernameInput.Top, passwordInput.Top);
            Assert.True(usernameInput.Left < passwordInput.Left);
            Assert.Equal(usernameInput.Width, passwordInput.Width);
            Assert.True(privateKeyPathInput.Top > usernameInput.Bottom);
            Assert.True(statusInput.Left > connectionDetailsLabel.Right);
            Assert.True(Math.Abs(statusInput.Top + statusInput.Height / 2 - (connectionDetailsLabel.Top + connectionDetailsLabel.Height / 2)) <= 2);
            Assert.True(connectionDetails.Top > statusInput.Bottom);
            Assert.Equal(FlatStyle.Flat, generate.FlatStyle);
            Assert.NotEqual(SystemColors.Control, form.BackColor);

            language.SelectedItem = "EN";

            Assert.Equal("SSHKEY   //   SSH Key Setup", form.Text);
            Assert.Equal(form.Text, headerTitle.Text);
            Assert.Equal("Generate and Install", generate.Text);
        });
    }

    [Fact]
    public void Form1_CustomChromeMinimizesAndStatusPresentationCanChangeTone()
    {
        RunInSta(() =>
        {
            using var form = new Form1();
            var minimize = Find<Button>(form, "minimizeButton");
            var status = Find<TextBox>(form, "statusTextBox");
            var setStatus = typeof(Form1).GetMethod(
                "SetStatus",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(setStatus);
            setStatus.Invoke(
                form,
                new object[] { "failed", Color.FromArgb(255, 107, 122) });
            Assert.Equal("failed", status.Text);
            Assert.Equal(Color.FromArgb(255, 107, 122), status.ForeColor);

            form.ShowInTaskbar = false;
            form.Show();
            Application.DoEvents();
            minimize.PerformClick();
            Assert.Equal(FormWindowState.Minimized, form.WindowState);
        });
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
}
