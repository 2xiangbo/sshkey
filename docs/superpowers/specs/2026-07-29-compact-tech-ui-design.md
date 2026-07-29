# Compact Tech UI Design

## Goal

Redesign the existing SSH key setup window as a compact, polished desktop tool
with a dark space-black and cyan visual language. Preserve all SSH key
generation, server setup, status reporting, and Codex connection-copy behavior.

## Window

- Fixed client size: 680 x 520 pixels.
- Borderless window with a custom 42-pixel title bar.
- The title bar supports window dragging and includes flat minimize and close
  buttons.
- Main background: `#0B1118`.
- No gradients, decorative illustrations, large empty areas, or default Windows
  button styling.

## Layout

The content uses 20-pixel horizontal margins and compact vertical spacing:

1. First row: server IP on the left and port on the right.
2. Second row: username on the left and password on the right.
3. Third row: private-key path across the full content width.
4. Fourth section: compact status display across the full content width.
5. Fifth section: Codex connection details across the full content width.
6. Primary action button aligned to the lower-right corner.

The connection-details area remains tall enough to show all five generated
lines without hiding the private-key path.

## Visual System

- Surface and input background: `#0E1822` and `#101B26`.
- Neutral border: `#263747`.
- Cyan accent and focus color: `#38D7FF`.
- Primary text: `#E9F5FA`.
- Secondary labels: `#7F95A3`.
- Success text: `#45E6A1`.
- Error text: `#FF6B7A`.
- Primary button: cyan fill with dark text and flat hover/pressed states.
- UI font: Segoe UI; generated connection details: Consolas.
- Crisp one-pixel borders and restrained cyan accents provide the technology
  character without simulated neon glow or visual clutter.

## Components

- A custom title bar owns dragging, minimize, and close interactions.
- Existing input and output control names remain unchanged so current form logic
  continues to work.
- Inputs use flat dark styling and consistent fixed dimensions.
- Status and connection-details fields remain read-only.
- Password masking remains enabled.

## Behavior And Data Flow

The redesign does not alter the setup workflow:

1. The user enters IP, port, username, password, and private-key path.
2. The existing setup service generates the key and installs it on the server.
3. The status area shows progress, success, or the existing error message.
4. On success, the existing formatter fills and copies the five-line Codex
   connection description.

The password must never appear in the status or connection-details output.

## Error Handling

Existing validation and SSH errors remain unchanged. The compact status field
uses one line; longer messages remain selectable so the full text can be read
or copied. Error text uses the error color; successful completion uses the
success color.

## Verification

- Add a layout test for the compact dimensions, borderless frame, title-bar
  controls, required row relationships, dark surfaces, and cyan primary action.
- Run the complete automated test suite.
- Publish a self-contained Windows x64 single-file executable as version 7.
- Launch the packaged executable and verify that its process remains responsive.

## Acceptance Criteria

- The window is visibly smaller and denser than version 6.
- The interface reads as space-black and cyan rather than gray and green.
- No default Windows button appearance remains.
- All five requested layout sections remain in the required order.
- Existing generation, server installation, status, and copy behavior still
  pass their tests.
