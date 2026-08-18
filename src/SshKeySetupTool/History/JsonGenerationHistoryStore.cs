using System.Text.Json;

namespace SshKeySetupTool.History;

public sealed class JsonGenerationHistoryStore : IGenerationHistoryStore
{
    private const int MaximumEntryCount = 100;
    private readonly string _historyPath;

    public JsonGenerationHistoryStore(string historyPath)
    {
        _historyPath = historyPath ?? throw new ArgumentNullException(nameof(historyPath));
    }

    public static JsonGenerationHistoryStore CreateDefault()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SSHKEY");
        return new JsonGenerationHistoryStore(Path.Combine(directory, "generation-history.json"));
    }

    public IReadOnlyList<GenerationHistoryEntry> Read()
    {
        if (!File.Exists(_historyPath))
        {
            return [];
        }

        try
        {
            var entries = JsonSerializer.Deserialize<List<GenerationHistoryEntry>>(
                File.ReadAllText(_historyPath));
            return (entries ?? [])
                .OrderByDescending(entry => entry.CompletedAtUtc)
                .Take(MaximumEntryCount)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
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

        var entries = Read().Append(entry)
            .OrderByDescending(item => item.CompletedAtUtc)
            .Take(MaximumEntryCount)
            .ToArray();
        var directory = Path.GetDirectoryName(_historyPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_historyPath, JsonSerializer.Serialize(entries));
    }

    public bool Clear()
    {
        try
        {
            if (File.Exists(_historyPath))
            {
                File.Delete(_historyPath);
            }

            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
