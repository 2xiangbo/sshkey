# SSH Key Setup Tool Design

## Goal

Deliver a self-contained Windows executable that configures password-authenticated
Linux SSH access for Codex. The user enters a host IP address, port, account name,
and password. The tool generates an Ed25519 keypair locally, installs its public
key in the remote account's `~/.ssh/authorized_keys`, and verifies key-based SSH
authentication.

The released executable must not require Python or a separately installed .NET
runtime.

## User Interface

The Windows desktop window contains these fields:

- Server IP address
- SSH port, defaulting to `22`
- SSH account name
- Password, masked in the UI
- Private-key save path, defaulting to `%USERPROFILE%\\.ssh\\id_ed25519`

The primary action is `Generate and deploy`. A progress area reports the active
step and a final result. The password is never shown in the progress area.

## Architecture

The application is a C# WinForms program published as a Windows x64,
self-contained, single-file executable. It uses SSH.NET for SSH authentication
and remote command execution, plus BouncyCastle to generate and serialize an
Ed25519 keypair in OpenSSH-compatible files.

The code is divided into three units:

- `SetupForm` owns input validation, control state, and progress presentation.
- `KeySetupService` runs the key generation, remote installation, and verification
  workflow without depending on UI controls.
- `SshClientFactory` creates password- and key-authenticated SSH clients, with
  host-key validation supplied by the UI's trusted-host service.

## Workflow

1. Validate the host, port, account, and private-key target path.
2. Establish an SSH connection using the supplied password.
3. Display the server host-key fingerprint on its first use and require the user
   to confirm it. Store approved fingerprints locally. Reject a fingerprint that
   changes later.
4. Create the local `.ssh` directory as needed and generate an Ed25519 private
   key plus a matching `.pub` file. Never overwrite an existing private-key path.
5. On the Linux host, create `~/.ssh` with mode `700`, then append the public key
   to `~/.ssh/authorized_keys` only when that exact key is not already present.
   Set `authorized_keys` mode to `600`.
6. Close the password-authenticated connection. Connect again with the generated
   private key and run a minimal command to prove key authentication works.
7. Show the private-key path to use as Codex's SSH identity file.

## Security and Failure Handling

- The password is held only in memory for the active operation. It is not written
  to disk, logs, settings, command lines, or error messages.
- Private keys are written with user-only Windows file permissions where possible.
- Public-key installation is idempotent and does not remove existing remote keys.
- A failed deployment leaves existing remote SSH configuration intact.
- The UI distinguishes invalid input, network failure, password rejection,
  insufficient remote permissions, host-key mismatch, local write failure, and
  key-authentication verification failure.
- A successful completion requires the final key-authentication connection. A
  public-key write alone is not reported as success.

## Testing and Verification

Unit tests cover validation, key target collision handling, public-key
deduplication, remote command composition, fingerprint trust decisions, and
workflow outcomes for password- and key-authentication success and failure.

Integration verification uses an SSH test host or container to confirm that a
fresh account can be configured and subsequently accessed with the generated
private key. Release verification publishes the self-contained executable and
starts it on Windows.

## Out of Scope

- Saving server passwords or creating reusable server profiles.
- Managing SSH keys already installed by other tools.
- Windows SSH servers.
- Changing the server's SSH daemon configuration.
