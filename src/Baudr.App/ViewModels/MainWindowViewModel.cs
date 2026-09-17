using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Baudr.Core.Abstractions;
using Baudr.Core.Models;
using Baudr.Infrastructure.Diagnostics;
using Baudr.Infrastructure.Serial;

namespace Baudr.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ISerialPortEnumerator _portEnumerator;
    private int _sessionCounter = 1;

    [ObservableProperty]
    private ObservableCollection<SessionViewModel> _sessions = [];

    [ObservableProperty]
    private SessionViewModel? _selectedSession;

    [ObservableProperty]
    private CommandPaletteViewModel _commandPalette;

    [ObservableProperty]
    private SettingsViewModel _settings;

    public MainWindowViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _portEnumerator = new SystemSerialPortEnumerator();

        _commandPalette = new CommandPaletteViewModel();
        _settings = new SettingsViewModel(_settingsService);

        // Open first session
        CreateNewSession();

        PopulateCommandPalette();
    }

    [RelayCommand]
    public void CreateNewSession()
    {
        var session = new SessionViewModel(
            portEnumerator: _portEnumerator,
            settings: _settingsService.Current)
        {
            Title = $"Session {_sessionCounter++}"
        };

        Sessions.Add(session);
        SelectedSession = session;
    }

    [RelayCommand]
    public void SelectSession(SessionViewModel? session)
    {
        if (session != null)
        {
            SelectedSession = session;
        }
    }

    [RelayCommand]
    public void DuplicateSession(SessionViewModel? session)
    {
        var source = session ?? SelectedSession;
        if (source == null) return;

        var newSession = new SessionViewModel(
            portEnumerator: _portEnumerator,
            settings: _settingsService.Current)
        {
            Title = $"{source.Title} (Copy)",
            BaudRate = source.BaudRate,
            DataBits = source.DataBits,
            Parity = source.Parity,
            StopBits = source.StopBits,
            Handshake = source.Handshake,
            DtrEnable = source.DtrEnable,
            RtsEnable = source.RtsEnable,
            SelectedLineEnding = source.SelectedLineEnding,
            CustomLineEnding = source.CustomLineEnding,
            TerminalMode = source.TerminalMode,
            InterpretAnsi = source.InterpretAnsi,
            ShowControlCharacters = source.ShowControlCharacters,
            ShowLineNumbers = source.ShowLineNumbers
        };

        if (source.SelectedPortInfo != null)
        {
            newSession.SelectedPortInfo = source.SelectedPortInfo;
        }

        Sessions.Add(newSession);
        SelectedSession = newSession;
    }

    [RelayCommand]
    public async Task CloseSessionAsync(SessionViewModel? session)
    {
        var target = session ?? SelectedSession;
        if (target == null) return;

        if (Sessions.Count <= 1)
        {
            // Always keep at least one session
            await target.DisconnectAsync().ConfigureAwait(true);
            target.ClearTerminal();
            return;
        }

        int index = Sessions.IndexOf(target);
        Sessions.Remove(target);
        await target.DisposeAsync().ConfigureAwait(true);

        if (SelectedSession == target)
        {
            SelectedSession = Sessions[Math.Min(index, Sessions.Count - 1)];
        }
    }

    [RelayCommand]
    public async Task CloseOtherSessionsAsync(SessionViewModel? session)
    {
        var keep = session ?? SelectedSession;
        if (keep == null) return;

        var toRemove = Sessions.Where(s => s != keep).ToList();
        foreach (var s in toRemove)
        {
            Sessions.Remove(s);
            await s.DisposeAsync().ConfigureAwait(true);
        }

        SelectedSession = keep;
    }

    [RelayCommand]
    public void OpenCommandPalette()
    {
        PopulateCommandPalette();
        CommandPalette.IsOpen = true;
    }

    [RelayCommand]
    public void OpenSettings()
    {
        Settings = new SettingsViewModel(_settingsService)
        {
            IsOpen = true
        };
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        var current = _settingsService.Current.General.Theme;
        var next = current switch
        {
            AppTheme.Dark => AppTheme.Light,
            AppTheme.Light => AppTheme.System,
            _ => AppTheme.Dark
        };

        _settingsService.Update(s => s.General.Theme = next);
        App.ApplyTheme(next);
    }

    private void PopulateCommandPalette()
    {
        var actions = new List<PaletteAction>
        {
            new("New Session", "Session", "Open a new serial connection tab", "Ctrl+N", () => CreateNewSession()),
            new("Close Session", "Session", "Close active serial connection tab", "Ctrl+W", async () => await CloseSessionAsync(SelectedSession)),
            new("Duplicate Session", "Session", "Clone active session configuration", "Ctrl+D", () => DuplicateSession(SelectedSession)),

            new("Connect / Disconnect", "Connection", "Toggle connection for active session", "Ctrl+Shift+C", async () =>
            {
                if (SelectedSession != null)
                {
                    if (SelectedSession.State == ConnectionState.Connected)
                        await SelectedSession.DisconnectAsync();
                    else
                        await SelectedSession.ConnectAsync();
                }
            }),

            new("Clear Terminal", "Terminal", "Clear visible terminal output", "Ctrl+L", () => SelectedSession?.ClearTerminal()),
            new("Toggle Plotter", "Tools", "Show or hide live serial waveform chart", "Ctrl+P", () => SelectedSession?.TogglePlotter()),
            new("Toggle Packet Inspector", "Tools", "Show or hide multi-byte binary inspector", "Ctrl+I", () => SelectedSession?.ToggleInspector()),

            new("Toggle Hex View", "Terminal", "Switch between Text and Hex views", "Ctrl+H", () =>
            {
                if (SelectedSession != null)
                {
                    SelectedSession.TerminalMode = SelectedSession.TerminalMode == TerminalMode.Hex 
                        ? TerminalMode.Text 
                        : TerminalMode.Hex;
                }
            }),

            new("Toggle ANSI Escape Sequences", "Terminal", "Enable or disable ANSI styling interpretation", null, () =>
            {
                if (SelectedSession != null)
                {
                    SelectedSession.InterpretAnsi = !SelectedSession.InterpretAnsi;
                }
            }),

            new("Theme: Dark", "Appearance", "Switch to modern dark theme", null, () =>
            {
                _settingsService.Update(s => s.General.Theme = AppTheme.Dark);
                App.ApplyTheme(AppTheme.Dark);
            }),

            new("Theme: Light", "Appearance", "Switch to clean light theme", null, () =>
            {
                _settingsService.Update(s => s.General.Theme = AppTheme.Light);
                App.ApplyTheme(AppTheme.Light);
            }),

            new("Theme: System", "Appearance", "Follow OS theme setting", null, () =>
            {
                _settingsService.Update(s => s.General.Theme = AppTheme.System);
                App.ApplyTheme(AppTheme.System);
            }),

            new("Open Settings", "Application", "Configure preferences, shortcuts, and serial defaults", "Ctrl+,", () => OpenSettings()),
            new("Open Logs Folder", "Application", "Open diagnostic logs directory", null, () => SettingsViewModel.OpenLogsFolder())
        };

        CommandPalette.RegisterActions(actions);
    }
}
