using Baudr.Core.Buffers;
using Baudr.Core.Models;
using Xunit;

namespace Baudr.Core.Tests;

public class TerminalBufferAndStatsTests
{
    [Fact]
    public void TerminalBuffer_RespectsBoundedCapacity()
    {
        var buffer = new TerminalBuffer(capacity: 5);

        for (int i = 1; i <= 10; i++)
        {
            buffer.Add(new TerminalLine { Text = $"Line {i}" });
        }

        Assert.Equal(5, buffer.Count);
        var snapshot = buffer.GetSnapshot();
        Assert.Equal("Line 6", snapshot[0].Text);
        Assert.Equal("Line 10", snapshot[4].Text);
    }

    [Fact]
    public void TerminalBuffer_Clear_EmptiesBuffer()
    {
        var buffer = new TerminalBuffer(capacity: 10);
        buffer.Add(new TerminalLine { Text = "Line 1" });
        buffer.Add(new TerminalLine { Text = "Line 2" });

        bool clearedEventFired = false;
        buffer.Cleared += () => clearedEventFired = true;

        buffer.Clear();
        Assert.Equal(0, buffer.Count);
        Assert.True(clearedEventFired);
    }

    [Fact]
    public void SessionStatistics_TracksBytesAndLines()
    {
        var stats = new SessionStatistics();
        stats.OnConnected();

        stats.AddRx(1024, 10);
        stats.AddTx(256, 2);

        Assert.Equal(1024, stats.RxBytes);
        Assert.Equal(256, stats.TxBytes);
        Assert.Equal(10, stats.RxLines);
        Assert.Equal(2, stats.TxLines);

        stats.Reset();
        Assert.Equal(0, stats.RxBytes);
        Assert.Equal(0, stats.TxBytes);
        Assert.Equal(0, stats.RxLines);
        Assert.Equal(0, stats.TxLines);
    }
}

