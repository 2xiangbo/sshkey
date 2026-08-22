using SshKeySetupTool.History;

namespace SshKeySetupTool.Tests.History;

public sealed class JsonGenerationHistoryStoreTests : IDisposable
{
    private readonly string _temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        nameof(JsonGenerationHistoryStoreTests),
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Append_Read_ReturnsNewestConnectionDetailsFirstWithoutChangingText()
    {
        var store = new JsonGenerationHistoryStore(Path.Combine(_temporaryDirectory, "history.json"));
        const string oldDetails = "Server: old.example\r\nPrivate key: C:\\keys\\old";
        const string newDetails = "服务器地址：new.example\r\n私钥路径：C:\\keys\\new";

        store.Append(new GenerationHistoryEntry(
            DateTimeOffset.Parse("2026-08-19T08:00:00Z"),
            oldDetails));
        store.Append(new GenerationHistoryEntry(
            DateTimeOffset.Parse("2026-08-19T09:00:00Z"),
            newDetails));

        var entries = store.Read();

        Assert.Equal([newDetails, oldDetails], entries.Select(entry => entry.ConnectionDetails));
    }

    [Fact]
    public void Clear_RemovesAllRecordedSuccessfulConnections()
    {
        var store = new JsonGenerationHistoryStore(Path.Combine(_temporaryDirectory, "history.json"));
        store.Append(new GenerationHistoryEntry(DateTimeOffset.UtcNow, "connection details"));

        store.Clear();

        Assert.Empty(store.Read());
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }
}
