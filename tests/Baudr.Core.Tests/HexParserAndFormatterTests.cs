using Baudr.Core.Parsing;
using Xunit;

namespace Baudr.Core.Tests;

public class HexParserAndFormatterTests
{
    [Fact]
    public void Parse_SpacedHex_ReturnsCorrectBytes()
    {
        var input = "01 FF A0 0D 0A";
        Assert.True(HexParser.TryParse(input, out var bytes, out var error));
        Assert.Null(error);
        Assert.Equal([0x01, 0xFF, 0xA0, 0x0D, 0x0A], bytes);
    }

    [Fact]
    public void Parse_ContinuousHex_ReturnsCorrectBytes()
    {
        var input = "01FFA00D0A";
        Assert.True(HexParser.TryParse(input, out var bytes, out var error));
        Assert.Null(error);
        Assert.Equal([0x01, 0xFF, 0xA0, 0x0D, 0x0A], bytes);
    }

    [Fact]
    public void Parse_PrefixedHexWithDelimiters_ReturnsCorrectBytes()
    {
        var input = "0x01, 0xFF, \\xA0, 0x0D, 0x0A";
        Assert.True(HexParser.TryParse(input, out var bytes, out var error));
        Assert.Null(error);
        Assert.Equal([0x01, 0xFF, 0xA0, 0x0D, 0x0A], bytes);
    }

    [Fact]
    public void Parse_OddLength_ReturnsError()
    {
        var input = "01FFA00D0";
        Assert.False(HexParser.TryParse(input, out var bytes, out var error));
        Assert.NotNull(error);
        Assert.Contains("odd length", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_InvalidHexCharacters_ReturnsError()
    {
        var input = "01 ZZ A0";
        Assert.False(HexParser.TryParse(input, out var bytes, out var error));
        Assert.NotNull(error);
        Assert.Contains("Invalid hex byte", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Formatter_ToHexString_OutputsFormattedString()
    {
        byte[] bytes = [0x01, 0x02, 0xFE, 0xFF];
        var formatted = HexFormatter.ToHexString(bytes, " ");
        Assert.Equal("01 02 FE FF", formatted);
    }

    [Fact]
    public void Formatter_ToHexDump_OutputsOffsetAndAsciiGutter()
    {
        byte[] bytes = [0x48, 0x65, 0x6C, 0x6C, 0x6F]; // "Hello"
        var dump = HexFormatter.ToHexDump(bytes, 16);
        Assert.Contains("00000000", dump);
        Assert.Contains("48 65 6C 6C 6F", dump);
        Assert.Contains("|Hello|", dump);
    }
}

