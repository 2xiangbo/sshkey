using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using SshKeySetupTool.History;
using SshKeySetupTool.Presentation;

namespace SshKeySetupTool.Tests.Presentation;

public sealed class GenerationHistoryFormTests
{
    [Fact]
    public void Form_UsesChineseTitleAndColumnHeaders()
    {
        RunInSta(() =>
        {
            var store = new InMemoryHistoryStore();
            store.Append(new(
                DateTimeOffset.UtcNow,
                "203.0.113.5",
                22,
                "root",
                @"D:\keys\id_ed25519_codex",
                GenerationHistoryOutcome.Succeeded,
                "Completed"));

            using var form = new GenerationHistoryForm(store, UiLanguage.Chinese);
            var grid = Assert.IsType<DataGridView>(
                Assert.Single(form.Controls.Find("historyGrid", searchAllChildren: true)));

            Assert.Equal("生成历史", form.Text);
            Assert.Contains("服务器", grid.Columns.Cast<DataGridViewColumn>()
                .Select(column => column.HeaderText));
            Assert.Contains("成功", grid.Rows.Cast<DataGridViewRow>()
                .Select(row => row.Cells[5].Value));
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

    private sealed class InMemoryHistoryStore : IGenerationHistoryStore
    {
        private readonly List<GenerationHistoryEntry> _entries = [];

        public IReadOnlyList<GenerationHistoryEntry> Read() => _entries;

        public void Append(GenerationHistoryEntry entry) => _entries.Add(entry);

        public void Clear() => _entries.Clear();
    }
}
