using Baudr.Core.Models;
using Baudr.Core.Parsing;
using Xunit;

namespace Baudr.Core.Tests;

public class AnsiParserTests
{
    [Fact]
    public void Parse_PlainTextWithoutEscape_ReturnsCleanStringWithoutSpans()
    {
        var parser = new AnsiParser();
        var (clean, spans) = parser.Parse("Hello Embedded World!");

        Assert.Equal("Hello Embedded World!", clean);
        Assert.Empty(spans);
    }

    [Fact]
    public void Parse_StandardColors_CreatesColoredSpans()
    {
        var parser = new AnsiParser();
        // \x1b[31mRed\x1b[0mNormal\x1b[32mGreen\x1b[0m
        var raw = "\x1b[31mRed\x1b[0mNormal\x1b[32mGreen\x1b[0m";
        var (clean, spans) = parser.Parse(raw);

        Assert.Equal("RedNormalGreen", clean);
        Assert.Equal(2, spans.Count);

        // First span: "Red"
        Assert.Equal(0, spans[0].Start);
        Assert.Equal(3, spans[0].Length);
        Assert.Equal(new ColorRgb(205, 49, 49), spans[0].Foreground);

        // Second span: "Green"
        Assert.Equal(9, spans[1].Start);
        Assert.Equal(5, spans[1].Length);
        Assert.Equal(new ColorRgb(13, 188, 121), spans[1].Foreground);
    }

    [Fact]
    public void Parse_24BitTrueColor_ParsesRgb()
    {
        var parser = new AnsiParser();
        // ESC[38;2;100;150;200m
        var raw = "\x1b[38;2;100;150;200mCustomRGB\x1b[0m";
        var (clean, spans) = parser.Parse(raw);

        Assert.Equal("CustomRGB", clean);
        Assert.Single(spans);
        Assert.Equal(new ColorRgb(100, 150, 200), spans[0].Foreground);
    }

    [Fact]
    public void Parse_BoldAndUnderline_SetsFlags()
    {
        var parser = new AnsiParser();
        var raw = "\x1b[1;4mImportant\x1b[0m";
        var (clean, spans) = parser.Parse(raw);

        Assert.Equal("Important", clean);
        Assert.Single(spans);
        Assert.True(spans[0].Bold);
        Assert.True(spans[0].Underline);
    }

    [Fact]
    public void Parse_Backspace_OverwritesPreviousChar()
    {
        var parser = new AnsiParser();
        var raw = "abc\b\bxy";
        var (clean, _) = parser.Parse(raw);

        Assert.Equal("axy", clean);
    }

    [Fact]
    public void Parse_MalformedEscapeSequence_RecoversGracefully()
    {
        var parser = new AnsiParser();
        var raw = "Broken\x1b[??99xyzNormal";
        var (clean, _) = parser.Parse(raw);

        Assert.Contains("Broken", clean);
        Assert.Contains("Normal", clean);
    }
}

