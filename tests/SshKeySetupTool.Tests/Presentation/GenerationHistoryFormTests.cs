using SshKeySetupTool.History;
using SshKeySetupTool.Presentation;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;

namespace SshKeySetupTool.Tests.Presentation;

public sealed class GenerationHistoryFormTests
{
    [Fact]
    public void HistoryForm_UsesChineseCopyButtonAndShowsSelectedConnectionDetails()
    {
        RunInSta(() =>
        {
            const string connectionDetails = "ssh -p 22 -i \"C:\\keys\\id_ed25519\" root@example.com";
            var store = new InMemoryHistoryStore([
                new GenerationHistoryEntry(DateTimeOffset.Parse("2026-08-19T09:00:00Z"), connectionDetails, "example.com")]);

            using var form = new GenerationHistoryForm(store, UiLanguage.Chinese);

            Assert.Equal("生成历史", form.Text);
            Assert.Equal("复制", form.CopyButton.Text);
            Assert.Equal(connectionDetails, form.SelectedConnectionDetails);
            var historyList = Assert.IsType<ListBox>(form.Controls["historyListBox"]);
            Assert.Contains("服务器: example.com", historyList.GetItemText(historyList.Items[0]));
        });
    }

    [Fact]
    public void HistoryForm_KeepsLegacyEntriesWithoutHostReadable()
    {
        RunInSta(() =>
        {
            var store = new InMemoryHistoryStore([
                new GenerationHistoryEntry(
                    DateTimeOffset.Parse("2026-08-19T09:00:00Z"),
                    "ssh -p 22 -i \"C:\\keys\\id_ed25519\" root@example.com")]);

            using var form = new GenerationHistoryForm(store, UiLanguage.Chinese);

            var historyList = Assert.IsType<ListBox>(form.Controls["historyListBox"]);
            Assert.Contains("生成时间:", historyList.GetItemText(historyList.Items[0]));
            Assert.DoesNotContain("    ", historyList.GetItemText(historyList.Items[0]));
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
        private readonly List<GenerationHistoryEntry> _entries;

        public InMemoryHistoryStore(IEnumerable<GenerationHistoryEntry> entries)
        {
            _entries = entries.ToList();
        }

        public IReadOnlyList<GenerationHistoryEntry> Read() => _entries;

        public void Append(GenerationHistoryEntry entry) => _entries.Add(entry);

        public void Clear() => _entries.Clear();
    }
}
