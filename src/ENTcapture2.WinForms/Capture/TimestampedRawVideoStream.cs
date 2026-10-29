using System.Buffers.Binary;
using System.Text;

namespace ENTcapture2.WinForms.Capture;

// Minimal streaming Matroska transport for BGR24 + explicit capture timestamps.
// Rawvideo stdin has no place for per-frame timestamps; this transport does.
internal sealed class TimestampedRawVideoStream
{
    private readonly Stream _stream;

    public TimestampedRawVideoStream(Stream stream, int width, int height, double fps)
    {
        _stream = stream;
        WriteElement(stream, 0x1A45DFA3, Elements(
            UInt(0x4286, 1), UInt(0x42F7, 1), UInt(0x42F2, 4), UInt(0x42F3, 8),
            Text(0x4282, "matroska"), UInt(0x4287, 4), UInt(0x4285, 2)));
        WriteId(stream, 0x18538067); // Segment with unknown length (live stream).
        stream.Write([0x01, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff]);
        WriteElement(stream, 0x1549A966, Elements(
            UInt(0x2AD7B1, 1_000_000), Text(0x4D80, "ENTcapture2"), Text(0x5741, "ENTcapture2")));
        byte[] bitmapInfo = new byte[40];
        BinaryPrimitives.WriteInt32LittleEndian(bitmapInfo, 40);
        BinaryPrimitives.WriteInt32LittleEndian(bitmapInfo.AsSpan(4), width);
        BinaryPrimitives.WriteInt32LittleEndian(bitmapInfo.AsSpan(8), -height); // top-down BGR
        BinaryPrimitives.WriteUInt16LittleEndian(bitmapInfo.AsSpan(12), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(bitmapInfo.AsSpan(14), 24);
        WriteElement(stream, 0x1654AE6B, Element(0xAE, Elements(
            UInt(0xD7, 1), UInt(0x73C5, 1), UInt(0x83, 1),
            Text(0x86, "V_MS/VFW/FOURCC"), Element(0x63A2, bitmapInfo),
            UInt(0x23E383, (ulong)Math.Round(1_000_000_000 / fps)),
            Element(0xE0, Elements(UInt(0xB0, (ulong)width), UInt(0xBA, (ulong)height))))));
    }

    public void WriteFrame(ReadOnlySpan<byte> pixels, long milliseconds)
    {
        byte[] timecode = UInt(0xE7, (ulong)milliseconds);
        // One cluster per frame avoids signed 16-bit relative-timecode limits.
        WriteId(_stream, 0x1F43B675);
        WriteSize(_stream, (ulong)(timecode.Length + 1 + SizeLength((ulong)pixels.Length + 4) + pixels.Length + 4));
        _stream.Write(timecode);
        WriteId(_stream, 0xA3);
        WriteSize(_stream, (ulong)pixels.Length + 4);
        _stream.Write([0x81, 0, 0, 0x80]); // track 1, relative time 0, keyframe
        _stream.Write(pixels);
    }

    private static byte[] UInt(uint id, ulong value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        int start = 0;
        while (start < 7 && bytes[start] == 0) start++;
        return Element(id, bytes[start..].ToArray());
    }

    private static byte[] Text(uint id, string text) => Element(id, Encoding.UTF8.GetBytes(text));
    private static byte[] Element(uint id, byte[] data)
    {
        using var stream = new MemoryStream();
        WriteElement(stream, id, data);
        return stream.ToArray();
    }
    private static byte[] Elements(params byte[][] elements) => elements.SelectMany(item => item).ToArray();
    private static void WriteElement(Stream stream, uint id, byte[] data)
    {
        WriteId(stream, id);
        WriteSize(stream, (ulong)data.Length);
        stream.Write(data);
    }
    private static void WriteId(Stream stream, uint id)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, id);
        int start = 0;
        while (start < 3 && bytes[start] == 0) start++;
        stream.Write(bytes[start..]);
    }
    private static int SizeLength(ulong size)
    {
        int length = 1;
        while (length < 8 && size >= (1UL << (7 * length)) - 1) length++;
        return length;
    }
    private static void WriteSize(Stream stream, ulong size)
    {
        int length = SizeLength(size);
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, size | (1UL << (7 * length)));
        stream.Write(bytes[(8 - length)..]);
    }
}
