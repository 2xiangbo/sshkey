# Automatic Server SSH Configuration Design

## Goal

When the user starts SSHKEY setup and the target server has password access but
does not accept public-key authentication, guide a root login through a single
confirmation dialog, enable public-key authentication safely, and finish the
existing password-to-key setup without requiring manual server commands.

## Scope and boundaries

- Automatic server configuration is available only when the requested SSH
  username is exactly `root`.
- A non-root account may continue through the existing flow when public-key
  authentication is already enabled. If it is disabled, the operation stops
  with an actionable message and makes no configuration change.
- The feature enables only `PubkeyAuthentication yes`. It does not install an
  SSH server package, change `PermitRootLogin`, disable password login, add
  `sudo` support, or alter SELinux policy.
- Passwords remain operation-scoped. They are never persisted, placed in
  command-line arguments, or included in errors and logs.
- The existing host-fingerprint confirmation and per-operation host-key pin
  remain mandatory.

## User flow

1. Validate the form and create the local Ed25519 key material using the
   existing no-overwrite behavior.
2. Discover and show the server host key. Stop if the user does not approve
   the fingerprint.
3. Open a password-authenticated, host-pinned SSH session as the requested
   account and inspect the effective `sshd` configuration. The probe reads the
   `pubkeyauthentication` value from `sshd -T` and reports whether it is
   enabled, disabled, or cannot be inspected.
4. If public-key authentication is enabled, continue without a prompt.
5. If it is disabled and the account is `root`, show a localized confirmation
   dialog. The dialog names the host, reports `PubkeyAuthentication no`, and
   says that SSHKEY will create or update a managed configuration entry, run
   `sshd -t`, verify the effective setting, reload SSH, and automatically undo
   the change if the rest of setup fails. The default button is Cancel.
6. After confirmation, apply the configuration transaction. Only after the
   transaction is active does the client install the public key and perform
   private-key verification.
7. On success, commit the configuration transaction, remove its temporary
   backup, and show the existing Codex connection details.
8. On cancellation or any failure after configuration was applied, roll back
   the exact configuration change, validate the restored configuration, and
   reload SSH before returning the original failure. If rollback fails, retain
   the backup and report that manual recovery may be required.

The existing password session behavior is preserved. A reload is used instead
of a service restart, so the current administrative session is not deliberately
terminated.

## Configuration strategy

The client uses a two-stage, reversible strategy.

### Managed drop-in

When `/etc/ssh/sshd_config.d` is available, create
`/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf` with an explicit marker and
the single directive:

```text
# Managed by SSHKEY. Do not edit while setup is running.
PubkeyAuthentication yes
```

The file is created with owner `root:root` and mode `600`. An existing file at
that path is never overwritten unless it contains the exact SSHKEY marker. A
recognized existing file is copied to the transaction backup before it is
replaced; an unrecognized file is treated as a conflict and the main-file
strategy is used instead.

After writing the fragment, run `sshd -t`, then `sshd -T`, and require the
effective output to contain `pubkeyauthentication yes`. If the fragment is
not included or is overridden, restore the recognized prior file when one
existed, otherwise remove the new fragment, and then continue with the
fallback.

### Main-file fallback

If the drop-in is unavailable or ineffective, copy the active
`/etc/ssh/sshd_config` to a uniquely named backup beside it, then prepend the
same marked directive to the active file. The original file metadata is
preserved. The modified file must pass `sshd -t` and the effective check before
SSHKEY reloads the service.

The applied change is represented by an opaque transaction record containing
the strategy, active path, backup path, and whether the active path existed
before the transaction. Commit deletes only the backup owned by that
transaction. Rollback removes a newly created drop-in or restores the exact
prior file from its backup, then repeats syntax validation and reload.

## Remote command and service behavior

- Resolve `sshd` from the standard command path (`command -v sshd`, with the
  common `/usr/sbin/sshd` fallback) and fail without mutation if it cannot be
  found.
- Resolve the reload operation through the active service manager, preferring
  `systemctl reload sshd`, then `systemctl reload ssh`, and finally the
  platform's `service` equivalent. A missing or unsuccessful reload is a
  failed transaction.
- All paths, marker values, and generated transaction identifiers are shell
  quoted by the command builder. User-provided host, username, password, and
  public-key text must not be interpolated into configuration paths.
- Inspection, apply, commit, and rollback are separate `ISshSetupClient`
  operations. Each uses the approved host key and the existing password
  fallback, and each returns structured output instead of making the service
  parse human-facing error strings.
- The setup service wraps apply/install/verify/commit in `try/finally`. A
  cancellation token is honored between remote operations. Rollback uses a
  separate bounded cleanup token rather than the already-canceled operation
  token, so cancellation after apply still attempts restoration.

## Code boundaries

- Add a server-configuration status and transaction model under the domain or
  SSH boundary.
- Add a focused Linux SSH configuration command builder for probe, drop-in,
  fallback, commit, and rollback commands.
- Extend `ISshSetupClient` and `WindowsOpenSshSetupClient` with structured
  inspection and transaction operations while retaining host-key pinning and
  password redaction.
- Extend `KeySetupService` with an injected configuration-consent callback,
  `SetupPhase` progress reporting, and the transaction orchestration described
  above. The service owns the root-only policy; the SSH client owns remote
  command execution.
- Extend `Form1` with a localized consent callback and phase-specific status
  updates supplied through `IProgress<SetupPhase>`. Add the corresponding
  Chinese and English strings to `UiTextCatalog`; do not put server command
  details in the status bar.

## Error handling

The user-visible error must distinguish these cases:

- server configuration could not be inspected;
- automatic repair is unavailable for a non-root account;
- the user declined the repair;
- configuration syntax/effective-check/reload failed;
- public-key installation failed;
- private-key verification failed;
- configuration rollback failed after another error.

The last case is highest priority in the UI and includes the preserved remote
backup path. The original password is redacted before any exception reaches the
form.

## Testing and acceptance

Unit tests must cover:

- effective configuration parsing for `yes`, `no`, and malformed output;
- safe command construction and shell quoting;
- drop-in success, drop-in ineffectiveness followed by fallback, and conflict
  handling;
- syntax-check, effective-check, and reload failures with rollback;
- commit cleanup and rollback restoration;
- `KeySetupService` ordering for ready, repaired, declined, non-root, canceled,
  verification-failed, and rollback-failed paths;
- localized consent copy and phase status rendering;
- password redaction and host-key pinning on every remote operation.

Before release, run the complete .NET test suite and publish build. Perform one
end-to-end run against a disposable Linux SSH host with
`PubkeyAuthentication no`, then repeat with it already enabled. Confirm that a
declined prompt leaves the configuration unchanged and that a forced private
key verification failure restores the original configuration. If no disposable
Linux host is available, record that integration verification as outstanding
instead of treating unit-test success as proof of server behavior.

## Out of scope

Installing `openssh-server`, changing root-login policy, disabling password
authentication, configuring `sudo`, fixing SELinux labels, managing multiple
server profiles, and editing arbitrary SSH directives are separate features.
