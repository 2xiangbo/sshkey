namespace SshKeySetupTool.History;

public interface IGenerationHistoryStore
{
    IReadOnlyList<GenerationHistoryEntry> Read();

    void Append(GenerationHistoryEntry entry);

    bool Clear();
}
