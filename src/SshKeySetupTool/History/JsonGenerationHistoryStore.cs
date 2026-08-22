using System.Text.Json;

namespace SshKeySetupTool.History;

public sealed class JsonGenerationHistoryStore : IGenerationHistoryStore
{
    private const int MaximumEntries = 100;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _historyFilePath;

    public JsonGenerationHistoryStore(string historyFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(historyFilePath);
        _historyFilePath = historyFilePath;
    }

    public static JsonGenerationHistoryStore CreateDefault()
    {
        var localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        return new JsonGenerationHistoryStore(Path.Combine(
            localApplicationData,
            "SSHKEY",
            "generation-history.json"));
    }

    public IReadOnlyList<GenerationHistoryEntry> Read()
    {
        try
        {
            if (!File.Exists(_historyFilePath))
            {
                return [];
            }

            var entries = JsonSerializer.Deserialize<List<GenerationHistoryEntry>>(
                File.ReadAllText(_historyFilePath),
                SerializerOptions);
            return (entries ?? [])
                .OrderByDescending(entry => entry.CompletedAtUtc)
                .ToArray();
        }
        catch (IOException)
        {
            return [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    public void Append(GenerationHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var entries = Read()
            .Append(entry)
            .OrderByDescending(historyEntry => historyEntry.CompletedAtUtc)
            .Take(MaximumEntries)
            .ToArray();
        var directory = Path.GetDirectoryName(_historyFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(
            _historyFilePath,
            JsonSerializer.Serialize(entries, SerializerOptions));
    }

    public void Clear()
    {
        if (File.Exists(_historyFilePath))
        {
            File.Delete(_historyFilePath);
        }
    }
}
