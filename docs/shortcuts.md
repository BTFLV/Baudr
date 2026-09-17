# Baudr Keyboard Shortcuts & Quick Navigation

Baudr is designed to be fully keyboard-driven so developers and engineers never have to leave the home row while testing hardware.

---

## Global Shortcuts

| Shortcut | Action | Description |
| :--- | :--- | :--- |
| `Ctrl + K` or `Ctrl + P` / `Cmd + P` | **Command Palette** | Open searchable omnibar to run any action or switch ports |
| `Ctrl + T` / `Cmd + T` | **New Tab** | Open a new serial session tab |
| `Ctrl + W` / `Cmd + W` | **Close Tab** | Close the currently active session tab |
| `Ctrl + Tab` | **Next Tab** | Cycle to the next open session |
| `Ctrl + Shift + Tab` | **Previous Tab** | Cycle to the previous open session |
| `Ctrl + ,` / `Cmd + ,` | **Settings Dialog** | Open global application settings |

---

## Session & Connection Shortcuts

| Shortcut | Action | Description |
| :--- | :--- | :--- |
| `Ctrl + O` / `Cmd + O` | **Toggle Connect / Disconnect** | Connect to or disconnect from the configured port |
| `Ctrl + L` / `Cmd + K` | **Clear Buffer** | Clear terminal output and reset line counter |
| `Ctrl + F` / `Cmd + F` | **Focus Search Bar** | Open search bar and jump cursor to search box |
| `F3` | **Find Next** | Jump to the next match of current search query |
| `Shift + F3` | **Find Previous** | Jump to previous match of current search query |
| `Escape` | **Close Dialog / Bar** | Dismiss search bar, command palette, or modal dialog |

---

## Terminal & Data Send Shortcuts

| Shortcut | Action | Description |
| :--- | :--- | :--- |
| `Enter` (in input field) | **Send Command** | Transmit text/hex according to current mode & line ending |
| `Up Arrow` (in input field) | **History Previous** | Recall previously sent commands |
| `Down Arrow` (in input field) | **History Next** | Advance forward through command history |
| `Ctrl + C` / `Cmd + C` | **Copy Selection** | Copy highlighted terminal text to system clipboard |
| `Ctrl + Shift + S` | **Send Break** | Pulse the serial TX break condition |
| `Ctrl + Shift + R` | **Toggle RTS** | Invert the state of Request To Send hardware pin |
| `Ctrl + Shift + D` | **Toggle DTR** | Invert the state of Data Terminal Ready hardware pin |

---

## Command Palette Usage

Press `Ctrl+K` at any time to open the Command Palette. Type search terms to quickly execute actions:
* `connect` — Connect to the selected port
* `disconnect` — Disconnect the active port
* `clear` — Clear terminal screen
* `plotter` — Toggle real-time plotter drawer
* `hex` — Toggle hex dump mode
* `export` — Save session log to disk
* `settings` — Open application settings

