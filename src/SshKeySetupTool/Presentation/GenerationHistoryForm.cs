using SshKeySetupTool.History;

namespace SshKeySetupTool.Presentation;

public sealed class GenerationHistoryForm : Form
{
    private readonly IGenerationHistoryStore _store;
    private readonly UiText _text;
    private readonly DataGridView _historyGrid;
    private readonly Label _emptyLabel;

    public GenerationHistoryForm(IGenerationHistoryStore store, UiLanguage language)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _text = UiTextCatalog.For(language);

        Text = _text.GenerationHistory;
        ClientSize = new Size(780, 360);
        MinimumSize = Size;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(11, 17, 24);

        _historyGrid = new DataGridView
        {
            Name = "historyGrid",
            Location = new Point(16, 16),
            Size = new Size(748, 276),
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.FromArgb(14, 24, 34),
            BorderStyle = BorderStyle.FixedSingle,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(16, 27, 38),
                ForeColor = Color.FromArgb(233, 245, 250)
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(14, 24, 34),
                ForeColor = Color.FromArgb(233, 245, 250),
                SelectionBackColor = Color.FromArgb(38, 55, 71)
            },
            EnableHeadersVisualStyles = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        AddColumns();

        _emptyLabel = new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(127, 149, 163),
            Location = new Point(16, 306),
            Text = _text.HistoryEmpty
        };

        var clearButton = new Button
        {
            AutoSize = true,
            BackColor = Color.FromArgb(38, 55, 71),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(233, 245, 250),
            Location = new Point(648, 304),
            Text = _text.ClearHistory
        };
        clearButton.FlatAppearance.BorderSize = 0;
        clearButton.Click += clearButton_Click;

        Controls.Add(_historyGrid);
        Controls.Add(_emptyLabel);
        Controls.Add(clearButton);
        RefreshHistory();
    }

    private void AddColumns()
    {
        _historyGrid.Columns.Add("time", _text.HistoryTime);
        _historyGrid.Columns.Add("host", _text.HistoryHost);
        _historyGrid.Columns.Add("port", _text.HistoryPort);
        _historyGrid.Columns.Add("username", _text.HistoryUsername);
        _historyGrid.Columns.Add("privateKeyPath", _text.HistoryPrivateKeyPath);
        _historyGrid.Columns.Add("result", _text.HistoryResult);
        _historyGrid.Columns.Add("message", _text.HistoryMessage);
    }

    private void RefreshHistory()
    {
        _historyGrid.Rows.Clear();
        foreach (var entry in _store.Read())
        {
            _historyGrid.Rows.Add(
                entry.CompletedAtUtc.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                entry.Host,
                entry.Port,
                entry.Username,
                entry.PrivateKeyPath,
                OutcomeText(entry.Outcome),
                entry.Message);
        }

        _emptyLabel.Visible = _historyGrid.Rows.Count == 0;
    }

    private string OutcomeText(GenerationHistoryOutcome outcome) => outcome switch
    {
        GenerationHistoryOutcome.Succeeded => _text.HistorySucceeded,
        GenerationHistoryOutcome.Cancelled => _text.HistoryCancelled,
        _ => _text.HistoryFailed
    };

    private void clearButton_Click(object? sender, EventArgs e)
    {
        if (MessageBox.Show(
                this,
                _text.ClearHistoryConfirmation,
                _text.GenerationHistory,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.OK)
        {
            return;
        }

        _store.Clear();
        RefreshHistory();
    }
}
