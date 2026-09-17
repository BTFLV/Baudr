using Baudr.Core.Models;
using Xunit;

namespace Baudr.Core.Tests;

public class SerialPortConfigTests
{
    [Fact]
    public void ValidConfig_SucceedsValidation()
    {
        var config = new SerialPortConfig
        {
            PortName = "COM3",
            BaudRate = 115200,
            DataBits = 8,
            Parity = SerialParity.None,
            StopBits = SerialStopBits.One
        };

        Assert.True(config.IsValid(out var error));
        Assert.Null(error);
        Assert.Equal("115200 8N1", config.Shorthand);
    }

    [Theory]
    [InlineData("", 115200, 8, "Port name cannot be empty.")]
    [InlineData("COM1", 0, 8, "Baud rate must be greater than 0.")]
    [InlineData("COM1", -9600, 8, "Baud rate must be greater than 0.")]
    [InlineData("COM1", 9600, 4, "Data bits must be between 5 and 8.")]
    [InlineData("COM1", 9600, 9, "Data bits must be between 5 and 8.")]
    public void InvalidConfig_ReturnsAppropriateError(string port, int baud, int dataBits, string expectedErrorSubstring)
    {
        var config = new SerialPortConfig
        {
            PortName = port,
            BaudRate = baud,
            DataBits = dataBits
        };

        Assert.False(config.IsValid(out var error));
        Assert.NotNull(error);
        Assert.Contains(expectedErrorSubstring, error);
    }

    [Fact]
    public void StandardBaudRates_ContainsExpectedPresets()
    {
        Assert.Contains(9600, SerialPortConfig.StandardBaudRates);
        Assert.Contains(115200, SerialPortConfig.StandardBaudRates);
        Assert.Contains(921600, SerialPortConfig.StandardBaudRates);
        Assert.Contains(2000000, SerialPortConfig.StandardBaudRates);
    }
}

