using SshKeySetupTool.History;

namespace SshKeySetupTool.Tests.History;

public sealed class JsonGenerationHistoryStoreTests : IDisposable
{
    private readonly string _temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Append_Read_ReturnsNewestEntriesFirst()
    {
        var store = new JsonGenerationHistoryStore(Path.Combine(_temporaryDirectory, "history.json"));
        store.Append(new(
            DateTimeOffset.Parse("2026-08-19T08:00:00Z"),
            "old.example",
            22,
            "root",
            @"D:\keys\old",
            GenerationHistoryOutcome.Succeeded));
        store.Append(new(
            DateTimeOffset.Parse("2026-08-19T09:00:00Z"),
            "new.example",
            2222,
            "admin",
            @"D:\keys\new",
            GenerationHistoryOutcome.Failed));

        Assert.Equal(["new.example", "old.example"], store.Read().Select(entry => entry.Host));
    }

    [Fact]
    public void Append_WritesNoSensitiveFields()
    {
        var historyPath = Path.Combine(_temporaryDirectory, "history.json");
        var store = new JsonGenerationHistoryStore(historyPath);
        store.Append(new(
            DateTimeOffset.UtcNow,
            "server.example",
            22,
            "root",
            @"D:\keys\id_ed25519_codex",
            GenerationHistoryOutcome.Succeeded));

        var json = File.ReadAllText(historyPath);

        Assert.DoesNotContain("Password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PrivateKeyMaterial", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionDetails", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Entry_ContainsNoArbitraryMessageField()
    {
        Assert.DoesNotContain(
            typeof(GenerationHistoryEntry).GetProperties(),
            property => string.Equals(property.Name, "Message", StringComparison.Ordinal));
    }

    [Fact]
    public void Clear_RemovesAllRecordedEntries()
    {
        var store = new JsonGenerationHistoryStore(Path.Combine(_temporaryDirectory, "history.json"));
        store.Append(new(
            DateTimeOffset.UtcNow,
            "server.example",
            22,
            "root",
            "key",
            GenerationHistoryOutcome.Succeeded));

        store.Clear();

        Assert.Empty(store.Read());
    }

    [Fact]
    public void Clear_ReturnsFalseWhenHistoryFileIsLocked()
    {
        var historyPath = Path.Combine(_temporaryDirectory, "history.json");
        var store = new JsonGenerationHistoryStore(historyPath);
        store.Append(new(
            DateTimeOffset.UtcNow,
            "server.example",
            22,
            "root",
            "key",
            GenerationHistoryOutcome.Succeeded));

        using var handle = File.Open(historyPath, FileMode.Open, FileAccess.Read, FileShare.None);

        Assert.False(store.Clear());
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }
}
