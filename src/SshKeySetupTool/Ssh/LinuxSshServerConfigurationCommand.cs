using SshKeySetupTool.Domain;

namespace SshKeySetupTool.Ssh;

public static class LinuxSshServerConfigurationCommand
{
    internal const string MainConfigurationPath = "/etc/ssh/sshd_config";
    internal const string ManagedDropInPath =
        "/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf";
    internal const string ManagedMarker =
        "# Managed by SSHKEY. Do not edit while setup is running.";

    public static string BuildProbe() => """
set -eu
sshd="$(command -v sshd 2>/dev/null || true)"
[ -n "$sshd" ] || [ ! -x /usr/sbin/sshd ] || sshd=/usr/sbin/sshd
[ -n "$sshd" ] || { printf '%s\n' 'SSHKEY_PROBE_ERROR sshd executable was not found'; exit 41; }
if ! probe_output="$("$sshd" -T 2>&1)"; then
  printf '%s\n' "SSHKEY_PROBE_ERROR sshd -T failed: $probe_output"
  exit 42
fi
pubkey_line="$(printf '%s\n' "$probe_output" | awk 'tolower($1) == "pubkeyauthentication" { print; exit }')"
if [ -z "$pubkey_line" ]; then
  printf '%s\n' "SSHKEY_PROBE_ERROR sshd -T returned no pubkeyauthentication value: $probe_output"
  exit 42
fi
printf '%s\n' "$pubkey_line"
""";

    public static string BuildApply(string operationId)
    {
        ValidateOperationId(operationId);

        return $$"""
set -eu
main_config='/etc/ssh/sshd_config'
managed_config='/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf'
marker='# Managed by SSHKEY. Do not edit while setup is running.'
operation_marker='# SSHKEY operation: {{operationId}}'
new_drop_in_backup_marker='# SSHKEY new drop-in: {{operationId}}'
backup='/etc/ssh/sshd_config.sshkey-setup-{{operationId}}.bak'
managed_backup='/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf.sshkey-setup-{{operationId}}.bak'
sshd="$(command -v sshd 2>/dev/null || true)"
[ -n "$sshd" ] || [ ! -x /usr/sbin/sshd ] || sshd=/usr/sbin/sshd
[ -n "$sshd" ] || { printf '%s\n' 'SSHKEY_ERROR sshd-not-found'; exit 41; }
is_enabled() {
  "$sshd" -T 2>/dev/null |
    awk 'tolower($1) == "pubkeyauthentication" { print tolower($2); exit }' |
    grep -qx yes
}
reload_sshd() {
  if command -v systemctl >/dev/null 2>&1 &&
     (systemctl reload sshd >/dev/null 2>&1 || systemctl reload ssh >/dev/null 2>&1); then
    return 0
  fi
  if command -v service >/dev/null 2>&1 &&
     (service sshd reload >/dev/null 2>&1 || service ssh reload >/dev/null 2>&1); then
    return 0
  fi
  return 1
}
drop_in_tmp=''
main_tmp=''
had_existing_drop_in=false
cleanup() {
  if [ -n "$drop_in_tmp" ]; then rm -f -- "$drop_in_tmp" || true; fi
  if [ -n "$main_tmp" ]; then rm -f -- "$main_tmp" || true; fi
}
restore_drop_in() {
  if [ "$had_existing_drop_in" = true ]; then
    cp -a -- "$managed_backup" "$managed_config"
  else
    rm -f -- "$managed_config"
  fi
}
restore_main() {
  cp -a -- "$backup" "$main_config"
}
restore_drop_in_and_reload() {
  restore_drop_in && "$sshd" -t 2>/dev/null && reload_sshd
}
restore_main_and_reload() {
  restore_main && "$sshd" -t 2>/dev/null && reload_sshd
}
trap cleanup EXIT

if [ -d '/etc/ssh/sshd_config.d' ]; then
  try_drop_in=false
  if [ ! -e "$managed_config" ]; then
    try_drop_in=true
  elif [ -f "$managed_config" ] && [ "$(sed -n '1p' "$managed_config" || true)" = "$marker" ]; then
    try_drop_in=true
    had_existing_drop_in=true
  fi

  if [ "$try_drop_in" = true ]; then
    if [ "$had_existing_drop_in" = true ]; then
      if ! cp -a -- "$managed_config" "$managed_backup"; then
        printf '%s\n' 'SSHKEY_ERROR backup-failed'
        exit 42
      fi
    elif ! printf '%s\n' "$new_drop_in_backup_marker" > "$managed_backup" ||
         ! chmod 600 "$managed_backup"; then
      printf '%s\n' 'SSHKEY_ERROR backup-failed'
      exit 42
    fi
    if ! drop_in_tmp="$(mktemp "${managed_config}.tmp.XXXXXX")"; then
      printf '%s\n' 'SSHKEY_ERROR temporary-file-failed'
      exit 42
    fi
    if ! cat > "$drop_in_tmp" <<EOF
$marker
$operation_marker
PubkeyAuthentication yes
EOF
    then
      printf '%s\n' 'SSHKEY_ERROR write-failed'
      exit 42
    fi
    if ! chown root:root "$drop_in_tmp" || ! chmod 600 "$drop_in_tmp" || ! mv -f -- "$drop_in_tmp" "$managed_config"; then
      restore_drop_in || true
      printf '%s\n' 'SSHKEY_ERROR write-failed'
      exit 42
    fi
    drop_in_tmp=''
    if "$sshd" -t 2>/dev/null && is_enabled && reload_sshd; then
      if [ "$had_existing_drop_in" = true ]; then
        printf '%s\n' 'SSHKEY_APPLIED drop-in-existing'
      else
        printf '%s\n' 'SSHKEY_APPLIED drop-in-new'
      fi
      exit 0
    fi
    if ! restore_drop_in_and_reload; then
      printf '%s\n' 'SSHKEY_ERROR rollback-failed'
      exit 43
    fi
    rm -f -- "$managed_backup"
  fi
fi

if ! cp -a -- "$main_config" "$backup"; then
  printf '%s\n' 'SSHKEY_ERROR backup-failed'
  exit 42
fi
if ! main_tmp="$(mktemp "${main_config}.tmp.XXXXXX")"; then
  printf '%s\n' 'SSHKEY_ERROR temporary-file-failed'
  exit 42
fi
if ! cat > "$main_tmp" <<EOF
$marker
PubkeyAuthentication yes
$(cat "$main_config")
EOF
then
  printf '%s\n' 'SSHKEY_ERROR write-failed'
  exit 42
fi
if ! cat "$main_tmp" > "$main_config"; then
  restore_main || true
  printf '%s\n' 'SSHKEY_ERROR write-failed'
  exit 42
fi
rm -f -- "$main_tmp"
main_tmp=''
if ! "$sshd" -t 2>/dev/null || ! is_enabled; then
  if ! restore_main_and_reload; then
    printf '%s\n' 'SSHKEY_ERROR rollback-failed'
    exit 43
  fi
  rm -f -- "$backup"
  printf '%s\n' 'SSHKEY_ERROR validation-failed'
  exit 42
fi
if ! reload_sshd; then
  if ! restore_main_and_reload; then
    printf '%s\n' 'SSHKEY_ERROR rollback-failed'
    exit 43
  fi
  rm -f -- "$backup"
  printf '%s\n' 'SSHKEY_ERROR reload-failed'
  exit 42
fi
printf '%s\n' 'SSHKEY_APPLIED main'
""";
    }

