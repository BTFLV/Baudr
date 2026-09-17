using System.Globalization;

namespace Baudr.Infrastructure.Diagnostics;

public static class DiagnosticLog
{
    private static readonly object Lock = new();
    private static string? _logFilePath;

    public static string GetLogsDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "Baudr", "logs");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string GetLogFilePath()
    {
        if (_logFilePath == null)
        {
            var dir = GetLogsDirectory();
            var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            _logFilePath = Path.Combine(dir, $"baudr_{dateStr}.log");
        }
        return _logFilePath;
    }

    public static void Info(string message) => WriteEntry("INFO", message);
    public static void Warn(string message) => WriteEntry("WARN", message);
    public static void Error(string message, Exception? ex = null)
    {
        var msg = ex != null ? $"{message} | Exception: {ex.GetType().Name}: {ex.Message}" : message;
        WriteEntry("ERROR", msg);
    }

    private static void WriteEntry(string level, string message)
    {
        try
        {
            var path = GetLogFilePath();
            var ts = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var line = $"[{ts}] [{level}] {message}{Environment.NewLine}";

            lock (Lock)
            {
                File.AppendAllText(path, line);
            }
        }
        catch
        {
            // Diagnostics should never crash the host application
        }
    }
}

