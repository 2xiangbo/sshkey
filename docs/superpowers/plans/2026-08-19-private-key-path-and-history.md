# Successful Connection History Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist only successful SSH setup connection details and allow users to copy any prior generated result from local history.

**Architecture:** `Form1` formats the same text it already displays and copies on a successful setup, then appends that text and a UTC timestamp to a JSON store. A modal history form reads newest-first entries, displays the selected text, and copies it exactly. Non-success outcomes never reach the append operation.

**Tech Stack:** .NET 8, C# records, `System.Text.Json`, WinForms, xUnit.

## Global Constraints

- Store history under `Environment.SpecialFolder.LocalApplicationData` for the current user.
- Persist only UTC completion time and the existing formatted connection-details text.
- Never persist passwords, private-key bytes, public-key bytes, process output, or exception details.
- Append only after a successful result produces connection details; failures, cancellations, and exceptions create no record.
- Keep Chinese and English UI behavior and the existing SSH setup flow.

---

### Task 1: Add a successful-connection JSON store

**Files:**
- Create: `src/SshKeySetupTool/History/GenerationHistoryEntry.cs`
- Create: `src/SshKeySetupTool/History/IGenerationHistoryStore.cs`
- Create: `src/SshKeySetupTool/History/JsonGenerationHistoryStore.cs`
- Create: `tests/SshKeySetupTool.Tests/History/JsonGenerationHistoryStoreTests.cs`

**Interfaces:**
- Produces `GenerationHistoryEntry(DateTimeOffset CompletedAtUtc, string ConnectionDetails)`.
- Produces `IGenerationHistoryStore.Read()`, `Append(GenerationHistoryEntry entry)`, and `Clear()`.

- [ ] **Step 1: Write the failing store tests**

```csharp
[Fact]
public void Append_Read_ReturnsNewestConnectionDetailsFirst()
{
    var store = new JsonGenerationHistoryStore(Path.Combine(_temporaryDirectory, "history.json"));
    store.Append(new(DateTimeOffset.Parse("2026-08-19T08:00:00Z"), "old details"));
    store.Append(new(DateTimeOffset.Parse("2026-08-19T09:00:00Z"), "new details"));

    Assert.Equal(["new details", "old details"], store.Read().Select(entry => entry.ConnectionDetails));
}

[Fact]
public void Clear_RemovesAllRecordedSuccessfulConnections()
{
    var store = new JsonGenerationHistoryStore(Path.Combine(_temporaryDirectory, "history.json"));
    store.Append(new(DateTimeOffset.UtcNow, "connection details"));

    store.Clear();

    Assert.Empty(store.Read());
}
```

- [ ] **Step 2: Verify the tests fail because the history API is absent**

Run: `dotnet test .\\tests\\SshKeySetupTool.Tests\\SshKeySetupTool.Tests.csproj -c Release --filter FullyQualifiedName~JsonGenerationHistoryStoreTests`

- [ ] **Step 3: Implement the minimum store**

Create the record and interface. Implement an exact-path constructor for tests and `CreateDefault()` resolving `%LocalAppData%\\SSHKEY\\generation-history.json`. Return an empty collection for missing or malformed JSON, sort newest first, retain the latest 100 entries after append, and delete the file only when clearing an existing file.

- [ ] **Step 4: Verify store tests pass**

Run: `dotnet test .\\tests\\SshKeySetupTool.Tests\\SshKeySetupTool.Tests.csproj -c Release --filter FullyQualifiedName~JsonGenerationHistoryStoreTests`

### Task 2: Add a localized, copyable history dialog

**Files:**
- Modify: `src/SshKeySetupTool/Presentation/UiLanguage.cs`
- Create: `src/SshKeySetupTool/Presentation/GenerationHistoryForm.cs`
- Create: `tests/SshKeySetupTool.Tests/Presentation/GenerationHistoryFormTests.cs`

**Interfaces:**
- Consumes `IGenerationHistoryStore` and `UiLanguage` in `GenerationHistoryForm`.
- Adds localized history, copy, clear, empty-state, time, and confirmation labels to `UiText`.

- [ ] **Step 1: Write failing dialog tests**

```csharp
[Fact]
public void HistoryForm_UsesChineseCopyButtonAndShowsSelectedConnectionDetails()
{
    var store = new InMemoryHistoryStore([
        new(DateTimeOffset.Parse("2026-08-19T09:00:00Z"), "服务器地址：example.com")]);

    using var form = new GenerationHistoryForm(store, UiLanguage.Chinese);

    Assert.Equal("生成历史", form.Text);
    Assert.Equal("复制", form.CopyButton.Text);
    Assert.Contains("example.com", form.SelectedConnectionDetails);
}
```

