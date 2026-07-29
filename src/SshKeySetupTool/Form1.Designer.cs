using System.Drawing;
using System.Windows.Forms;

namespace SshKeySetupTool;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Icon = null;
            System.Threading.Interlocked.Exchange(ref _applicationIcon, null)?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        titleBarPanel = new Panel();
        headerTitleLabel = new Label();
        minimizeButton = new Button();
        closeButton = new Button();
        headerRulePanel = new Panel();
        hostLabel = new Label();
        portLabel = new Label();
        usernameLabel = new Label();
        passwordLabel = new Label();
        privateKeyPathLabel = new Label();
        statusLabel = new Label();
        connectionDetailsLabel = new Label();
        hostTextBox = new TextBox();
        portTextBox = new TextBox();
        usernameTextBox = new TextBox();
        passwordTextBox = new TextBox();
        privateKeyPathTextBox = new TextBox();
        statusTextBox = new TextBox();
        connectionDetailsTextBox = new TextBox();
        generateButton = new Button();
        var inputBackColor = Color.FromArgb(14, 24, 34);
        var primaryTextColor = Color.FromArgb(233, 245, 250);
        var secondaryTextColor = Color.FromArgb(127, 149, 163);
        var neutralBorderColor = Color.FromArgb(38, 55, 71);
        var cyanColor = Color.FromArgb(56, 215, 255);
        SuspendLayout();
        titleBarPanel.SuspendLayout();
        //
        // titleBarPanel
        //
        titleBarPanel.BackColor = Color.FromArgb(16, 27, 38);
        titleBarPanel.Controls.Add(closeButton);
        titleBarPanel.Controls.Add(minimizeButton);
        titleBarPanel.Controls.Add(headerTitleLabel);
        titleBarPanel.Location = new Point(1, 1);
        titleBarPanel.Name = "titleBarPanel";
        titleBarPanel.Size = new Size(678, 42);
        titleBarPanel.TabIndex = 0;
        //
        // headerTitleLabel
        //
        headerTitleLabel.AutoSize = true;
        headerTitleLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        headerTitleLabel.ForeColor = Color.FromArgb(233, 245, 250);
        headerTitleLabel.Location = new Point(14, 11);
        headerTitleLabel.Name = "headerTitleLabel";
        headerTitleLabel.Size = new Size(184, 19);
        headerTitleLabel.TabIndex = 0;
        headerTitleLabel.Text = "CODEX  //  SSH \u5bc6\u94a5\u8bbe\u7f6e";
        //
        // minimizeButton
        //
        minimizeButton.BackColor = titleBarPanel.BackColor;
        minimizeButton.FlatAppearance.BorderSize = 0;
        minimizeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(38, 55, 71);
        minimizeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 43, 57);
        minimizeButton.FlatStyle = FlatStyle.Flat;
        minimizeButton.Font = new Font("Segoe MDL2 Assets", 10F, FontStyle.Regular, GraphicsUnit.Point);
        minimizeButton.ForeColor = primaryTextColor;
        minimizeButton.Location = new Point(606, 0);
        minimizeButton.Name = "minimizeButton";
        minimizeButton.Size = new Size(36, 42);
        minimizeButton.TabIndex = 1;
        minimizeButton.Text = "\uE921";
        minimizeButton.UseVisualStyleBackColor = false;
        //
        // closeButton
        //
        closeButton.BackColor = titleBarPanel.BackColor;
        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(255, 107, 122);
        closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(91, 47, 59);
        closeButton.FlatStyle = FlatStyle.Flat;
        closeButton.Font = new Font("Segoe MDL2 Assets", 10F, FontStyle.Regular, GraphicsUnit.Point);
        closeButton.ForeColor = primaryTextColor;
        closeButton.Location = new Point(642, 0);
        closeButton.Name = "closeButton";
        closeButton.Size = new Size(36, 42);
        closeButton.TabIndex = 2;
        closeButton.Text = "\uE8BB";
        closeButton.UseVisualStyleBackColor = false;
        //
        // headerRulePanel
        //
        headerRulePanel.BackColor = cyanColor;
        headerRulePanel.Location = new Point(1, 43);
        headerRulePanel.Name = "headerRulePanel";
        headerRulePanel.Size = new Size(678, 1);
        headerRulePanel.TabIndex = 1;
        //
        // hostLabel
        //
        hostLabel.AutoSize = true;
        hostLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
        hostLabel.ForeColor = secondaryTextColor;
        hostLabel.Location = new Point(20, 58);
        hostLabel.Name = "hostLabel";
        hostLabel.Size = new Size(66, 15);
        hostLabel.TabIndex = 2;
        hostLabel.Text = "\u670d\u52a1\u5668 IP";
        //
        // portLabel
        //
        portLabel.AutoSize = true;
        portLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
        portLabel.ForeColor = secondaryTextColor;
        portLabel.Location = new Point(490, 58);
        portLabel.Name = "portLabel";
        portLabel.Size = new Size(28, 15);
        portLabel.TabIndex = 3;
        portLabel.Text = "\u7aef\u53e3";
        //
        // usernameLabel
        //
        usernameLabel.AutoSize = true;
        usernameLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
        usernameLabel.ForeColor = secondaryTextColor;
        usernameLabel.Location = new Point(20, 114);
        usernameLabel.Name = "usernameLabel";
        usernameLabel.Size = new Size(28, 15);
        usernameLabel.TabIndex = 4;
        usernameLabel.Text = "\u8d26\u53f7";
        //
        // passwordLabel
        //
        passwordLabel.AutoSize = true;
        passwordLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
        passwordLabel.ForeColor = secondaryTextColor;
        passwordLabel.Location = new Point(347, 114);
        passwordLabel.Name = "passwordLabel";
        passwordLabel.Size = new Size(28, 15);
        passwordLabel.TabIndex = 5;
        passwordLabel.Text = "\u5bc6\u7801";
        //
        // privateKeyPathLabel
        //
        privateKeyPathLabel.AutoSize = true;
        privateKeyPathLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
        privateKeyPathLabel.ForeColor = secondaryTextColor;
        privateKeyPathLabel.Location = new Point(20, 170);
        privateKeyPathLabel.Name = "privateKeyPathLabel";
        privateKeyPathLabel.Size = new Size(49, 15);
        privateKeyPathLabel.TabIndex = 6;
        privateKeyPathLabel.Text = "\u79c1\u94a5\u8def\u5f84";
        //
        // statusLabel
        //
        statusLabel.AutoSize = true;
        statusLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
        statusLabel.ForeColor = secondaryTextColor;
        statusLabel.Location = new Point(20, 226);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(28, 15);
        statusLabel.TabIndex = 7;
        statusLabel.Text = "\u72b6\u6001";
        //
        // connectionDetailsLabel
        //
        connectionDetailsLabel.AutoSize = true;
        connectionDetailsLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
        connectionDetailsLabel.ForeColor = secondaryTextColor;
        connectionDetailsLabel.Location = new Point(20, 288);
        connectionDetailsLabel.Name = "connectionDetailsLabel";
        connectionDetailsLabel.Size = new Size(83, 15);
        connectionDetailsLabel.TabIndex = 8;
        connectionDetailsLabel.Text = "Codex \u8fde\u63a5\u4fe1\u606f";
        //
        // hostTextBox
        //
        hostTextBox.BackColor = inputBackColor;
        hostTextBox.BorderStyle = BorderStyle.FixedSingle;
        hostTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        hostTextBox.ForeColor = primaryTextColor;
        hostTextBox.Location = new Point(20, 76);
        hostTextBox.Name = "hostTextBox";
        hostTextBox.Size = new Size(456, 28);
        hostTextBox.TabIndex = 0;
        //
        // portTextBox
        //
        portTextBox.BackColor = inputBackColor;
        portTextBox.BorderStyle = BorderStyle.FixedSingle;
        portTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        portTextBox.ForeColor = primaryTextColor;
        portTextBox.Location = new Point(490, 76);
        portTextBox.Name = "portTextBox";
        portTextBox.Size = new Size(170, 28);
        portTextBox.TabIndex = 1;
        portTextBox.Text = "22";
        //
        // usernameTextBox
        //
        usernameTextBox.BackColor = inputBackColor;
        usernameTextBox.BorderStyle = BorderStyle.FixedSingle;
        usernameTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        usernameTextBox.ForeColor = primaryTextColor;
        usernameTextBox.Location = new Point(20, 132);
        usernameTextBox.Name = "usernameTextBox";
        usernameTextBox.Size = new Size(313, 28);
        usernameTextBox.TabIndex = 2;
        //
        // passwordTextBox
        //
        passwordTextBox.BackColor = inputBackColor;
        passwordTextBox.BorderStyle = BorderStyle.FixedSingle;
        passwordTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        passwordTextBox.ForeColor = primaryTextColor;
        passwordTextBox.Location = new Point(347, 132);
        passwordTextBox.Name = "passwordTextBox";
        passwordTextBox.Size = new Size(313, 28);
        passwordTextBox.TabIndex = 3;
        passwordTextBox.UseSystemPasswordChar = true;
        //
        // privateKeyPathTextBox
        //
        privateKeyPathTextBox.BackColor = inputBackColor;
        privateKeyPathTextBox.BorderStyle = BorderStyle.FixedSingle;
        privateKeyPathTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        privateKeyPathTextBox.ForeColor = primaryTextColor;
        privateKeyPathTextBox.Location = new Point(20, 188);
        privateKeyPathTextBox.Name = "privateKeyPathTextBox";
        privateKeyPathTextBox.Size = new Size(640, 28);
        privateKeyPathTextBox.TabIndex = 4;
        //
        // statusTextBox
        //
        statusTextBox.BackColor = inputBackColor;
        statusTextBox.BorderStyle = BorderStyle.FixedSingle;
        statusTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        statusTextBox.ForeColor = primaryTextColor;
        statusTextBox.Location = new Point(20, 244);
        statusTextBox.Multiline = false;
        statusTextBox.Name = "statusTextBox";
        statusTextBox.ReadOnly = true;
        statusTextBox.Size = new Size(640, 30);
        statusTextBox.TabIndex = 5;
        statusTextBox.TabStop = false;
        statusTextBox.Text = "\u51c6\u5907\u5c31\u7eea\u3002";
        //
        // connectionDetailsTextBox
        //
        connectionDetailsTextBox.BackColor = inputBackColor;
        connectionDetailsTextBox.BorderStyle = BorderStyle.FixedSingle;
        connectionDetailsTextBox.Font = new Font("Consolas", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        connectionDetailsTextBox.ForeColor = primaryTextColor;
        connectionDetailsTextBox.Location = new Point(20, 306);
        connectionDetailsTextBox.Multiline = true;
        connectionDetailsTextBox.Name = "connectionDetailsTextBox";
        connectionDetailsTextBox.ReadOnly = true;
        connectionDetailsTextBox.ScrollBars = ScrollBars.Vertical;
        connectionDetailsTextBox.Size = new Size(640, 132);
        connectionDetailsTextBox.TabIndex = 6;
        connectionDetailsTextBox.TabStop = false;
        //
        // generateButton
        //
        generateButton.BackColor = cyanColor;
        generateButton.Cursor = Cursors.Hand;
        generateButton.FlatAppearance.BorderSize = 0;
        generateButton.FlatAppearance.BorderColor = neutralBorderColor;
        generateButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(26, 166, 195);
        generateButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(104, 226, 255);
        generateButton.FlatStyle = FlatStyle.Flat;
        generateButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        generateButton.ForeColor = Color.FromArgb(4, 25, 34);
        generateButton.Location = new Point(440, 458);
        generateButton.Name = "generateButton";
        generateButton.Size = new Size(220, 40);
        generateButton.TabIndex = 5;
        generateButton.Text = "\u751f\u6210\u5e76\u5199\u5165\u670d\u52a1\u5668";
        generateButton.UseVisualStyleBackColor = false;
        generateButton.Click += new EventHandler(generateButton_Click);
        //
        // Form1
        //
        AcceptButton = generateButton;
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.FromArgb(11, 17, 24);
        ClientSize = new Size(680, 520);
        Controls.Add(generateButton);
        Controls.Add(connectionDetailsTextBox);
        Controls.Add(statusTextBox);
        Controls.Add(privateKeyPathTextBox);
        Controls.Add(passwordTextBox);
        Controls.Add(usernameTextBox);
        Controls.Add(portTextBox);
        Controls.Add(hostTextBox);
        Controls.Add(connectionDetailsLabel);
        Controls.Add(statusLabel);
        Controls.Add(privateKeyPathLabel);
        Controls.Add(passwordLabel);
        Controls.Add(usernameLabel);
        Controls.Add(portLabel);
        Controls.Add(hostLabel);
        Controls.Add(headerRulePanel);
        Controls.Add(titleBarPanel);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        ForeColor = primaryTextColor;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "Form1";
        Padding = new Padding(1);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CODEX  //  SSH \u5bc6\u94a5\u8bbe\u7f6e";
        titleBarPanel.ResumeLayout(false);
        titleBarPanel.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Panel titleBarPanel;
    private Label headerTitleLabel;
    private Button minimizeButton;
    private Button closeButton;
    private Panel headerRulePanel;
    private Label hostLabel;
    private Label portLabel;
    private Label usernameLabel;
    private Label passwordLabel;
    private Label privateKeyPathLabel;
    private Label statusLabel;
    private Label connectionDetailsLabel;
    private TextBox hostTextBox;
    private TextBox portTextBox;
    private TextBox usernameTextBox;
    private TextBox passwordTextBox;
    private TextBox privateKeyPathTextBox;
    private TextBox statusTextBox;
    private TextBox connectionDetailsTextBox;
    private Button generateButton;
}
