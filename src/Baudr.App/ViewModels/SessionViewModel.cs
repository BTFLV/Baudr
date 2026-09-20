using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Baudr.Core.Abstractions;
using Baudr.Core.Buffers;
using Baudr.Core.Models;
using Baudr.Core.Parsing;
using Baudr.Core.Search;
using Baudr.Core.Text;
using Baudr.Infrastructure.Diagnostics;
using Baudr.Infrastructure.Logging;
using Baudr.Infrastructure.Serial;

namespace Baudr.App.ViewModels;

public partial class SessionViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly ISerialPort _serialPort;
    private readonly ISerialPortEnumerator _portEnumerator;
    private readonly ISessionLogger _sessionLogger;
    private readonly TerminalBuffer _terminalBuffer;
    private readonly SessionStatistics _statistics;
    private readonly RxBatchProcessor _rxProcessor;
    private readonly System.Timers.Timer _statsTimer;
    private readonly List<string> _sendHistory = [];
    private int _sendHistoryIndex = -1;
    private CancellationTokenSource? _readLoopCts;
    private CancellationTokenSource? _reconnectCts;
    private System.Timers.Timer? _repeatTimer;
    private int _remainingRepeatCount;

    [ObservableProperty]
    private string _title = "Serial 1";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    private ConnectionState _state = ConnectionState.Disconnected;

    public bool IsConnected => State == ConnectionState.Connected;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string? _statusMessage = "Disconnected";

    // Port Configuration
    [ObservableProperty]
    private ObservableCollection<SerialPortInfo> _availablePorts = [];

    [ObservableProperty]
    private SerialPortInfo? _selectedPortInfo;

    [ObservableProperty]
    private string _customPortName = string.Empty;

    [ObservableProperty]
    private int _baudRate = 115200;

    [ObservableProperty]
    private int _dataBits = 8;

    [ObservableProperty]
    private SerialParity _parity = SerialParity.None;

    [ObservableProperty]
    private SerialStopBits _stopBits = SerialStopBits.One;

    [ObservableProperty]
    private SerialHandshake _handshake = SerialHandshake.None;

    [ObservableProperty]
    private bool _dtrEnable = true;

    [ObservableProperty]
    private bool _rtsEnable = true;

    [ObservableProperty]
    private bool _autoReconnect = true;

    [ObservableProperty]
    private int _reconnectDelayMs = 1500;

    // Terminal Options
    [ObservableProperty]
    private TerminalMode _terminalMode = TerminalMode.Text;

    [ObservableProperty]
    private bool _interpretAnsi = true;

    [ObservableProperty]
    private bool _showControlCharacters;

    [ObservableProperty]
    private bool _showLineNumbers;

    [ObservableProperty]
    private TimestampMode _timestampDisplayMode = TimestampMode.None;

    [ObservableProperty]
    private double _terminalFontSize = 13.0;

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private int _unreadLinesCount;

    // Send Composer
    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private SendMode _sendMode = SendMode.Text;

    [ObservableProperty]
    private LineEnding _selectedLineEnding = LineEnding.CrLf;

    [ObservableProperty]
    private string _customLineEnding = string.Empty;

    [ObservableProperty]
    private bool _clearAfterSend = true;

    [ObservableProperty]
    private bool _localEcho;

    [ObservableProperty]
    private bool _isRepeating;

    [ObservableProperty]
    private int _repeatIntervalMs = 1000;

    [ObservableProperty]
    private int _repeatCount = 10;

    [ObservableProperty]
    private bool _continuousRepeat;

    // Search & Filter
    [ObservableProperty]
    private bool _isSearchVisible;

    [ObservableProperty]
    private string _searchPattern = string.Empty;

    [ObservableProperty]
    private bool _searchRegex;

    [ObservableProperty]
    private bool _searchCaseSensitive;

    [ObservableProperty]
    private SearchResult? _currentSearchResult;

    [ObservableProperty]
    private string _searchMatchSummary = string.Empty;

    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    private string _filterInclude = string.Empty;

    [ObservableProperty]
    private string _filterExclude = string.Empty;

    [ObservableProperty]
    private bool _filterRxOnly;

    [ObservableProperty]
    private bool _filterTxOnly;

    // Plotter
    [ObservableProperty]
    private bool _isPlotterVisible;

    [ObservableProperty]
    private bool _isPlotPaused;

    [ObservableProperty]
    private ObservableCollection<PlotSeries> _plotSeries = [];

    // Packet Inspector
    [ObservableProperty]
    private bool _isInspectorVisible;

    [ObservableProperty]
    private ByteSpanInterpretation? _packetInterpretation;

    // Modem Signals
    [ObservableProperty]
    private bool _ctsSignal;

    [ObservableProperty]
    private bool _dsrSignal;

    [ObservableProperty]
    private bool _cdSignal;

    // Statistics
    [ObservableProperty]
    private string _rxBytesFormatted = "0 B";

    [ObservableProperty]
    private string _txBytesFormatted = "0 B";

    [ObservableProperty]
    private string _rxRateFormatted = "0 B/s";

    [ObservableProperty]
    private string _txRateFormatted = "0 B/s";

    [ObservableProperty]
    private string _uptimeFormatted = "00:00:00";

    // Logging
    [ObservableProperty]
    private bool _isLogging;

    [ObservableProperty]
    private string? _activeLogFile;

    [ObservableProperty]
    private string _bytesLoggedFormatted = "0 B";

    public TerminalBuffer TerminalBuffer => _terminalBuffer;
    public static IReadOnlyList<int> StandardBaudRates => SerialPortConfig.StandardBaudRates;

    public SessionViewModel(
        ISerialPort? serialPort = null,
        ISerialPortEnumerator? portEnumerator = null,
        ISessionLogger? sessionLogger = null,
        AppSettings? settings = null)
    {
        _serialPort = serialPort ?? new SystemSerialPort();
        _portEnumerator = portEnumerator ?? new SystemSerialPortEnumerator();
        _sessionLogger = sessionLogger ?? new SessionLogger();

        var s = settings ?? new AppSettings();
        _baudRate = s.Serial.DefaultBaudRate;
        _dataBits = s.Serial.DefaultDataBits;
        _parity = s.Serial.DefaultParity;
        _stopBits = s.Serial.DefaultStopBits;
        _selectedLineEnding = s.Serial.DefaultLineEnding;
        _customLineEnding = s.Serial.CustomLineEnding;
        _dtrEnable = s.Serial.DefaultDtr;
        _rtsEnable = s.Serial.DefaultRts;
        _autoReconnect = s.Serial.AutoReconnect;
        _reconnectDelayMs = s.Serial.ReconnectDelayMs;

        _terminalFontSize = s.Terminal.FontSize;
        _interpretAnsi = s.Terminal.InterpretAnsi;
        _showControlCharacters = s.Terminal.ShowControlCharacters;
        _showLineNumbers = s.Terminal.ShowLineNumbers;
        _timestampDisplayMode = s.Terminal.TimestampMode;

        _terminalBuffer = new TerminalBuffer(s.Terminal.BufferCapacity);
        _statistics = new SessionStatistics();
        _rxProcessor = new RxBatchProcessor(_terminalBuffer, _statistics, 25)
        {
            InterpretAnsi = _interpretAnsi,
            ShowControlCharacters = _showControlCharacters,
            OnPlotPointsExtracted = HandlePlotPoints
        };

        _statsTimer = new System.Timers.Timer(500);
        _statsTimer.Elapsed += (s, e) => Avalonia.Threading.Dispatcher.UIThread.Post(UpdateStatisticsUI);
        _statsTimer.Start();

        RefreshPorts();
    }

    [RelayCommand]
    public void RefreshPorts()
    {
        try
        {
            var ports = _portEnumerator.GetAvailablePorts();
            AvailablePorts = new ObservableCollection<SerialPortInfo>(ports);

            if (SelectedPortInfo == null || !AvailablePorts.Any(p => p.PortName.Equals(SelectedPortInfo.PortName, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedPortInfo = AvailablePorts.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            DiagnosticLog.Error("Failed to enumerate ports", ex);
        }
    }

    [RelayCommand]
    public async Task ConnectAsync()
    {
        if (State == ConnectionState.Connected || State == ConnectionState.Connecting) return;

        string portName = SelectedPortInfo?.PortName ?? CustomPortName;
        if (string.IsNullOrWhiteSpace(portName))
        {
            StatusMessage = "Please select or enter a serial port.";
            return;
        }

        var config = new SerialPortConfig
        {
            PortName = portName,
            BaudRate = BaudRate,
            DataBits = DataBits,
            Parity = Parity,
            StopBits = StopBits,
            Handshake = Handshake,
            DtrEnable = DtrEnable,
            RtsEnable = RtsEnable
        };

        if (!config.IsValid(out var error))
        {
            StatusMessage = error;
            return;
        }

        State = ConnectionState.Connecting;
        StatusMessage = $"Connecting to {portName}...";

        try
        {
            await _serialPort.OpenAsync(config).ConfigureAwait(false);

            State = ConnectionState.Connected;
            StatusMessage = $"Connected to {portName} ({config.Shorthand})";
            _statistics.OnConnected();
            _rxProcessor.ResetSessionTime();

            _terminalBuffer.Add(new TerminalLine
            {
                Timestamp = DateTimeOffset.UtcNow,
                Direction = Direction.System,
                Text = $"[Connected to {portName} @ {BaudRate} baud ({config.Shorthand})]"
            });

            _readLoopCts = new CancellationTokenSource();
            _ = Task.Run(() => ReadLoopAsync(_readLoopCts.Token));
        }
        catch (Exception ex)
        {
            State = ConnectionState.Error;
            StatusMessage = $"Connection failed: {ex.Message}";
            DiagnosticLog.Error($"Connection failed to {portName}", ex);

            if (AutoReconnect)
            {
                TriggerAutoReconnect();
            }
        }
    }

    [RelayCommand]
    public async Task DisconnectAsync()
    {
        _reconnectCts?.Cancel();
        _reconnectCts = null;

        _readLoopCts?.Cancel();
        _readLoopCts = null;

        StopRepeat();

        try
        {
            await _serialPort.CloseAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DiagnosticLog.Warn($"Error during port close: {ex.Message}");
        }

        State = ConnectionState.Disconnected;
        StatusMessage = "Disconnected";
        _statistics.OnDisconnected();

        _terminalBuffer.Add(new TerminalLine
        {
            Timestamp = DateTimeOffset.UtcNow,
            Direction = Direction.System,
            Text = "[Disconnected]"
        });
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        try
        {
            while (!cancellationToken.IsCancellationRequested && _serialPort.IsOpen)
            {
                int read = await _serialPort.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read > 0)
                {
                    var chunk = new byte[read];
                    Array.Copy(buffer, chunk, read);
                    _rxProcessor.Enqueue(chunk, Direction.Rx);

                    if (IsLogging)
                    {
                        await _sessionLogger.LogRxAsync(chunk).ConfigureAwait(false);
                    }
                }
                else if (read == 0)
                {
                    // Stream closed
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on disconnect
        }
        catch (Exception ex)
        {
            DiagnosticLog.Error("Serial read exception (device possibly disconnected)", ex);
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                State = ConnectionState.Error;
                StatusMessage = "Device disconnected unexpectedly.";
                _terminalBuffer.Add(new TerminalLine
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Direction = Direction.System,
                    Text = "[Device disconnected unexpectedly]"
                });

                if (AutoReconnect)
                {
                    TriggerAutoReconnect();
                }
            });
        }
    }

    private void TriggerAutoReconnect()
    {
        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();
        var ct = _reconnectCts.Token;

        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested && State != ConnectionState.Connected)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    State = ConnectionState.Reconnecting;
                    StatusMessage = $"Reconnecting in {ReconnectDelayMs / 1000.0:F1}s...";
                });

                await Task.Delay(ReconnectDelayMs, ct).ConfigureAwait(false);

                if (ct.IsCancellationRequested) break;

                // Check if port device exists
                var ports = _portEnumerator.GetAvailablePorts();
                string targetPort = SelectedPortInfo?.PortName ?? CustomPortName;

                if (ports.Any(p => p.PortName.Equals(targetPort, StringComparison.OrdinalIgnoreCase)))
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                    {
                        if (State == ConnectionState.Reconnecting)
                        {
                            await ConnectAsync().ConfigureAwait(false);
                        }
                    });
                    break;
                }
            }
        }, ct);
    }

    [RelayCommand]
    public async Task SendAsync()
    {
        if (string.IsNullOrEmpty(InputText)) return;

        byte[] payload;
        string displayText = InputText;

        if (SendMode == SendMode.Hex)
        {
            if (!HexParser.TryParse(InputText, out var bytes, out var error))
            {
                StatusMessage = error ?? "Invalid hex input.";
                return;
            }
            payload = bytes;
            displayText = HexFormatter.ToHexString(bytes, " ");
        }
        else
        {
            var textWithEnding = LineEndingHelper.ApplyLineEnding(InputText, SelectedLineEnding, CustomLineEnding);
            payload = TextEncodingHelper.Encode(textWithEnding);
            displayText = InputText;
        }

        if (payload.Length == 0) return;

        // History
        if (_sendHistory.Count == 0 || _sendHistory[^1] != InputText)
        {
            _sendHistory.Add(InputText);
        }
        _sendHistoryIndex = _sendHistory.Count;

        if (LocalEcho)
        {
            _terminalBuffer.Add(new TerminalLine
            {
                Timestamp = DateTimeOffset.UtcNow,
                Direction = Direction.Tx,
                Text = displayText,
                RawBytes = payload
            });
        }

        if (State == ConnectionState.Connected)
        {
            try
            {
                await _serialPort.WriteAsync(payload).ConfigureAwait(false);
                _statistics.AddTx(payload.Length);

                if (IsLogging)
                {
                    await _sessionLogger.LogTxAsync(payload, displayText).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Send error: {ex.Message}";
                DiagnosticLog.Error("Write failed", ex);
            }
        }

        if (ClearAfterSend)
        {
            InputText = string.Empty;
        }
    }

    [RelayCommand]
    public void HistoryUp()
    {
        if (_sendHistory.Count == 0) return;
        if (_sendHistoryIndex > 0)
        {
            _sendHistoryIndex--;
            InputText = _sendHistory[_sendHistoryIndex];
        }
    }

    [RelayCommand]
    public void HistoryDown()
    {
        if (_sendHistory.Count == 0) return;
        if (_sendHistoryIndex < _sendHistory.Count - 1)
        {
            _sendHistoryIndex++;
            InputText = _sendHistory[_sendHistoryIndex];
        }
        else
        {
            _sendHistoryIndex = _sendHistory.Count;
            InputText = string.Empty;
        }
    }

    [RelayCommand]
    public void StartRepeat()
    {
        if (IsRepeating || string.IsNullOrEmpty(InputText)) return;

        IsRepeating = true;
        _remainingRepeatCount = RepeatCount;

        _repeatTimer = new System.Timers.Timer(Math.Max(50, RepeatIntervalMs));
        _repeatTimer.Elapsed += async (s, e) =>
        {
            if (!IsRepeating) return;

            if (!ContinuousRepeat)
            {
                if (--_remainingRepeatCount <= 0)
                {
                    StopRepeat();
                    return;
                }
            }

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await SendAsync().ConfigureAwait(false);
            });
        };
        _repeatTimer.Start();
    }

    [RelayCommand]
    public void StopRepeat()
    {
        IsRepeating = false;
        if (_repeatTimer != null)
        {
            _repeatTimer.Stop();
            _repeatTimer.Dispose();
            _repeatTimer = null;
        }
    }

    [RelayCommand]
    public void ClearTerminal()
    {
        _terminalBuffer.Clear();
        UnreadLinesCount = 0;
    }

    [RelayCommand]
    public void ZoomIn()
    {
        if (TerminalFontSize < 32) TerminalFontSize += 1.5;
    }

    [RelayCommand]
    public void ZoomOut()
    {
        if (TerminalFontSize > 8) TerminalFontSize -= 1.5;
    }

    [RelayCommand]
    public void ResetZoom()
    {
        TerminalFontSize = 13.0;
    }

    [RelayCommand]
    public void ToggleSearch()
    {
        IsSearchVisible = !IsSearchVisible;
    }

    [RelayCommand]
    public void ExecuteSearch()
    {
        if (string.IsNullOrEmpty(SearchPattern))
        {
            CurrentSearchResult = null;
            SearchMatchSummary = string.Empty;
            return;
        }

        var lines = _terminalBuffer.GetSnapshot();
        var query = new SearchQuery
        {
            Pattern = SearchPattern,
            IsRegex = SearchRegex,
            CaseSensitive = SearchCaseSensitive
        };

        CurrentSearchResult = SearchEngine.Execute(lines, query);
        if (CurrentSearchResult.ErrorMessage != null)
        {
            SearchMatchSummary = CurrentSearchResult.ErrorMessage;
        }
        else
        {
            SearchMatchSummary = CurrentSearchResult.TotalMatches > 0 
                ? $"{CurrentSearchResult.CurrentMatchIndex + 1} of {CurrentSearchResult.TotalMatches}" 
                : "No matches";
        }
    }

    [RelayCommand]
    public void NextSearchMatch()
    {
        if (CurrentSearchResult == null || CurrentSearchResult.TotalMatches == 0) return;
        CurrentSearchResult.MoveNext();
        SearchMatchSummary = $"{CurrentSearchResult.CurrentMatchIndex + 1} of {CurrentSearchResult.TotalMatches}";
    }

    [RelayCommand]
    public void PrevSearchMatch()
    {
        if (CurrentSearchResult == null || CurrentSearchResult.TotalMatches == 0) return;
        CurrentSearchResult.MovePrevious();
        SearchMatchSummary = $"{CurrentSearchResult.CurrentMatchIndex + 1} of {CurrentSearchResult.TotalMatches}";
    }

    [RelayCommand]
    public void ToggleDtr()
    {
        DtrEnable = !DtrEnable;
        _serialPort.SetDtr(DtrEnable);
    }

    [RelayCommand]
    public void ToggleRts()
    {
        RtsEnable = !RtsEnable;
        _serialPort.SetRts(RtsEnable);
    }

    [RelayCommand]
    public void SendBreak()
    {
        _serialPort.SetBreak(true);
        Task.Delay(250).ContinueWith(_ => _serialPort.SetBreak(false));
    }

    [RelayCommand]
    public void TogglePlotter()
    {
        IsPlotterVisible = !IsPlotterVisible;
    }

    [RelayCommand]
    public void ClearPlot()
    {
        foreach (var series in PlotSeries)
        {
            series.Clear();
        }
    }

    [RelayCommand]
    public void TogglePlotPause()
    {
        IsPlotPaused = !IsPlotPaused;
    }

    private static readonly string[] SeriesColors = ["#00B4D8", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6", "#EC4899"];

    private void HandlePlotPoints(List<PlotDataPoint> points)
    {
        if (IsPlotPaused || !IsPlotterVisible) return;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            foreach (var pt in points)
            {
                var s = PlotSeries.FirstOrDefault(p => p.Name.Equals(pt.SeriesName, StringComparison.OrdinalIgnoreCase));
                if (s == null)
                {
                    s = new PlotSeries
                    {
                        Name = pt.SeriesName,
                        ColorHex = SeriesColors[PlotSeries.Count % SeriesColors.Length]
                    };
                    PlotSeries.Add(s);
                }
                s.Add(pt.Value, pt.Timestamp);
            }
        });
    }

    [RelayCommand]
    public void ToggleInspector()
    {
        IsInspectorVisible = !IsInspectorVisible;
    }

    public void InspectBytes(byte[] bytes)
    {
        PacketInterpretation = PacketInspector.Inspect(bytes);
    }

    [RelayCommand]
    public void ResetStatistics()
    {
        _statistics.Reset();
        UpdateStatisticsUI();
    }

    private void UpdateStatisticsUI()
    {
        _statistics.UpdateRates();

        // Modem lines
        var signals = _serialPort.ReadSignals();
        CtsSignal = signals.CtsHolding;
        DsrSignal = signals.DsrHolding;
        CdSignal = signals.CdHolding;

        RxBytesFormatted = FormatBytes(_statistics.RxBytes);
        TxBytesFormatted = FormatBytes(_statistics.TxBytes);
        RxRateFormatted = $"{FormatBytes((long)_statistics.CurrentRxRate)}/s";
        TxRateFormatted = $"{FormatBytes((long)_statistics.CurrentTxRate)}/s";

        var dur = _statistics.ConnectedDuration;
        UptimeFormatted = $"{(int)dur.TotalHours:D2}:{dur.Minutes:D2}:{dur.Seconds:D2}";

        if (IsLogging)
        {
            BytesLoggedFormatted = FormatBytes(_sessionLogger.BytesWritten);
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    public async ValueTask DisposeAsync()
    {
        _statsTimer.Stop();
        _statsTimer.Dispose();
        StopRepeat();
        await DisconnectAsync().ConfigureAwait(false);
        await _rxProcessor.DisposeAsync().ConfigureAwait(false);
        await _sessionLogger.DisposeAsync().ConfigureAwait(false);
        await _serialPort.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