    public static SshServerConfigurationChange ParseApplyResult(
        string operationId,
        string output)
    {
        ValidateOperationId(operationId);
        ArgumentNullException.ThrowIfNull(output);

        var sentinels = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToArray();
        if (sentinels.Length != 1)
        {
            throw CreateApplyException();
        }

        return sentinels[0] switch
        {
            "SSHKEY_APPLIED drop-in-new" => new SshServerConfigurationChange(
                operationId,
                SshServerConfigurationStrategy.ManagedDropIn,
                false),
            "SSHKEY_APPLIED drop-in-existing" => new SshServerConfigurationChange(
                operationId,
                SshServerConfigurationStrategy.ManagedDropIn,
                true),
            "SSHKEY_APPLIED main" => new SshServerConfigurationChange(
                operationId,
                SshServerConfigurationStrategy.MainConfiguration,
                false),
            _ => throw CreateApplyException()
        };
    }

    public static string BuildCommit(SshServerConfigurationChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        ValidateOperationId(change.OperationId);
        ValidateStrategy(change.Strategy);
        var backupPath = change.Strategy == SshServerConfigurationStrategy.ManagedDropIn
            ? ManagedDropInPath + ".sshkey-setup-" + change.OperationId + ".bak"
            : MainConfigurationPath + ".sshkey-setup-" + change.OperationId + ".bak";
        return $"set -eu\nrm -f -- {ShellQuote(backupPath)}\n";
    }

