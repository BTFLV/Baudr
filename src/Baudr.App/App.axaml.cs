using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Baudr.App.ViewModels;
using Baudr.App.Views;
using Baudr.Core.Models;
using Baudr.Infrastructure.Persistence;

namespace Baudr.App;

public partial class App : Application
{
    private static SettingsService? _settingsService;
    public static SettingsService SettingsService => _settingsService ??= new SettingsService();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            SettingsService.Load();
            ApplyTheme(SettingsService.Current.General.Theme);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainVm = new MainWindowViewModel(SettingsService);
                var mainWindow = new MainWindow
                {
                    DataContext = mainVm
                };
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Baudr", "crash.log");
            try
            {
                var dir = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(logPath, $"{DateTime.UtcNow:O} INIT ERROR: {ex}\n");
            }
            catch
            {
                // Ignore secondary logging failure
            }
            throw;
        }
    }

    public static void ApplyTheme(AppTheme theme)
    {
        if (Current == null) return;

        Current.RequestedThemeVariant = theme switch
        {
            AppTheme.Dark => ThemeVariant.Dark,
            AppTheme.Light => ThemeVariant.Light,
            _ => ThemeVariant.Default
        };
    }
}

