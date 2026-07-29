# SSHKEY bilingual UI and OpenSSH readiness design

## Goal

Rename the application’s visible identity to **SSHKEY**, provide immediate
Chinese/English UI switching (Chinese by default), add project links to the
footer, and make the first connection row show whether Windows OpenSSH Client
is ready. When it is missing, let the user install it through a clearly marked
administrator-approved action.

## Scope

This design changes the Windows Forms UI and its app-owned user-facing strings.
It does not rename the solution, namespaces, or executable file. Existing key
generation, server connection, and connection-details behavior remain intact.

## Localization model

A small, centralized UI text catalog will own all Chinese and English strings
used by `Form1`. It will expose a language value and a complete set of text
values so the form does not scatter `if (language == ...)` checks across event
handlers.

- Default language: Chinese (`中文`).
- Selector choices: `中文` and `EN`.
- Selecting a language updates the window caption, custom title bar, field
  labels, button captions, current UI-owned status text, confirmation dialog,
  and OpenSSH states without restarting.
- Chinese title: `SSHKEY   //   SSH密钥设置`.
- English title: `SSHKEY   //   SSH Key Setup`.
- Existing service messages that originate outside the form remain unchanged;
  form-owned wrapper messages and validation presentation use the selected
  language.

## Form layout

The form keeps its current compact width and dark visual style.

### First row

The first input row is repartitioned inside the existing content width:

1. Server IP/host field, reduced to approximately 244 pixels.
2. Port field, reduced to approximately 72 pixels.
3. An approximately 296-pixel OpenSSH readiness area on the right.

The readiness area is a single stateful control:

- While checking: disabled `检测 OpenSSH…` / `Checking OpenSSH…`.
- Ready: disabled green `✓ OpenSSH 已安装` / `✓ OpenSSH installed`.
- Missing: enabled primary action `一键安装 OpenSSH` / `Install OpenSSH`.
- Installation failure: enabled error-state retry action that gives the user a
  concise localized failure explanation.

The app detects OpenSSH asynchronously when the form is shown, so it never
blocks initial rendering. It will reuse the application’s system `ssh.exe`
resolution rules. The action becomes available only after a missing result.

### Footer row

The existing generation button remains on the right. The empty space to its
left is used, in this order, for:

1. A `2xiangbo/sshkey` `LinkLabel` opening
   `https://github.com/2xiangbo/sshkey`.
2. An `XXCodex` `LinkLabel` opening `https://xxcode.com`.
3. The narrow language selector immediately to the left of the primary button.
4. The existing generation button.

External URLs are opened through the shell with `UseShellExecute = true`; the
UI never embeds credentials or executes downloaded content.

## OpenSSH installation

The install action uses the Windows optional-feature command for
`OpenSSH.Client~~~~0.0.1.0`. It launches a separate elevated PowerShell process
with `Verb = "runas"`, so Windows owns the UAC consent prompt. The form waits
for the installer process to exit, then performs a fresh detection. UAC
cancellation, nonzero installer exit codes, and unexpected process failures
leave the action enabled and render a localized, retryable failure state.

The setup workflow itself remains responsible for reporting errors if OpenSSH
is removed after the readiness check.

## Testing

Tests will cover:

- Chinese is selected by default and the displayed Chinese title is exact.
- Switching to `EN` updates the title, core labels, action caption, and
  language-sensitive UI strings.
- The links expose the exact GitHub and XXCodex targets without opening a
  browser during tests.
- The compact first-row layout keeps the host, port, and OpenSSH control within
  the form and without overlap.
- A focused OpenSSH readiness service verifies executable detection and the
  elevated installer command construction, including missing and failed states.

## Acceptance criteria

- A fresh launch is Chinese and displays `SSHKEY   //   SSH密钥设置` in both
  title locations.
- `EN` changes the full app-owned interface immediately, without a restart.
- Both footer links are clickable and point exactly to their requested URLs.
- The first row visibly reports OpenSSH readiness; a missing client offers a
  one-click installation action that requests UAC approval.
- Existing key setup behavior and the full test suite remain passing.
