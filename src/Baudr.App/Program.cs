using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Avalonia;
using Baudr.Core.Models;
using Baudr.Infrastructure.Persistence;
using Baudr.Infrastructure.Serial;

namespace Baudr.App;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        // Non-interactive package sanity verification mode
        if (args.Length >= 2 && args[0].Equals("--verify-package", StringComparison.OrdinalIgnoreCase))
        {
            return RunPackageVerification(args[1]);
        }

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal startup error: {ex}");
            return 1;
        }
    }

    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };

    private static int RunPackageVerification(string outputPath)
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var ver = asm.GetName().Version?.ToString() ?? "0.1.0-dev";
            var infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";

            // Test subsystem sanity (without touching hardware)
            var enumerator = new SystemSerialPortEnumerator();
            var ports = enumerator.GetAvailablePorts();

            var testSettings = new AppSettings();
            var json = JsonSerializer.Serialize(testSettings, BaudrJsonContext.Default.AppSettings);

            var report = new
            {
                status = "passed",
                application = "Baudr",
                version = ver,
                informationalVersion = infoVer,
                os = RuntimeInformation.OSDescription,
                architecture = RuntimeInformation.OSArchitecture.ToString(),
                framework = RuntimeInformation.FrameworkDescription,
                isAot = !System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported,
                detectedPortsCount = ports.Count,
                settingsSerializationSuccess = !string.IsNullOrEmpty(json),
                timestamp = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };

            var reportJson = JsonSerializer.Serialize(report, IndentedJsonOptions);

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(outputPath, reportJson);
            Console.WriteLine($"Package verification passed. Report written to {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Package verification failed: {ex}");
            try
            {
                var errorReport = new
                {
                    status = "failed",
                    error = ex.ToString(),
                    timestamp = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                };
                File.WriteAllText(outputPath, JsonSerializer.Serialize(errorReport));
            }
            catch
            {
                // Ignore secondary error
            }
            return 1;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