    public static string BuildRecovery(string operationId)
    {
        ValidateOperationId(operationId);
        var mainBackupPath = MainConfigurationPath +
            ".sshkey-setup-" + operationId + ".bak";
        var managedBackupPath = ManagedDropInPath +
            ".sshkey-setup-" + operationId + ".bak";
        var operationMarker = "# SSHKEY operation: " + operationId;
        var newDropInBackupMarker = "# SSHKEY new drop-in: " + operationId;

        return $$"""
set -eu
main_config={{ShellQuote(MainConfigurationPath)}}
managed_config={{ShellQuote(ManagedDropInPath)}}
main_backup={{ShellQuote(mainBackupPath)}}
managed_backup={{ShellQuote(managedBackupPath)}}
marker={{ShellQuote(ManagedMarker)}}
operation_marker={{ShellQuote(operationMarker)}}
new_drop_in_backup_marker={{ShellQuote(newDropInBackupMarker)}}
sshd="$(command -v sshd 2>/dev/null || true)"
[ -n "$sshd" ] || [ ! -x /usr/sbin/sshd ] || sshd=/usr/sbin/sshd
[ -n "$sshd" ] || { printf '%s\n' 'SSHKEY_ERROR sshd-not-found'; exit 41; }
reload_sshd() {
  if command -v systemctl >/dev/null 2>&1 &&
     (systemctl reload sshd >/dev/null 2>&1 || systemctl reload ssh >/dev/null 2>&1); then
    return 0
  fi
  if command -v service >/dev/null 2>&1 &&
     (service sshd reload >/dev/null 2>&1 || service ssh reload >/dev/null 2>&1); then
    return 0
  fi
  return 1
}
recovered=false
if [ -f "$main_backup" ]; then
  cp -a -- "$main_backup" "$main_config"
  recovered=true
elif [ -f "$managed_backup" ]; then
  if [ "$(sed -n '1p' "$managed_backup" || true)" = "$new_drop_in_backup_marker" ]; then
    rm -f -- "$managed_config"
  else
    cp -a -- "$managed_backup" "$managed_config"
  fi
  recovered=true
elif [ -f "$managed_config" ] &&
     [ "$(sed -n '1p' "$managed_config" || true)" = "$marker" ] &&
     [ "$(sed -n '2p' "$managed_config" || true)" = "$operation_marker" ]; then
  rm -f -- "$managed_config"
  recovered=true
fi
if [ "$recovered" = false ]; then
  exit 0
fi
"$sshd" -t 2>/dev/null
reload_sshd
rm -f -- "$main_backup" "$managed_backup"
""";
    }

    public static string BuildRollback(SshServerConfigurationChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        ValidateOperationId(change.OperationId);
        ValidateStrategy(change.Strategy);
        var backupPath = change.Strategy == SshServerConfigurationStrategy.ManagedDropIn
            ? ManagedDropInPath + ".sshkey-setup-" + change.OperationId + ".bak"
            : MainConfigurationPath + ".sshkey-setup-" + change.OperationId + ".bak";
        var restore = change.Strategy == SshServerConfigurationStrategy.ManagedDropIn
            ? change.HadExistingManagedDropIn
                ? $"cp -a -- {ShellQuote(backupPath)} {ShellQuote(ManagedDropInPath)}"
                : $"rm -f -- {ShellQuote(ManagedDropInPath)}"
            : $"cp -a -- {ShellQuote(backupPath)} {ShellQuote(MainConfigurationPath)}";
        var quotedBackupPath = ShellQuote(backupPath);

        return $$"""
set -eu
sshd="$(command -v sshd 2>/dev/null || true)"
[ -n "$sshd" ] || [ ! -x /usr/sbin/sshd ] || sshd=/usr/sbin/sshd
[ -n "$sshd" ] || { printf '%s\n' 'SSHKEY_ERROR sshd-not-found'; exit 41; }
reload_sshd() {
  if command -v systemctl >/dev/null 2>&1 &&
     (systemctl reload sshd >/dev/null 2>&1 || systemctl reload ssh >/dev/null 2>&1); then
    return 0
  fi
  if command -v service >/dev/null 2>&1 &&
     (service sshd reload >/dev/null 2>&1 || service ssh reload >/dev/null 2>&1); then
    return 0
  fi
  return 1
}
{{restore}}
"$sshd" -t 2>/dev/null
reload_sshd
rm -f -- {{quotedBackupPath}}
""";
    }

    private static SshSetupOperationException CreateApplyException() =>
        new(
            SetupFailureKind.ServerConfigurationApply,
            "The remote SSH server configuration apply result was invalid.");

    private static void ValidateOperationId(string operationId)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        if (operationId.Length != 32 || operationId.Any(c =>
                !((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))))
        {
            throw new ArgumentException(
                "The operation id must contain exactly 32 lowercase hexadecimal characters.",
                nameof(operationId));
        }
    }

    private static void ValidateStrategy(SshServerConfigurationStrategy strategy)
    {
        if (strategy is not SshServerConfigurationStrategy.ManagedDropIn and
            not SshServerConfigurationStrategy.MainConfiguration)
        {
            throw new ArgumentOutOfRangeException(nameof(strategy));
        }
    }

    private static string ShellQuote(string value) =>
        "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";
}
