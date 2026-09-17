using System.Text;
using Baudr.Core.Buffers;
using Baudr.Core.Models;
using Baudr.Infrastructure.Serial;
using Xunit;

namespace Baudr.IntegrationTests;

public class MockSerialPipelineTests
{
    [Fact]
    public async Task Pipeline_HighThroughputData_BatchesAndFillsTerminalBuffer()
    {
        var buffer = new TerminalBuffer(capacity: 1000);
        var stats = new SessionStatistics();
        await using var processor = new RxBatchProcessor(buffer, stats, batchIntervalMs: 15);

        int plotCount = 0;
        processor.OnPlotPointsExtracted = points =>
        {
            Interlocked.Add(ref plotCount, points.Count);
        };

        await using var mockPort = new MockSerialPort();
        await mockPort.OpenAsync(new SerialPortConfig { PortName = "MOCK_PORT" });
        stats.OnConnected();

        // Feed 500 lines of telemetry
        for (int i = 1; i <= 500; i++)
        {
            var lineBytes = Encoding.UTF8.GetBytes($"temp={20.0 + (i % 10)} humidity={50 + (i % 20)}\n");
            processor.Enqueue(lineBytes, Direction.Rx);
        }

        // Wait a short moment for background batch timer to drain
        await Task.Delay(100);
        processor.FlushRemaining();

        Assert.Equal(500, buffer.Count);
        Assert.Equal(500, stats.RxLines);
        Assert.True(stats.RxBytes > 0);
        Assert.Equal(1000, plotCount); // 2 data points per line (temp, humidity)
    }

    [Fact]
    public async Task Pipeline_SimulatedDisconnect_HandledGracefully()
    {
        await using var mockPort = new MockSerialPort();
        await mockPort.OpenAsync(new SerialPortConfig { PortName = "MOCK_PORT" });
        Assert.True(mockPort.IsOpen);

        // Disconnect unexpectedly
        mockPort.TriggerDisconnect();
        Assert.False(mockPort.IsOpen);

        // Attempting to read should throw IOException which caller handles as device disconnect
        var buffer = new byte[64];
        await Assert.ThrowsAsync<IOException>(async () =>
        {
            await mockPort.ReadAsync(buffer);
        });
    }
}

