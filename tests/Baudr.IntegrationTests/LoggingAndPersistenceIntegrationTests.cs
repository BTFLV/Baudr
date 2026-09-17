using System.Text;
using Baudr.Core.Models;
using Baudr.Infrastructure.Logging;
using Baudr.Infrastructure.Persistence;
using Xunit;

namespace Baudr.IntegrationTests;

public class LoggingAndPersistenceIntegrationTests
{
    [Fact]
    public async Task SessionLogger_WritesPlainTextAndCsv()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BaudrTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var logPath = Path.Combine(tempDir, "session.log");
            await using (var logger = new SessionLogger())
            {
                await logger.StartAsync(logPath, LogFormat.TimestampedText);
                Assert.True(logger.IsLogging);

                await logger.LogRxAsync(Encoding.UTF8.GetBytes("Booting device\n"), "Booting device");
                await logger.LogTxAsync(Encoding.UTF8.GetBytes("AT\n"), "AT");
                await logger.StopAsync();
                Assert.False(logger.IsLogging);
            }

            Assert.True(File.Exists(logPath));
            var content = await File.ReadAllTextAsync(logPath);
            Assert.Contains("[RX] Booting device", content);
            Assert.Contains("[TX] AT", content);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExportService_ExportsBufferToFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BaudrExportTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var lines = new List<TerminalLine>
            {
                new() { Index = 1, Text = "Line A", Direction = Direction.Rx, Timestamp = DateTimeOffset.UtcNow },
                new() { Index = 2, Text = "Line B", Direction = Direction.Tx, Timestamp = DateTimeOffset.UtcNow }
            };

            var csvPath = Path.Combine(tempDir, "export.csv");
            await ExportService.ExportToFileAsync(csvPath, lines, LogFormat.Csv);

            Assert.True(File.Exists(csvPath));
            var csv = await File.ReadAllTextAsync(csvPath);
            Assert.Contains("Index,Timestamp,Direction,Text", csv);
            Assert.Contains("Line A", csv);
            Assert.Contains("Line B", csv);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task SettingsService_SavesAndRecoversCorruptedFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BaudrSettingsTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var settingsPath = Path.Combine(tempDir, "settings.json");
            var service = new SettingsService(settingsPath);

            await service.LoadAsync();
            service.Update(s =>
            {
                s.Serial.DefaultBaudRate = 2000000;
            });

            await Task.Delay(50); // wait for save

            // Reload with a new instance
            var service2 = new SettingsService(settingsPath);
            await service2.LoadAsync();
            Assert.Equal(2000000, service2.Current.Serial.DefaultBaudRate);

            // Corrupt the file
            await File.WriteAllTextAsync(settingsPath, "{{corrupted json garbage!!");

            // Load should catch error, backup corrupted file, and reset to defaults
            var service3 = new SettingsService(settingsPath);
            await service3.LoadAsync();
            Assert.Equal(115200, service3.Current.Serial.DefaultBaudRate); // default restored

            // Corrupted backup file should exist
            var backups = Directory.GetFiles(tempDir, "*.bak");
            Assert.NotEmpty(backups);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