- [ ] **Step 2: Verify the dialog test fails because the dialog does not exist**

Run: `dotnet test .\\tests\\SshKeySetupTool.Tests\\SshKeySetupTool.Tests.csproj -c Release --filter FullyQualifiedName~GenerationHistoryFormTests`

- [ ] **Step 3: Implement the dialog**

Create a fixed-size modal form with a read-only list/grid of entries, a read-only multiline text area for the selected record, and Copy and Clear buttons. Expose internal test properties for the selected text and copy button. On selection, set the text area to `entry.ConnectionDetails`. The copy click uses `Clipboard.SetText` and does not mutate the history store. Clear refreshes the list and selected detail after localized confirmation.

- [ ] **Step 4: Verify dialog tests pass**

Run: `dotnet test .\\tests\\SshKeySetupTool.Tests\\SshKeySetupTool.Tests.csproj -c Release --filter FullyQualifiedName~GenerationHistoryFormTests`

### Task 3: Record only successful results and expose history

**Files:**
- Modify: `src/SshKeySetupTool/Form1.cs`
- Modify: `src/SshKeySetupTool/Form1.Designer.cs`
- Modify: `tests/SshKeySetupTool.Tests/Presentation/FormLayoutTests.cs`
- Modify: `tests/SshKeySetupTool.Tests/Presentation/FormLifecycleTests.cs`
- Modify: `README.md`

**Interfaces:**
- Extends `Form1` test construction with `IGenerationHistoryStore`.
- Produces `generationHistoryButton_Click` and success-only `RecordSuccessfulConnection` behavior.

- [ ] **Step 1: Write failing lifecycle and layout tests**

```csharp
[Fact]
public void SuccessfulSetup_PersistsTheSameConnectionDetailsShownInTheForm()
{
    RunInSta(() =>
    {
        var store = new InMemoryHistoryStore();
        using var form = new Form1(new SuccessfulSetupService(), new InstalledOpenSshManager(), store);
        PopulateRequiredFields(form);
        ClickGenerateAndPump(form);

        var shownDetails = Find<TextBox>(form, "connectionDetailsTextBox").Text;
        Assert.Equal(shownDetails, Assert.Single(store.Read()).ConnectionDetails);
    });
}

[Fact]
public void FailedSetup_DoesNotPersistHistory()
{
    RunInSta(() =>
    {
        var store = new InMemoryHistoryStore();
        using var form = new Form1(new FailedSetupService(), new InstalledOpenSshManager(), store);
        PopulateRequiredFields(form);
        ClickGenerateAndPump(form);

        Assert.Empty(store.Read());
    });
}
```

Also assert `generationHistoryButton` is present inside the existing client area.

- [ ] **Step 2: Verify lifecycle and layout tests fail**

Run: `dotnet test .\\tests\\SshKeySetupTool.Tests\\SshKeySetupTool.Tests.csproj -c Release --filter "FullyQualifiedName~FormLayoutTests|FullyQualifiedName~FormLifecycleTests"`

- [ ] **Step 3: Implement success-only form integration**

Create the default history store in the production constructor and accept an injected store through the internal constructor. In the existing `result.Succeeded` branch, after assigning `connectionDetailsTextBox.Text`, append `new GenerationHistoryEntry(DateTimeOffset.UtcNow, connectionDetails)` within a non-throwing helper. Do not call the helper in returned-failure, cancellation, or exception branches. Add the history button and modal click handler, then localize its label.

- [ ] **Step 4: Update README**

Document in English and Chinese that history stores only successful connection details, and that users can select a prior entry and copy its original generated text.

- [ ] **Step 5: Verify focused tests pass**

Run: `dotnet test .\\tests\\SshKeySetupTool.Tests\\SshKeySetupTool.Tests.csproj -c Release --filter "FullyQualifiedName~FormLayoutTests|FullyQualifiedName~FormLifecycleTests|FullyQualifiedName~GenerationHistoryFormTests|FullyQualifiedName~JsonGenerationHistoryStoreTests"`

- [ ] **Step 6: Complete verification**

Run: `dotnet test .\\SshKeySetupTool.sln -c Release; dotnet build .\\SshKeySetupTool.sln -c Release --no-restore; git diff --check`

Expected: all locally runnable tests pass; the Docker-backed shell-matrix test may remain unavailable until its required `python:3.12-slim` image is installed.
