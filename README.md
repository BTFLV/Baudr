<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/logo-dark.svg">
  <img src="assets/logo-light.svg" alt="Baudr logo" width="420">
</picture>

**Baudr** is a modern, high-performance Serial Monitor and Terminal for developers, embedded engineers, electronics engineers, makers, and anyone working with UART/serial devices.

Built with **C#**, **.NET 10**, and **Avalonia UI 12.x**, Baudr provides a keyboard-first, lightweight, and responsive experience capable of handling sustained high-throughput serial streams with zero UI stutter.

---

## ✨ Features

- **⚡ High-Throughput Buffering:** Decoupled background acquisition engine with batched rendering. Tested with sustained high-baud streams (up to 2,000,000+ baud) without UI thread freezing.
- **🖥️ Multiple Terminal Modes:**
  - **Text Mode:** Standard terminal with configurable encoding (UTF-8, ASCII, Latin-1) and resilient decoding that never crashes on corrupted bytes.
  - **Hex Mode:** Formatted hex dumps with memory offsets, hex byte pairs, and printable ASCII gutter.
  - **Text + Hex:** Combined views for protocol debugging.
  - **Control Character Glyphs:** Optional visual rendering of control characters (`␍` CR, `␊` LF, `␉` TAB, `␀` NUL).
- **🎨 ANSI Escape Sequence Support:** Robust SGR/CSI state-machine parser supporting standard 16 colors, 256 indexed colors, 24-bit RGB truecolor, bold, dim, italic, underline, and backspace.
- **📈 Real-Time Serial Plotter:** Custom hardware-accelerated 2D plotting canvas. Automatically parses numeric values, CSV streams, `key=val` pairs, and JSON telemetry with auto-scaling and series toggle.
- **🔍 Fast Search & Display Filtering:** Substring and regex search with match counts and navigation (`F3` / `Shift+F3`), plus real-time include/exclude and RX/TX display filters.
- **📦 Packet Inspector:** Select any byte range in hex or text view to inspect decodings: UInt8/16/32, Int8/16/32, Float32 (LE and BE), ASCII, UTF-8, and binary bit flags.
- **🗂️ Multi-Session Tabs:** Manage multiple independent serial sessions simultaneously with individual configurations, buffers, statistics, and loggers.
- **📋 Reusable Connection Profiles:** Store hardware presets (Arduino, ESP32, Modbus RTU, High-Speed UART) with custom line endings, bauds, and auto-reconnect rules.
- **⌨️ Keyboard-First Operation & Command Palette:** Press `Ctrl+K` (`Cmd+K` on macOS) to open the spotlight command palette.
- **💾 Session Logging & Export:** Record raw binary, plain text, timestamped text, or CSV files directly to disk independently of the UI display buffer.
- **🔌 Modem Signals & Break:** Inspect CTS, DSR, CD status lines in real time; toggle DTR and RTS; send BREAK signals.
- **🌙 Modern 2026 UI & Themes:** Seamless Dark, Light, and System themes using semantic design tokens and custom vector icons.
- **🚀 Native AOT Ready:** Architected without dynamic reflection or heavy runtime code generation for fast startup and minimal binary footprint.

---

## 💻 Supported Platforms

| Platform | Architecture | Binary Package |
| :--- | :--- | :--- |
| **Windows** | x64 | `Baudr-win-x64.zip` (`Baudr.exe`) |
| **macOS** | Apple Silicon (`arm64`) | `Baudr-osx-arm64.zip` (`Baudr.app`) |
| **macOS** | Intel (`x64`) | `Baudr-osx-x64.zip` (`Baudr.app`) |
| **Linux** | x64 | `Baudr-linux-x64.zip` (`Baudr`) |

> **No external .NET runtime required**: All rolling binaries are self-contained and precompiled.

---

## 🚀 Getting Started

