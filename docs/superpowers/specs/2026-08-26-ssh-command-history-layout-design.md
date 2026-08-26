# SSH Command Output, History IP, and Layout Design

## Goal

After a successful setup, show one directly executable SSH command in the main output box, show the server host beside each history completion time, and reduce the vertical gap before the output section.

## Behavior

- Build the command from the successful request and generated private-key path: `ssh -p {port} -i "{privateKeyPath}" {username}@{host}`.
- Replace the existing five-line Codex details text in the main output box with that command.
- Persist the exact command in successful-generation history so the existing history copy action copies a ready-to-run command.
- Persist the request host as an optional history field. New history rows display completion time followed by the host; old entries without the field remain readable and display their existing time only.
- Move the status/output section upward while preserving status readability and the output box's ability to display the complete command.

## Compatibility and safety

History JSON remains backward compatible by making the host field optional. Passwords and key material are never persisted. Command paths remain quoted so Windows paths with spaces can be pasted safely.

## Verification

- Unit-test command formatting with port, host, username, and a Windows path.
- Unit-test history row formatting for new and legacy entries and exact copy text.
- Update layout assertions for the reduced gap and command output sizing.
- Run the focused test classes and the full .NET test project, documenting any environment-only Docker failure.
