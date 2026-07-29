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
            var port = Find<TextBox>(form, "portTextBox");
            var username = Find<TextBox>(form, "usernameTextBox");
            var password = Find<TextBox>(form, "passwordTextBox");
            var privateKeyPath = Find<TextBox>(form, "privateKeyPathTextBox");
            var status = Find<TextBox>(form, "statusTextBox");
            var connectionDetails = Find<TextBox>(form, "connectionDetailsTextBox");
            var generate = Find<Button>(form, "generateButton");
            var titleBar = Find<Panel>(form, "titleBarPanel");
            var minimize = Find<Button>(form, "minimizeButton");
            var close = Find<Button>(form, "closeButton");

            Assert.Equal(new Size(680, 520), form.ClientSize);
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
            Assert.Equal(host.Top, port.Top);
            Assert.True(host.Left < port.Left);
            Assert.Equal(username.Top, password.Top);
            Assert.True(username.Left < password.Left);
            Assert.True(privateKeyPath.Top > username.Bottom);
            Assert.True(status.Top > privateKeyPath.Bottom);
            Assert.True(connectionDetails.Top > status.Bottom);
            Assert.Equal(FlatStyle.Flat, generate.FlatStyle);
            Assert.NotEqual(SystemColors.Control, form.BackColor);
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