### Downloading Rolling Builds
Every commit to `main` automatically builds, tests, and updates the latest rolling release on GitHub under the [`current`](https://github.com/BTFLV/Baudr/releases/tag/current) tag.

1. Download the archive for your operating system from the Releases page.
2. Extract the archive:
   - **Windows:** Extract `Baudr-win-x64.zip` and run `Baudr.exe`.
   - **macOS:** Extract `Baudr-osx-arm64.zip` (or `x64`), drag `Baudr.app` to Applications, and launch it.
   - **Linux:** Extract `Baudr-linux-x64.zip`, make executable (`chmod +x Baudr`), and run `./Baudr`.

---

## 🛠️ Building from Source

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (version 10.0.100 or later)
- Git

### Build Instructions
```bash
# 1. Clone the repository
git clone https://github.com/BTFLV/Baudr.git
cd Baudr

# 2. Restore dependencies
dotnet restore

# 3. Build the solution
dotnet build -c Release

# 4. Run automated tests
dotnet test -c Release

# 5. Launch the application
dotnet run --project src/Baudr.App
```

---

## 🐧 Serial Permissions on Linux

On Linux distributions, access to serial devices (`/dev/ttyUSB*`, `/dev/ttyACM*`) typically requires membership in the `dialout` or `uucp` group.

### Granting Permission
```bash
# For Ubuntu / Debian / Raspberry Pi OS:
sudo usermod -a -G dialout $USER

# For Arch Linux / Fedora:
sudo usermod -a -G uucp $USER
```
*Note: Log out and log back in for group changes to take effect.*

If ModemManager is interfering with serial ports on connect:
```bash
sudo systemctl stop ModemManager
```

---

## ⌨️ Keyboard Shortcuts

| Shortcut (Win/Linux) | Shortcut (macOS) | Action |
| :--- | :--- | :--- |
| `Ctrl+K` | `Cmd+K` | Open Command Palette |
| `Ctrl+N` | `Cmd+N` | New Serial Session Tab |
| `Ctrl+W` | `Cmd+W` | Close Active Session Tab |
| `Ctrl+Shift+C` | `Cmd+Shift+C` | Connect / Disconnect |
| `Ctrl+L` | `Cmd+L` | Clear Terminal Buffer |
| `Ctrl+F` | `Cmd+F` | Toggle Search Bar |
| `Ctrl+P` | `Cmd+P` | Toggle Serial Plotter |
| `Ctrl+I` | `Cmd+I` | Toggle Packet Inspector |
| `Ctrl+,` | `Cmd+,` | Open Settings Dialog |
| `Ctrl++` / `Ctrl+-` | `Cmd++` / `Cmd+-` | Zoom In / Out Terminal Font |
| `Ctrl+0` | `Cmd+0` | Reset Terminal Font Zoom |
| `Enter` / `Ctrl+Enter` | `Enter` / `Cmd+Enter` | Send Input in Send Composer |
| `Up` / `Down` | `Up` / `Down` | Navigate Send Command History |

---

## 🏛️ Architecture Overview

Baudr follows a pragmatic MVVM architecture with strict separation of concerns:

- **`Baudr.Core`**: Pure domain library with zero UI dependencies. Houses the serial abstractions (`ISerialPort`), bounded circular buffers (`TerminalBuffer`), high-throughput batching (`RxBatchProcessor`), ANSI escape parser (`AnsiParser`), packet inspector (`PacketInspector`), hex formatter, telemetry plotter parser, search and highlight engines.
- **`Baudr.Infrastructure`**: Concrete platform adapters wrapping `System.IO.Ports.SerialPort`, cross-platform serial enumeration (Windows COM, Linux `/dev/tty*`, macOS `/dev/cu.*`), device hot-plug watchers, atomic JSON persistence (`SettingsService`), and asynchronous file loggers (`SessionLogger`).
- **`Baudr.App`**: Avalonia UI desktop application featuring the custom virtualized `TerminalControl`, hardware-accelerated `PlotterControl`, themes, styles, views, and viewmodels.

---

## 🧪 Testing

Baudr includes comprehensive unit and integration tests:
- **Unit Tests (`tests/Baudr.Core.Tests`)**: Validates serial configuration models, hex parsers/formatters, packet inspector type decodings, ANSI sequence processing, ring buffers, search and filter engines, and settings serialization.
- **Integration Tests (`tests/Baudr.IntegrationTests`)**: Uses an in-memory mock serial transport (`MockSerialPort`) to test high-throughput streaming (5000+ packets/sec), buffer batching, unexpected device disconnections, and disk logging/exporting without requiring physical hardware.

To run tests:
```bash
dotnet test -c Release
```

---

## 📄 License

Baudr is released under the [MIT License](LICENSE).
Third-party component licenses are documented in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

