using SshKeySetupTool.History;
using System.Runtime.InteropServices;

namespace SshKeySetupTool.Presentation;

internal sealed class GenerationHistoryForm : Form
{
    private readonly IGenerationHistoryStore _historyStore;
    private readonly UiText _text;
    private readonly ListBox _historyListBox;
    private readonly TextBox _connectionDetailsTextBox;
    private readonly Label _emptyLabel;

    internal GenerationHistoryForm(IGenerationHistoryStore historyStore, UiLanguage language)
    {
        _historyStore = historyStore ?? throw new ArgumentNullException(nameof(historyStore));
        _text = UiTextCatalog.For(language);

        Text = _text.GenerationHistory;
        ClientSize = new Size(680, 500);
        MinimumSize = Size;
        MaximumSize = Size;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(11, 17, 24);
        ForeColor = Color.FromArgb(233, 245, 250);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        _historyListBox = new ListBox
        {
            Name = "historyListBox",
            Location = new Point(16, 16),
            Size = new Size(648, 150),
            BackColor = Color.FromArgb(14, 24, 34),
            ForeColor = ForeColor,
            BorderStyle = BorderStyle.FixedSingle,
            DisplayMember = nameof(HistoryListItem.DisplayText)
        };
        _historyListBox.SelectedIndexChanged += historyListBox_SelectedIndexChanged;

        _connectionDetailsTextBox = new TextBox
        {
            Name = "historyConnectionDetailsTextBox",
            Location = new Point(16, 182),
            Size = new Size(648, 246),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(14, 24, 34),
            ForeColor = ForeColor,
            BorderStyle = BorderStyle.FixedSingle
        };

        _emptyLabel = new Label
        {
            Name = "historyEmptyLabel",
            Location = new Point(16, 182),
            Size = new Size(648, 32),
            Text = _text.HistoryEmpty,
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false
        };

        CopyButton = new Button
        {
            Name = "copyHistoryEntryButton",
            Text = _text.CopyHistoryEntry,
            Location = new Point(468, 444),
            Size = new Size(94, 36),
            Enabled = false
        };
        CopyButton.Click += copyHistoryEntryButton_Click;

        var clearButton = new Button
        {
            Name = "clearHistoryButton",
            Text = _text.ClearHistory,
            Location = new Point(570, 444),
            Size = new Size(94, 36)
        };
        clearButton.Click += clearHistoryButton_Click;

        Controls.Add(_historyListBox);
        Controls.Add(_connectionDetailsTextBox);
        Controls.Add(_emptyLabel);
        Controls.Add(CopyButton);
        Controls.Add(clearButton);

        LoadHistory();
    }

    internal Button CopyButton { get; }

    internal string SelectedConnectionDetails => _connectionDetailsTextBox.Text;

    private void LoadHistory()
    {
        _historyListBox.BeginUpdate();
        try
        {
            _historyListBox.Items.Clear();
            foreach (var entry in _historyStore.Read())
            {
                _historyListBox.Items.Add(new HistoryListItem(
                    entry,
                    _text.HistoryCompletedAt,
                    _text.HistoryHost));
            }
        }
        finally
        {
            _historyListBox.EndUpdate();
        }

        var hasEntries = _historyListBox.Items.Count > 0;
        _historyListBox.Visible = hasEntries;
        _connectionDetailsTextBox.Visible = hasEntries;
        _emptyLabel.Visible = !hasEntries;
        CopyButton.Enabled = hasEntries;
        _connectionDetailsTextBox.Clear();
        if (hasEntries)
        {
            _historyListBox.SelectedIndex = 0;
        }
    }

    private void historyListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _connectionDetailsTextBox.Text = _historyListBox.SelectedItem is HistoryListItem item
            ? item.Entry.ConnectionDetails
            : string.Empty;
        CopyButton.Enabled = _connectionDetailsTextBox.TextLength > 0;
    }

    private void copyHistoryEntryButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_connectionDetailsTextBox.Text))
        {
            return;
        }

        try
        {
            Clipboard.SetText(_connectionDetailsTextBox.Text);
        }
        catch (ExternalException)
        {
        }
    }

    private void clearHistoryButton_Click(object? sender, EventArgs e)
    {
        if (MessageBox.Show(
                this,
                _text.ClearHistoryConfirmation,
                _text.ClearHistoryTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        _historyStore.Clear();
        LoadHistory();
    }

    private sealed record HistoryListItem(
        GenerationHistoryEntry Entry,
        string CompletedAtLabel,
        string HostLabel)
    {
        public string DisplayText => string.IsNullOrWhiteSpace(Entry.Host)
            ? $"{CompletedAtLabel}: {Entry.CompletedAtUtc.LocalDateTime:g}"
            : $"{CompletedAtLabel}: {Entry.CompletedAtUtc.LocalDateTime:g}    {HostLabel}: {Entry.Host}";
    }
}
