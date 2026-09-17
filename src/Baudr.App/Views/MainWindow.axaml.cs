using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Baudr.App.ViewModels;

namespace Baudr.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        KeyDown += OnMainWindowKeyDown;
        Closing += OnMainWindowClosing;
        Loaded += OnMainWindowLoaded;
    }

    private void OnMainWindowLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Restore window state if available in settings
        var s = App.SettingsService.Current.Window;
        if (s.Width > 400 && s.Height > 300)
        {
            Width = s.Width;
            Height = s.Height;
        }
        if (s.X.HasValue && s.Y.HasValue)
        {
            Position = new Avalonia.PixelPoint((int)s.X.Value, (int)s.Y.Value);
        }
        if (s.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // Remember window state
        App.SettingsService.Update(s =>
        {
            s.Window.IsMaximized = WindowState == WindowState.Maximized;
            if (WindowState != WindowState.Maximized)
            {
                s.Window.Width = Width;
                s.Window.Height = Height;
                s.Window.X = Position.X;
                s.Window.Y = Position.Y;
            }
        });
    }

    private void OnMainWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        bool isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var primaryMod = isMac ? KeyModifiers.Meta : KeyModifiers.Control;

        // Command Palette (Ctrl/Cmd + K)
        if (e.KeyModifiers.HasFlag(primaryMod) && e.Key == Key.K)
        {
            vm.OpenCommandPalette();
            e.Handled = true;
            return;
        }

        // New Session (Ctrl/Cmd + N)
        if (e.KeyModifiers.HasFlag(primaryMod) && e.Key == Key.N)
        {
            vm.CreateNewSession();
            e.Handled = true;
            return;
        }

        // Close Session (Ctrl/Cmd + W)
        if (e.KeyModifiers.HasFlag(primaryMod) && e.Key == Key.W)
        {
            _ = vm.CloseSessionAsync(vm.SelectedSession);
            e.Handled = true;
            return;
        }

        // Settings (Ctrl/Cmd + ,)
        if (e.KeyModifiers.HasFlag(primaryMod) && e.Key == Key.OemComma)
        {
            vm.OpenSettings();
            e.Handled = true;
            return;
        }

        var session = vm.SelectedSession;
        if (session == null) return;

        // Connect / Disconnect (Ctrl/Cmd + Shift + C)
        if (e.KeyModifiers.HasFlag(primaryMod) && e.KeyModifiers.HasFlag(KeyModifiers.Shift) && e.Key == Key.C)
        {
            if (session.State == Baudr.Core.Models.ConnectionState.Connected)
                _ = session.DisconnectAsync();
            else
                _ = session.ConnectAsync();
            e.Handled = true;
            return;
        }

        // Clear Terminal (Ctrl/Cmd + L)
        if (e.KeyModifiers.HasFlag(primaryMod) && e.Key == Key.L)
        {
            session.ClearTerminal();
            e.Handled = true;
            return;
        }

        // Toggle Search (Ctrl/Cmd + F)
        if (e.KeyModifiers.HasFlag(primaryMod) && e.Key == Key.F)
        {
            session.IsSearchVisible = !session.IsSearchVisible;
            e.Handled = true;
            return;
        }

        // Toggle Plotter (Ctrl/Cmd + P)
        if (e.KeyModifiers.HasFlag(primaryMod) && e.Key == Key.P)
        {
            session.TogglePlotter();
            e.Handled = true;
            return;
        }

        // Toggle Packet Inspector (Ctrl/Cmd + I)
        if (e.KeyModifiers.HasFlag(primaryMod) && e.Key == Key.I)
        {
            session.ToggleInspector();
            e.Handled = true;
            return;
        }

        // Font size zoom
        if (e.KeyModifiers.HasFlag(primaryMod))
        {
            if (e.Key == Key.OemPlus || e.Key == Key.Add)
            {
                session.ZoomIn();
                e.Handled = true;
            }
            else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            {
                session.ZoomOut();
                e.Handled = true;
            }
            else if (e.Key == Key.D0 || e.Key == Key.NumPad0)
            {
                session.ResetZoom();
                e.Handled = true;
            }
        }
    }
}

