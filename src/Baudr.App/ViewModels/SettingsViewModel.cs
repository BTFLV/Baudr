using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Baudr.Core.Abstractions;
using Baudr.Core.Models;
using Baudr.Infrastructure.Diagnostics;

namespace Baudr.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private bool _isOpen;

    // General
    [ObservableProperty]
    private AppTheme _theme;

    [ObservableProperty]
    private bool _restoreSessions;

    // Terminal
    [ObservableProperty]
    private string _fontFamily;

    [ObservableProperty]
    private double _fontSize;

    [ObservableProperty]
    private bool _lineWrapping;

    [ObservableProperty]
    private TimestampMode _timestampMode;

    [ObservableProperty]
    private int _bufferCapacity;

    [ObservableProperty]
    private bool _interpretAnsi;

    [ObservableProperty]
    private bool _showControlCharacters;

    [ObservableProperty]
    private bool _showLineNumbers;

    // Serial
    [ObservableProperty]
    private int _defaultBaudRate;

    [ObservableProperty]
    private LineEnding _defaultLineEnding;

    [ObservableProperty]
    private bool _autoReconnect;

    [ObservableProperty]
    private int _reconnectDelayMs;

    [ObservableProperty]
    private bool _defaultDtr;

    [ObservableProperty]
    private bool _defaultRts;

    // Logging
    [ObservableProperty]
    private string _logDirectory;

    [ObservableProperty]
    private LogFormat _defaultLogFormat;

    // About
    public string AppVersion { get; }
    public string RuntimeInfo { get; }
    public string CommitSha { get; }

    public event Action? Closed;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        var s = _settingsService.Current;

        _theme = s.General.Theme;
        _restoreSessions = s.General.RestoreSessionsOnStartup;

        _fontFamily = s.Terminal.FontFamily;
        _fontSize = s.Terminal.FontSize;
        _lineWrapping = s.Terminal.LineWrapping;
        _timestampMode = s.Terminal.TimestampMode;
        _bufferCapacity = s.Terminal.BufferCapacity;
        _interpretAnsi = s.Terminal.InterpretAnsi;
        _showControlCharacters = s.Terminal.ShowControlCharacters;
        _showLineNumbers = s.Terminal.ShowLineNumbers;

        _defaultBaudRate = s.Serial.DefaultBaudRate;
        _defaultLineEnding = s.Serial.DefaultLineEnding;
        _autoReconnect = s.Serial.AutoReconnect;
        _reconnectDelayMs = s.Serial.ReconnectDelayMs;
        _defaultDtr = s.Serial.DefaultDtr;
        _defaultRts = s.Serial.DefaultRts;

        _logDirectory = s.Logging.DefaultDirectory;
        _defaultLogFormat = s.Logging.DefaultFormat;

        var asm = Assembly.GetExecutingAssembly();
        var ver = asm.GetName().Version;
        AppVersion = ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "0.1.0-dev";
        RuntimeInfo = $"{RuntimeInformation.FrameworkDescription} ({RuntimeInformation.OSArchitecture}) on {RuntimeInformation.OSDescription}";

        var infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        CommitSha = !string.IsNullOrEmpty(infoVer) ? infoVer : "local-dev";
    }

    [RelayCommand]
    public void Save()
    {
        _settingsService.Update(s =>
        {
            s.General.Theme = Theme;
            s.General.RestoreSessionsOnStartup = RestoreSessions;

            s.Terminal.FontFamily = FontFamily;
            s.Terminal.FontSize = FontSize;
            s.Terminal.LineWrapping = LineWrapping;
            s.Terminal.TimestampMode = TimestampMode;
            s.Terminal.BufferCapacity = BufferCapacity;
            s.Terminal.InterpretAnsi = InterpretAnsi;
            s.Terminal.ShowControlCharacters = ShowControlCharacters;
            s.Terminal.ShowLineNumbers = ShowLineNumbers;

            s.Serial.DefaultBaudRate = DefaultBaudRate;
            s.Serial.DefaultLineEnding = DefaultLineEnding;
            s.Serial.AutoReconnect = AutoReconnect;
            s.Serial.ReconnectDelayMs = ReconnectDelayMs;
            s.Serial.DefaultDtr = DefaultDtr;
            s.Serial.DefaultRts = DefaultRts;

            s.Logging.DefaultDirectory = LogDirectory;
            s.Logging.DefaultFormat = DefaultLogFormat;
        });

        App.ApplyTheme(Theme);
        Close();
    }

    [RelayCommand]
    public void Close()
    {
        IsOpen = false;
        Closed?.Invoke();
    }

    [RelayCommand]
    public static void OpenLogsFolder()
    {
        try
        {
            var dir = DiagnosticLog.GetLogsDirectory();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", dir);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", dir);
            }
        }
        catch (Exception ex)
        {
            DiagnosticLog.Error("Failed to open logs directory", ex);
        }
    }
}
