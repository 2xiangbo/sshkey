# SSH Command Output, History IP, and Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the main result text with a ready-to-paste SSH command, add host context to history rows, and tighten the main form layout.

**Architecture:** Keep command construction in the domain formatting helper, pass the successful host into the backward-compatible history record, and adjust only the existing WinForms controls/coordinates. The history detail textbox and copy action continue to use the persisted connection string unchanged.

**Tech Stack:** .NET 8 WinForms, C#, xUnit.

## Global Constraints

- Only successful setups are recorded.
- The persisted result must be directly copyable as an SSH command.
- The history host field is optional for compatibility with existing JSON files.
- Do not persist passwords or private-key contents.

---

### Task 1: Command formatting

**Files:**
- Modify: `src/SshKeySetupTool/Domain/CodexConnectionDetails.cs`
- Test: `tests/SshKeySetupTool.Tests/Domain/CodexConnectionDetailsTests.cs`

- [x] Add a failing test asserting `Format` returns `ssh -p 31121 -i "C:\\Users\\Administrator\\.ssh\\38.76.198.139\\_id_ed25519" root@38.76.198.139`.
- [x] Run the focused test and confirm the old labeled output fails the assertion.
- [x] Replace the formatter's multi-line result with the quoted one-line command.
- [x] Run the focused test and confirm it passes.

### Task 2: Host-aware generation history

**Files:**
- Modify: `src/SshKeySetupTool/History/GenerationHistoryEntry.cs`
- Modify: `src/SshKeySetupTool/Form1.cs`
- Modify: `src/SshKeySetupTool/Presentation/GenerationHistoryForm.cs`
- Test: `tests/SshKeySetupTool.Tests/Presentation/GenerationHistoryFormTests.cs`
- Test: `tests/SshKeySetupTool.Tests/Presentation/FormLifecycleTests.cs`

- [x] Add failing tests for a new entry displaying completion time followed by its host and for a legacy entry without a host remaining readable.
- [x] Run the focused history tests and confirm the new display expectation fails.
- [x] Add nullable `Host` to `GenerationHistoryEntry`, pass `request.Host` from `RecordSuccessfulConnection`, and include it in `HistoryListItem.DisplayText` when present.
- [x] Keep the selected detail text equal to the persisted command and retain the existing copy button behavior.
- [x] Run the focused history/lifecycle tests and confirm they pass.

### Task 3: Compact main form spacing

**Files:**
- Modify: `src/SshKeySetupTool/Form1.Designer.cs`
- Test: `tests/SshKeySetupTool.Tests/Presentation/FormLayoutTests.cs`

- [x] Add a failing layout assertion that the output section begins closer to the private-key row while the output remains below the status panel.
- [x] Run the focused layout test and confirm the current coordinates fail the new spacing expectation.
- [x] Move the status panel, connection label, and output textbox upward by the smallest consistent amount; keep controls inside the 680px-wide client area and preserve multiline status height.
- [x] Run the focused layout test and confirm it passes.

### Task 4: Full verification

**Files:**
- No additional files.

- [x] Run `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --no-restore`.
- [x] Record the one environment-only failure caused by the unavailable `python:3.12-slim` Docker image.
- [ ] Review the diff and commit the implementation with a focused message.
