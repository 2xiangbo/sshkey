# Private Key Path Selection and Generation History Design

## Goal

Let users choose a directory other than the default private-key location and
review past SSH key generation attempts without exposing passwords or private
key material.

## User Experience

The private-key path row retains its editable path field and adds a localized
`Browse...` button. Selecting a folder sets the field to that folder plus the
standard suggested filename, `id_ed25519_codex`. The user can still edit the
result before generation.

On the credential row, the username input is reduced from half the form width
to a compact field. The password field remains beside it. A localized
`Generation history` button occupies the newly available right-hand area.

Selecting the history button opens a modal history window. It shows entries in
reverse chronological order with time, server, port, username, private-key
path, result, and a non-sensitive message. The window provides a localized
clear-history command with confirmation.

## Persistence and Privacy

The application stores history as JSON at a per-user path beneath
`Environment.SpecialFolder.LocalApplicationData`. The history store creates
the directory when needed and tolerates a missing history file. It writes an
entry after every completed setup attempt, including validation and unexpected
failures.

Each record contains only:

- UTC completion timestamp
- Server host and port
- Username
- Private-key destination path
- Success, cancelled, or failed outcome
- A non-sensitive status message

Passwords, private-key bytes, public-key bytes, connection details, and full
exception output are never persisted. The UI continues to clear the password
box after an attempt.

## Components

`GenerationHistoryEntry` is an immutable domain record. `IGenerationHistoryStore`
provides append, read, and clear operations. `JsonGenerationHistoryStore`
owns path resolution, serialization, malformed-file recovery, sorting, and a
bounded maximum entry count.

`Form1` owns only presentation behavior: it opens the folder picker, builds
history entries from completed runs, and opens the history dialog. The dialog
receives the store abstraction and renders localized table headings and result
labels.

`UiText` gains labels for browsing, history, history columns, result states,
empty history, and clear-history confirmation in Chinese and English.

## Error Handling

Cancelling the folder picker changes nothing. A history write or read failure
does not change the key-generation result; the form reports the persistence
error through the normal status surface where useful. A corrupt JSON file is
treated as no readable history so the main workflow remains available.

## Tests

Unit tests cover private-key path suggestion for a selected folder, JSON store
append/read ordering, bounded retention, clear behavior, and the exclusion of
password and key material. Presentation tests verify the revised credential
row, browse button, history button, and localized labels.
