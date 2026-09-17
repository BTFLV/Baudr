using System.Buffers.Binary;
using System.Text;

namespace Baudr.Core.Parsing;

public record ByteSpanInterpretation
{
    public int Length { get; init; }
    public string HexString { get; init; } = string.Empty;
    public string BinaryBits { get; init; } = string.Empty;
    public string AsciiString { get; init; } = string.Empty;
    public string? Utf8String { get; init; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifiers should not contain type names", Justification = "Standard protocol data type name")]
    public byte? UInt8 { get; init; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifiers should not contain type names", Justification = "Standard protocol data type name")]
    public sbyte? Int8 { get; init; }

    public ushort? UInt16LE { get; init; }
    public ushort? UInt16BE { get; init; }
    public short? Int16LE { get; init; }
    public short? Int16BE { get; init; }

    public uint? UInt32LE { get; init; }
    public uint? UInt32BE { get; init; }
    public int? Int32LE { get; init; }
    public int? Int32BE { get; init; }

    public float? Float32LE { get; init; }
    public float? Float32BE { get; init; }
}

public static class PacketInspector
{
    public static ByteSpanInterpretation Inspect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return new ByteSpanInterpretation { Length = 0 };
        }

        var hex = HexFormatter.ToHexString(bytes, " ");
        var asciiSb = new StringBuilder(bytes.Length);
        var bitsSb = new StringBuilder(bytes.Length * 9);

        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];
            char c = (b >= 32 && b <= 126) ? (char)b : '.';
            asciiSb.Append(c);

            if (i > 0) bitsSb.Append(' ');
            bitsSb.Append(Convert.ToString(b, 2).PadLeft(8, '0'));
        }

        string? utf8 = null;
        try
        {
            var enc = new UTF8Encoding(false, true);
            utf8 = enc.GetString(bytes);
        }
        catch
        {
            utf8 = null;
        }

        byte? u8 = bytes.Length >= 1 ? bytes[0] : null;
        sbyte? s8 = bytes.Length >= 1 ? (sbyte)bytes[0] : null;

        ushort? u16Le = bytes.Length >= 2 ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) : null;
        ushort? u16Be = bytes.Length >= 2 ? BinaryPrimitives.ReadUInt16BigEndian(bytes) : null;
        short? s16Le = bytes.Length >= 2 ? BinaryPrimitives.ReadInt16LittleEndian(bytes) : null;
        short? s16Be = bytes.Length >= 2 ? BinaryPrimitives.ReadInt16BigEndian(bytes) : null;

        uint? u32Le = bytes.Length >= 4 ? BinaryPrimitives.ReadUInt32LittleEndian(bytes) : null;
        uint? u32Be = bytes.Length >= 4 ? BinaryPrimitives.ReadUInt32BigEndian(bytes) : null;
        int? s32Le = bytes.Length >= 4 ? BinaryPrimitives.ReadInt32LittleEndian(bytes) : null;
        int? s32Be = bytes.Length >= 4 ? BinaryPrimitives.ReadInt32BigEndian(bytes) : null;

        float? f32Le = bytes.Length >= 4 ? BinaryPrimitives.ReadSingleLittleEndian(bytes) : null;
        float? f32Be = bytes.Length >= 4 ? BinaryPrimitives.ReadSingleBigEndian(bytes) : null;

        return new ByteSpanInterpretation
        {
            Length = bytes.Length,
            HexString = hex,
            BinaryBits = bitsSb.ToString(),
            AsciiString = asciiSb.ToString(),
            Utf8String = utf8,
            UInt8 = u8,
            Int8 = s8,
            UInt16LE = u16Le,
            UInt16BE = u16Be,
            Int16LE = s16Le,
            Int16BE = s16Be,
            UInt32LE = u32Le,
            UInt32BE = u32Be,
            Int32LE = s32Le,
            Int32BE = s32Be,
            Float32LE = f32Le,
            Float32BE = f32Be
        };
    }
}
