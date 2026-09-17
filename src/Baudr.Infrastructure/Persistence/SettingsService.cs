using System.Globalization;
using System.Text.Json;
using Baudr.Core.Abstractions;
using Baudr.Core.Models;

namespace Baudr.Infrastructure.Persistence;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly object _lock = new();
    private AppSettings _current = new();

    public AppSettings Current
    {
        get { lock (_lock) return _current; }
        private set { lock (_lock) _current = value; }
    }

    public event EventHandler<AppSettings>? SettingsChanged;

    public SettingsService(string? customFilePath = null)
    {
        if (!string.IsNullOrWhiteSpace(customFilePath))
        {
            _settingsFilePath = customFilePath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "Baudr");
            Directory.CreateDirectory(dir);
            _settingsFilePath = Path.Combine(dir, "settings.json");
        }
    }

    public void Load()
    {
        if (!File.Exists(_settingsFilePath))
        {
            Current = new AppSettings();
            Save();
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            var loaded = JsonSerializer.Deserialize(json, BaudrJsonContext.Default.AppSettings);
            Current = loaded ?? new AppSettings();
        }
        catch (Exception ex)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                var backupPath = $"{_settingsFilePath}.corrupted_{timestamp}.bak";
                File.Copy(_settingsFilePath, backupPath, overwrite: true);
            }
            catch
            {
                // Ignore backup copy failure
            }

            Current = new AppSettings();
            Save();
            System.Diagnostics.Debug.WriteLine($"Settings file corrupted, recovered defaults: {ex.Message}");
        }
    }

    public void Save()
    {
        string json;
        lock (_lock)
        {
            json = JsonSerializer.Serialize(_current, BaudrJsonContext.Default.AppSettings);
        }

        var dir = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var tempFile = $"{_settingsFilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(tempFile, json);
            File.Move(tempFile, _settingsFilePath, overwrite: true);
        }
        catch
        {
            try
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
            catch
            {
                // Ignore cleanup error
            }
            throw;
        }

        SettingsChanged?.Invoke(this, Current);
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(_settingsFilePath))
        {
            Current = new AppSettings();
            await SaveAsync().ConfigureAwait(false);
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsFilePath).ConfigureAwait(false);
            var loaded = JsonSerializer.Deserialize(json, BaudrJsonContext.Default.AppSettings);
            Current = loaded ?? new AppSettings();
        }
        catch (Exception ex)
        {
            // Corrupted file recovery: back up bad file and restore defaults
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                var backupPath = $"{_settingsFilePath}.corrupted_{timestamp}.bak";
                File.Copy(_settingsFilePath, backupPath, overwrite: true);
            }
            catch
            {
                // Ignore backup copy failure
            }

            Current = new AppSettings();
            await SaveAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"Settings file corrupted, recovered defaults: {ex.Message}");
        }
    }

    public async Task SaveAsync()
    {
        string json;
        lock (_lock)
        {
            json = JsonSerializer.Serialize(_current, BaudrJsonContext.Default.AppSettings);
        }

        var dir = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Atomic write via temporary file
        var tempFile = $"{_settingsFilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(tempFile, json).ConfigureAwait(false);
            File.Move(tempFile, _settingsFilePath, overwrite: true);
        }
        catch
        {
            try
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
            catch
            {
                // Ignore cleanup error
            }
            throw;
        }

        SettingsChanged?.Invoke(this, Current);
    }

    public void Update(Action<AppSettings> updateAction)
    {
        ArgumentNullException.ThrowIfNull(updateAction);
        lock (_lock)
        {
            updateAction(_current);
        }
        _ = SaveAsync();
    }
}

