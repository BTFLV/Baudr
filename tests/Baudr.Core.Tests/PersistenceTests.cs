using System.Text.Json;
using Baudr.Core.Models;
using Xunit;

namespace Baudr.Core.Tests;

public class PersistenceTests
{
    [Fact]
    public void AppSettings_HasReasonableDefaults()
    {
        var settings = new AppSettings();

        Assert.Equal(1, settings.SchemaVersion);
        Assert.Equal(AppTheme.Dark, settings.General.Theme);
        Assert.Equal(115200, settings.Serial.DefaultBaudRate);
        Assert.NotEmpty(settings.Profiles);
        Assert.NotEmpty(settings.SavedCommands);
        Assert.NotEmpty(settings.HighlightRules);
    }

    [Fact]
    public void AppSettings_JsonRoundtrip_PreservesData()
    {
        var settings = new AppSettings
        {
            General = new GeneralSettings { Theme = AppTheme.Light },
            Serial = new SerialSettings { DefaultBaudRate = 921600 }
        };

        var json = JsonSerializer.Serialize(settings);
        var deserialized = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(AppTheme.Light, deserialized.General.Theme);
        Assert.Equal(921600, deserialized.Serial.DefaultBaudRate);
    }
}

