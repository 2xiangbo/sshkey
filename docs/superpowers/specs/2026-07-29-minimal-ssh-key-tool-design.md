# Minimal SSH Key Tool Design

## Goal

Ship one self-contained Windows executable for Codex SSH setup. The user enters a Linux server IP, SSH port, account, and password. One button generates a local Ed25519 keypair, installs its public key on that account, verifies key login, and shows the private-key path for Codex.

## Window

The window contains only IP, port (default `22`), account, password, private-key path (default `%USERPROFILE%\\.ssh\\id_ed25519`), a status line, and `Generate and write to server`.

## Operation

The button validates the fields, creates `id_ed25519` and `id_ed25519.pub` locally without overwriting an existing path, connects to Linux using the password, appends the public key to `~/.ssh/authorized_keys` when absent, then reconnects with the generated private key. Success displays the exact private-key path for Codex.

## Boundaries

The password is used only for this operation and is not saved. The tool has no server profiles, multi-server list, advanced settings, persistent host-trust database, concurrent setup support, or SSH daemon configuration.

## Verification

Automated tests cover input validation, key-file collision handling, and the password-to-key operation sequence with a fake SSH connection. Release verification publishes and starts the single-file Windows executable.
