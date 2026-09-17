using Baudr.Core.Parsing;
using Xunit;

namespace Baudr.Core.Tests;

public class PlotDataParserTests
{
    [Fact]
    public void Parse_SingleNumber_ReturnsSinglePoint()
    {
        var now = DateTimeOffset.UtcNow;
        var points = PlotDataParser.ParseLine("42.5", now, 1);

        Assert.Single(points);
        Assert.Equal(42.5, points[0].Value);
        Assert.Equal("Series 1", points[0].SeriesName);
    }

    [Fact]
    public void Parse_CsvValues_ReturnsMultipleSeries()
    {
        var now = DateTimeOffset.UtcNow;
        var points = PlotDataParser.ParseLine("23.4, 55.1, 100", now, 1);

        Assert.Equal(3, points.Count);
        Assert.Equal(23.4, points[0].Value);
        Assert.Equal(55.1, points[1].Value);
        Assert.Equal(100.0, points[2].Value);
        Assert.Equal("Series 1", points[0].SeriesName);
        Assert.Equal("Series 2", points[1].SeriesName);
        Assert.Equal("Series 3", points[2].SeriesName);
    }

    [Fact]
    public void Parse_KeyValueTelemetry_ReturnsNamedSeries()
    {
        var now = DateTimeOffset.UtcNow;
        var points = PlotDataParser.ParseLine("temp=23.4 humidity:55.1", now, 1);

        Assert.Equal(2, points.Count);
        Assert.Equal("temp", points[0].SeriesName);
        Assert.Equal(23.4, points[0].Value);
        Assert.Equal("humidity", points[1].SeriesName);
        Assert.Equal(55.1, points[1].Value);
    }

    [Fact]
    public void Parse_JsonTelemetry_ReturnsNamedSeries()
    {
        var now = DateTimeOffset.UtcNow;
        var points = PlotDataParser.ParseLine("{\"voltage\": 3.31, \"current\": 0.45}", now, 1);

        Assert.Equal(2, points.Count);
        Assert.Equal("voltage", points[0].SeriesName);
        Assert.Equal(3.31, points[0].Value);
        Assert.Equal("current", points[1].SeriesName);
        Assert.Equal(0.45, points[1].Value);
    }

    [Fact]
    public void Parse_NonNumericText_ReturnsEmpty()
    {
        var now = DateTimeOffset.UtcNow;
        var points = PlotDataParser.ParseLine("Ready to connect", now, 1);
        Assert.Empty(points);
    }
}

