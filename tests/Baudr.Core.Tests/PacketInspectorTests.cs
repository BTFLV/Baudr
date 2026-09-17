using Baudr.Core.Parsing;
using Xunit;

namespace Baudr.Core.Tests;

public class PacketInspectorTests
{
    [Fact]
    public void Inspect_4Bytes_DecodesAllRepresentations()
    {
        // 0x01, 0x02, 0x03, 0x04
        byte[] data = [0x01, 0x02, 0x03, 0x04];
        var result = PacketInspector.Inspect(data);

        Assert.Equal(4, result.Length);
        Assert.Equal("01 02 03 04", result.HexString);
        Assert.Equal((byte)1, result.UInt8);
        Assert.Equal((sbyte)1, result.Int8);

        // UInt16 LE = 0x0201 = 513
        Assert.Equal((ushort)0x0201, result.UInt16LE);
        // UInt16 BE = 0x0102 = 258
        Assert.Equal((ushort)0x0102, result.UInt16BE);

        // UInt32 LE = 0x04030201 = 67305985
        Assert.Equal((uint)0x04030201, result.UInt32LE);
        // UInt32 BE = 0x01020304 = 16909060
        Assert.Equal((uint)0x01020304, result.UInt32BE);

        Assert.NotNull(result.Float32LE);
        Assert.NotNull(result.Float32BE);
        Assert.Equal(4, result.BinaryBits.Split(' ').Length);
    }

    [Fact]
    public void Inspect_PartialData_NeverInterpretsBeyondAvailableBytes()
    {
        byte[] data = [0x42];
        var result = PacketInspector.Inspect(data);

        Assert.Equal(1, result.Length);
        Assert.Equal((byte)0x42, result.UInt8);
        Assert.Equal("B", result.AsciiString);
        Assert.Null(result.UInt16LE);
        Assert.Null(result.UInt16BE);
        Assert.Null(result.UInt32LE);
        Assert.Null(result.Float32LE);
    }

    [Fact]
    public void Inspect_Empty_ReturnsZeroLength()
    {
        var result = PacketInspector.Inspect([]);
        Assert.Equal(0, result.Length);
    }
}

