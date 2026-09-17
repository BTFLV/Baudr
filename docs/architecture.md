# Baudr Architecture & Design Deep Dive

Baudr is designed from the ground up for high throughput, zero UI stutter, deterministic memory footprint, and Native AOT / cross-platform compatibility.

---

## 1. High-Throughput Decoupled Buffering Architecture

Serial terminals frequently lock up or consume gigabytes of RAM when receiving high-speed telemetry (e.g., 2,000,000 baud with streaming binary or sensor logs). Baudr completely decouples serial transport ingestion from UI rendering via a three-tier pipeline:

```
+--------------------------------------------------------------------+
|                         ISerialPort (OS Thread)                     |
|           Async background I/O (System.IO.Ports / Mock)            |
+---------------------------------+----------------------------------+
                                  |
                        byte[] (Raw Stream)
                                  v
+--------------------------------------------------------------------+
|                    RxBatchProcessor (Worker Task)                  |
|  * High-frequency buffer collection                                 |
|  * Batched dispatch at 25ms cadence (max 40 UI fps updates)         |
|  * Prevents UI message-pump saturation                             |
+---------------------------------+----------------------------------+
                                  |
                     Batched byte[] Chunks
                                  v
+--------------------------------------------------------------------+
|               TerminalBuffer & Parsers (Core Engine)               |
|  * AnsiParser (ANSI color, bold, dim, RGB, cursor control)          |
|  * LineEndingAndControlHelper (Unicode control glyphs)             |
|  * PlotDataParser (telemetry extraction for real-time plotter)     |
|  * PacketInspector (binary LE/BE decoding)                         |
|  * Fixed ring buffer (configurable 5,000 - 100,000 lines)          |
+---------------------------------+----------------------------------+
                                  |
                       Observable Line Tokens
                                  v
+--------------------------------------------------------------------+
|              TerminalControl (Avalonia Custom Control)             |
|  * Virtualized direct canvas rendering                             |
|  * Calculates visible line range from ScrollViewer.Offset.Y        |
|  * Only renders visible viewport lines (typically 30 - 80 lines)   |
|  * Smooth mouse drag selection, copy to clipboard                  |
+--------------------------------------------------------------------+
```

### Key Buffering Properties:
* **Fixed-capacity ring buffer (`TerminalBuffer`):** Once capacity is reached, oldest lines are dropped efficiently without allocating new arrays or shifting items linearly.
* **Batch processor (`RxBatchProcessor`):** Groups rapid bursts of serial events. Under high data rates, UI notifications occur at a regulated 25ms timer interval (40 fps), avoiding dispatching thousands of single-byte UI events.
* **Virtualized Canvas (`TerminalControl`):** Rather than instantiating thousands of Avalonia `TextBlock` elements inside an ItemsControl, `TerminalControl` overrides `Render(DrawingContext context)` and uses `FormattedText` exclusively for the viewport slice. Even with 100,000 lines in the buffer, rendering time is strictly $O(V)$ where $V$ is visible rows.

---

## 2. Solution Structure

The repository follows a clean 3-layer architecture with separate test suites:

```
Baudr.slnx
├── src/
│   ├── Baudr.Core/             # Zero-dependency domain models, parsers, ring buffer
│   ├── Baudr.Infrastructure/   # Serial hardware drivers, hotplug watcher, JSON persistence
│   └── Baudr.App/              # Avalonia UI 12.x desktop app, views, controls, viewmodels
└── tests/
    ├── Baudr.Core.Tests/       # 40 fast unit tests covering parsers, buffer, ANSI, telemetry
    └── Baudr.IntegrationTests/ # 5 end-to-end integration tests using MockSerialPort
```

### Dependency Rules:
* `Baudr.Core` has **no UI and no OS dependencies**. It can run on any .NET runtime, WebAssembly, or embedded CLR.
* `Baudr.Infrastructure` implements the interfaces defined in `Baudr.Core` (`ISerialPort`, `ISerialPortEnumerator`, `ISettingsService`, `ISessionLogger`).
* `Baudr.App` references `Baudr.Core` and `Baudr.Infrastructure`. ViewModels rely on abstraction interfaces to allow 100% testability.

---

## 3. Native AOT Readiness

Baudr is engineered for .NET 10 Native AOT (`PublishAot=true`):
1. **Source-Generated JSON:** All serialization/deserialization uses `[JsonSerializable]` with `BaudrJsonContext`. Reflection-based `JsonSerializer` is prohibited.
2. **Compiled Bindings:** Avalonia XAML bindings use `x:DataType` and compiled bindings throughout.
3. **MVVM Source Generators:** ViewModels use `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`), eliminating runtime reflection.
4. **No Dynamic Assembly Loading:** All components are linked statically.

---

## 4. Signal Handling and Hardware Control

The `ISerialPort` interface exposes fine-grained hardware flow control and pin status monitoring:
* **Input Signals (Pins read from device):**
  * `CtsHolding`: Clear To Send
  * `DsrHolding`: Data Set Ready
  * `CdHolding`: Carrier Detect
* **Output Signals (Pins driven by Baudr):**
  * `DtrEnable`: Data Terminal Ready (often used to trigger ESP32 / Arduino bootloaders)
  * `RtsEnable`: Request To Send
  * `BreakState`: Transmit continuous break condition
* **Serial Port Hot-Plug Watcher:** Periodically polls the OS hardware registry/enumerator. When a USB-to-UART adapter (FTDI, CP2102, CH340) is unplugged or plugged in, the device list updates immediately and active sessions transition safely to an error/disconnected state without crashing.

