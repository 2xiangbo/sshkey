# SSHKEY form visual polish design

## Goal

Apply the approved compact SSHKEY layout to make the inputs easier to scan and
the connection state easier to find, without changing any setup, OpenSSH,
localization, link, or window-behavior logic.

## Scope

Only `Form1` presentation and its layout tests change. Existing control names,
event handlers, data flow, service calls, URLs, language behavior, and button
semantics remain unchanged.

## Layout

The form retains its 680-pixel width, custom title bar, dark palette, and
footer actions.

1. First input row has three adjacent sections:
   - Server IP: flexible and widest section.
   - Port: narrow fixed-width section.
   - OpenSSH: remaining right-side section, containing the existing readiness
     or install button.
2. Second input row contains the username and password fields at equal widths.
3. Private-key path remains a full-width field below the credentials.
4. The connection-details header is one horizontal row: `Codex 连接信息` on
   the left and the existing ready/working/success/error status on the right.
   The read-only connection-details text box remains immediately below it.
5. The footer preserves the GitHub link, XXCodex link, language selector, and
   primary action in their current logical order.

## Visual treatment

- Inputs use a consistent dark surface with a calm border and clear label
  spacing, rather than relying on an ungrouped collection of text boxes.
- The main action remains the sole high-emphasis cyan control.
- OpenSSH state colors remain semantically unchanged: neutral during checks,
  green when installed, cyan when installation is available, and red after an
  installation failure.
- Labels and the status line must remain legible at the existing compact form
  size; no content may overlap, truncate essential labels, or reduce keyboard
  tab accessibility.

## Testing

Layout tests will assert the three-part first row, equal-width credential row,
the inline connection-status header, retained footer order, and unchanged
control names. Existing lifecycle and domain tests remain unchanged.

## Acceptance criteria

- Server IP, port, and OpenSSH are visibly aligned in one first row.
- Username and password share the second row equally.
- Status text appears on the same header line as connection details.
- All existing application behavior remains intact.
- The full test suite passes.
