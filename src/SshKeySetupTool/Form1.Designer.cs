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
        hostInputPanel = new Panel();
        portInputPanel = new Panel();
        openSshInputPanel = new Panel();
        usernameInputPanel = new Panel();
        passwordInputPanel = new Panel();
        privateKeyPathInputPanel = new Panel();
        statusInputPanel = new Panel();
        generateButton = new Button();
        browsePrivateKeyPathButton = new Button();
        generationHistoryButton = new Button();
        languageComboBox = new ComboBox();
        openSshButton = new Button();
        projectLinkLabel = new LinkLabel();
        xxCodexLinkLabel = new LinkLabel();
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
        headerTitleLabel.Text = "SSHKEY   //   SSH\u5bc6\u94a5\u8bbe\u7f6e";
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
        portLabel.Location = new Point(336, 58);
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
        passwordLabel.Location = new Point(194, 114);
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
        statusLabel.Visible = false;
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
        hostTextBox.BorderStyle = BorderStyle.None;
        hostTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        hostTextBox.ForeColor = primaryTextColor;
        hostTextBox.Location = new Point(9, 5);
        hostTextBox.Name = "hostTextBox";
        hostTextBox.Size = new Size(284, 22);
        hostTextBox.TabIndex = 0;
        //
        // portTextBox
        //
        portTextBox.BackColor = inputBackColor;
        portTextBox.BorderStyle = BorderStyle.None;
        portTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        portTextBox.ForeColor = primaryTextColor;
        portTextBox.Location = new Point(9, 5);
        portTextBox.Name = "portTextBox";
        portTextBox.Size = new Size(56, 22);
        portTextBox.TabIndex = 1;
        portTextBox.Text = "22";
        //
        // usernameTextBox
        //
        usernameTextBox.BackColor = inputBackColor;
        usernameTextBox.BorderStyle = BorderStyle.None;
        usernameTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        usernameTextBox.ForeColor = primaryTextColor;
        usernameTextBox.Location = new Point(9, 5);
        usernameTextBox.Name = "usernameTextBox";
        usernameTextBox.Size = new Size(140, 22);
        usernameTextBox.TabIndex = 2;
        //
        // passwordTextBox
        //
        passwordTextBox.BackColor = inputBackColor;
        passwordTextBox.BorderStyle = BorderStyle.None;
        passwordTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        passwordTextBox.ForeColor = primaryTextColor;
        passwordTextBox.Location = new Point(9, 5);
        passwordTextBox.Name = "passwordTextBox";
        passwordTextBox.Size = new Size(180, 22);
        passwordTextBox.TabIndex = 3;
        passwordTextBox.UseSystemPasswordChar = true;
        //
        // privateKeyPathTextBox
        //
        privateKeyPathTextBox.BackColor = inputBackColor;
        privateKeyPathTextBox.BorderStyle = BorderStyle.None;
        privateKeyPathTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        privateKeyPathTextBox.ForeColor = primaryTextColor;
        privateKeyPathTextBox.Location = new Point(9, 5);
        privateKeyPathTextBox.Name = "privateKeyPathTextBox";
        privateKeyPathTextBox.Size = new Size(514, 22);
        privateKeyPathTextBox.TabIndex = 4;
        //
        // statusTextBox
        //
        statusTextBox.BackColor = inputBackColor;
        statusTextBox.BorderStyle = BorderStyle.None;
        statusTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        statusTextBox.ForeColor = primaryTextColor;
        statusTextBox.Location = new Point(9, 4);
        statusTextBox.Multiline = true;
        statusTextBox.Name = "statusTextBox";
        statusTextBox.ReadOnly = true;
        statusTextBox.Size = new Size(392, 48);
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
        connectionDetailsTextBox.Location = new Point(20, 350);
        connectionDetailsTextBox.Multiline = true;
        connectionDetailsTextBox.Name = "connectionDetailsTextBox";
        connectionDetailsTextBox.ReadOnly = true;
        connectionDetailsTextBox.ScrollBars = ScrollBars.Vertical;
        connectionDetailsTextBox.Size = new Size(640, 88);
        connectionDetailsTextBox.TabIndex = 6;
        connectionDetailsTextBox.TabStop = false;
        //
        // hostInputPanel
        //
        hostInputPanel.BackColor = inputBackColor;
        hostInputPanel.BorderStyle = BorderStyle.FixedSingle;
        hostInputPanel.Controls.Add(hostTextBox);
        hostInputPanel.Location = new Point(20, 76);
        hostInputPanel.Name = "hostInputPanel";
        hostInputPanel.Padding = new Padding(1);
        hostInputPanel.Size = new Size(302, 32);
        hostInputPanel.TabIndex = 0;
        //
        // portInputPanel
        //
        portInputPanel.BackColor = inputBackColor;
        portInputPanel.BorderStyle = BorderStyle.FixedSingle;
        portInputPanel.Controls.Add(portTextBox);
        portInputPanel.Location = new Point(336, 76);
        portInputPanel.Name = "portInputPanel";
        portInputPanel.Padding = new Padding(1);
        portInputPanel.Size = new Size(76, 32);
        portInputPanel.TabIndex = 1;
        //
        // openSshInputPanel
        //
        openSshInputPanel.BackColor = inputBackColor;
        openSshInputPanel.BorderStyle = BorderStyle.FixedSingle;
        openSshInputPanel.Controls.Add(openSshButton);
        openSshInputPanel.Location = new Point(426, 76);
        openSshInputPanel.Name = "openSshInputPanel";
        openSshInputPanel.Padding = new Padding(1);
        openSshInputPanel.Size = new Size(234, 32);
        openSshInputPanel.TabIndex = 2;
        //
        // usernameInputPanel
        //
        usernameInputPanel.BackColor = inputBackColor;
        usernameInputPanel.BorderStyle = BorderStyle.FixedSingle;
        usernameInputPanel.Controls.Add(usernameTextBox);
        usernameInputPanel.Location = new Point(20, 132);
        usernameInputPanel.Name = "usernameInputPanel";
        usernameInputPanel.Padding = new Padding(1);
        usernameInputPanel.Size = new Size(160, 32);
        usernameInputPanel.TabIndex = 3;
        //
        // passwordInputPanel
        //
        passwordInputPanel.BackColor = inputBackColor;
        passwordInputPanel.BorderStyle = BorderStyle.FixedSingle;
        passwordInputPanel.Controls.Add(passwordTextBox);
        passwordInputPanel.Location = new Point(194, 132);
        passwordInputPanel.Name = "passwordInputPanel";
        passwordInputPanel.Padding = new Padding(1);
        passwordInputPanel.Size = new Size(200, 32);
        passwordInputPanel.TabIndex = 4;
        //
        // privateKeyPathInputPanel
        //
        privateKeyPathInputPanel.BackColor = inputBackColor;
        privateKeyPathInputPanel.BorderStyle = BorderStyle.FixedSingle;
        privateKeyPathInputPanel.Controls.Add(privateKeyPathTextBox);
        privateKeyPathInputPanel.Location = new Point(20, 188);
        privateKeyPathInputPanel.Name = "privateKeyPathInputPanel";
        privateKeyPathInputPanel.Padding = new Padding(1);
        privateKeyPathInputPanel.Size = new Size(534, 32);
        privateKeyPathInputPanel.TabIndex = 5;
        //
        // statusInputPanel
        //
        statusInputPanel.BackColor = inputBackColor;
        statusInputPanel.BorderStyle = BorderStyle.FixedSingle;
        statusInputPanel.Controls.Add(statusTextBox);
        statusInputPanel.Location = new Point(250, 285);
        statusInputPanel.Name = "statusInputPanel";
        statusInputPanel.Padding = new Padding(1);
        statusInputPanel.Size = new Size(410, 56);
        statusInputPanel.TabIndex = 6;
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
        // browsePrivateKeyPathButton
        //
        browsePrivateKeyPathButton.BackColor = Color.FromArgb(38, 55, 71);
        browsePrivateKeyPathButton.FlatAppearance.BorderSize = 0;
        browsePrivateKeyPathButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(28, 43, 57);
        browsePrivateKeyPathButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(56, 75, 91);
        browsePrivateKeyPathButton.FlatStyle = FlatStyle.Flat;
        browsePrivateKeyPathButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        browsePrivateKeyPathButton.ForeColor = primaryTextColor;
        browsePrivateKeyPathButton.Location = new Point(568, 188);
        browsePrivateKeyPathButton.Name = "browsePrivateKeyPathButton";
        browsePrivateKeyPathButton.Size = new Size(92, 32);
        browsePrivateKeyPathButton.TabIndex = 6;
        browsePrivateKeyPathButton.Text = "\u6d4f\u89c8...";
        browsePrivateKeyPathButton.UseVisualStyleBackColor = false;
        //
        // generationHistoryButton
        //
        generationHistoryButton.BackColor = Color.FromArgb(38, 55, 71);
        generationHistoryButton.FlatAppearance.BorderSize = 0;
        generationHistoryButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(28, 43, 57);
        generationHistoryButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(56, 75, 91);
        generationHistoryButton.FlatStyle = FlatStyle.Flat;
        generationHistoryButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        generationHistoryButton.ForeColor = primaryTextColor;
        generationHistoryButton.Location = new Point(408, 132);
        generationHistoryButton.Name = "generationHistoryButton";
        generationHistoryButton.Size = new Size(252, 32);
        generationHistoryButton.TabIndex = 5;
        generationHistoryButton.Text = "\u751f\u6210\u5386\u53f2";
        generationHistoryButton.UseVisualStyleBackColor = false;
        //
        // languageComboBox
        //
        languageComboBox.BackColor = inputBackColor;
        languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        languageComboBox.FlatStyle = FlatStyle.Flat;
        languageComboBox.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        languageComboBox.ForeColor = primaryTextColor;
        languageComboBox.FormattingEnabled = true;
        languageComboBox.Items.AddRange(new object[] { "\u4e2d\u6587", "EN" });
        languageComboBox.Location = new Point(356, 465);
        languageComboBox.Name = "languageComboBox";
        languageComboBox.Size = new Size(70, 25);
        languageComboBox.TabIndex = 7;
        //
        // openSshButton
        //
        openSshButton.BackColor = Color.FromArgb(38, 55, 71);
        openSshButton.FlatAppearance.BorderColor = neutralBorderColor;
        openSshButton.FlatStyle = FlatStyle.Flat;
        openSshButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        openSshButton.ForeColor = primaryTextColor;
        openSshButton.Location = new Point(1, 1);
        openSshButton.Name = "openSshButton";
        openSshButton.Size = new Size(232, 30);
        openSshButton.TabIndex = 2;
        openSshButton.UseVisualStyleBackColor = false;
        //
        // projectLinkLabel
        //
        projectLinkLabel.AutoSize = true;
        projectLinkLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
        projectLinkLabel.LinkColor = cyanColor;
        projectLinkLabel.Location = new Point(20, 470);
        projectLinkLabel.Name = "projectLinkLabel";
        projectLinkLabel.Size = new Size(103, 15);
        projectLinkLabel.TabIndex = 8;
        projectLinkLabel.TabStop = true;
        projectLinkLabel.Tag = "https://github.com/2xiangbo/sshkey";
        projectLinkLabel.Text = "2xiangbo/sshkey";
        //
        // xxCodexLinkLabel
        //
        xxCodexLinkLabel.AutoSize = true;
        xxCodexLinkLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
        xxCodexLinkLabel.LinkColor = cyanColor;
        xxCodexLinkLabel.Location = new Point(145, 470);
        xxCodexLinkLabel.Name = "xxCodexLinkLabel";
        xxCodexLinkLabel.Size = new Size(54, 15);
        xxCodexLinkLabel.TabIndex = 9;
        xxCodexLinkLabel.TabStop = true;
        xxCodexLinkLabel.Tag = "https://xxcodex.com";
        xxCodexLinkLabel.Text = "XXCodex";
        //
        // Form1
        //
        AcceptButton = generateButton;
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.FromArgb(11, 17, 24);
        ClientSize = new Size(680, 520);
        Controls.Add(xxCodexLinkLabel);
        Controls.Add(projectLinkLabel);
        Controls.Add(languageComboBox);
        Controls.Add(generationHistoryButton);
        Controls.Add(browsePrivateKeyPathButton);
        Controls.Add(generateButton);
        Controls.Add(connectionDetailsTextBox);
        Controls.Add(statusInputPanel);
        Controls.Add(privateKeyPathInputPanel);
        Controls.Add(passwordInputPanel);
        Controls.Add(usernameInputPanel);
        Controls.Add(openSshInputPanel);
        Controls.Add(portInputPanel);
        Controls.Add(hostInputPanel);
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
        Text = "SSHKEY   //   SSH\u5bc6\u94a5\u8bbe\u7f6e";
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
    private Panel hostInputPanel;
    private Panel portInputPanel;
    private Panel openSshInputPanel;
    private Panel usernameInputPanel;
    private Panel passwordInputPanel;
    private Panel privateKeyPathInputPanel;
    private Panel statusInputPanel;
    private Button generateButton;
    private Button browsePrivateKeyPathButton;
    private Button generationHistoryButton;
    private ComboBox languageComboBox;
    private Button openSshButton;
    private LinkLabel projectLinkLabel;
    private LinkLabel xxCodexLinkLabel;
}
