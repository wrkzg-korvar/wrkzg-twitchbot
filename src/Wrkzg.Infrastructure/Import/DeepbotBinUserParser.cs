using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Infrastructure.Import;

/// <summary>
/// Parses DeepBot users*.bin files (gzip-compressed protobuf).
/// Extracts username, points, watched minutes, display name, and Twitch ID.
/// </summary>
public static class DeepbotBinUserParser
{
    /// <summary>
    /// Parses a gzip-compressed protobuf stream of DeepBot user records.
    /// </summary>
    /// <param name="stream">The gzip-compressed input stream.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of parsed user records.</returns>
    public static async Task<List<ImportUserRecord>> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        byte[] decompressed = await DecompressGzipAsync(stream, ct);
        return ParseProtobuf(decompressed);
    }

    private static async Task<byte[]> DecompressGzipAsync(Stream stream, CancellationToken ct)
    {
        using GZipStream gzip = new(stream, CompressionMode.Decompress);
        using MemoryStream ms = new();
        await gzip.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    private static List<ImportUserRecord> ParseProtobuf(byte[] data)
    {
        List<ImportUserRecord> records = new();
        int offset = 0;
        int recordNumber = 0;

        while (offset < data.Length)
        {
            (int fieldNumber, int wireType, int bytesRead) = ReadTag(data, offset);
            offset += bytesRead;

            if (fieldNumber == 1 && wireType == 2)
            {
                // Length-delimited: UserRecord
                (int length, int lenBytes) = ReadVarint32(data, offset);
                offset += lenBytes;

                if (offset + length > data.Length)
                {
                    break;
                }

                recordNumber++;
                ReadOnlySpan<byte> recordSpan = data.AsSpan(offset, length);
                ImportUserRecord? record = ParseUserRecord(recordSpan, recordNumber);
                if (record is not null)
                {
                    records.Add(record);
                }

                offset += length;
            }
            else
            {
                // Skip unknown field
                offset = SkipField(data, offset, wireType);
                if (offset < 0)
                {
                    break;
                }
            }
        }

        return records;
    }

    private static ImportUserRecord? ParseUserRecord(ReadOnlySpan<byte> data, int recordNumber)
    {
        string username = string.Empty;
        double points = 0;
        int watchedMinutes = 0;
        string? displayName = null;
        long twitchId = 0;

        int offset = 0;
        while (offset < data.Length)
        {
            (int fieldNumber, int wireType, int bytesRead) = ReadTagSpan(data, offset);
            offset += bytesRead;

            switch (wireType)
            {
                case 0: // Varint
                {
                    (long value, int varBytes) = ReadVarint64Span(data, offset);
                    offset += varBytes;

                    if (fieldNumber == 9)
                    {
                        watchedMinutes = value >= 0 ? (int)Math.Min(value, int.MaxValue) : 0;
                    }
                    else if (fieldNumber == 20)
                    {
                        twitchId = value;
                    }

                    break;
                }
                case 1: // 64-bit (double)
                {
                    if (offset + 8 > data.Length)
                    {
                        return null;
                    }

                    if (fieldNumber == 8)
                    {
                        points = BinaryPrimitives.ReadDoubleLittleEndian(data.Slice(offset, 8));
                        if (points < 0)
                        {
                            points = 0;
                        }
                    }

                    offset += 8;
                    break;
                }
                case 2: // Length-delimited (string or embedded message)
                {
                    (int length, int lenBytes) = ReadVarint32Span(data, offset);
                    offset += lenBytes;

                    if (offset + length > data.Length)
                    {
                        return null;
                    }

                    if (fieldNumber == 1)
                    {
                        username = Encoding.UTF8.GetString(data.Slice(offset, length)).ToLowerInvariant();
                    }
                    else if (fieldNumber == 19)
                    {
                        displayName = Encoding.UTF8.GetString(data.Slice(offset, length));
                    }

                    offset += length;
                    break;
                }
                case 5: // 32-bit
                {
                    offset += 4;
                    break;
                }
                default:
                {
                    return null;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return new ImportUserRecord
        {
            Username = username,
            Points = (long)Math.Round(points),
            WatchedMinutes = watchedMinutes,
            DisplayName = displayName,
            TwitchId = twitchId > 0 ? twitchId.ToString() : null,
            LineNumber = recordNumber
        };
    }

    private static (int fieldNumber, int wireType, int bytesRead) ReadTag(byte[] data, int offset)
    {
        (int tag, int bytes) = ReadVarint32(data, offset);
        return (tag >> 3, tag & 0x07, bytes);
    }

    private static (int fieldNumber, int wireType, int bytesRead) ReadTagSpan(ReadOnlySpan<byte> data, int offset)
    {
        (int tag, int bytes) = ReadVarint32Span(data, offset);
        return (tag >> 3, tag & 0x07, bytes);
    }

    private static (int value, int bytesRead) ReadVarint32(byte[] data, int offset)
    {
        int result = 0;
        int shift = 0;
        int bytesRead = 0;

        while (offset + bytesRead < data.Length)
        {
            byte b = data[offset + bytesRead];
            bytesRead++;
            result |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return (result, bytesRead);
            }

            shift += 7;
            if (shift >= 35)
            {
                break;
            }
        }

        return (result, bytesRead);
    }

    private static (int value, int bytesRead) ReadVarint32Span(ReadOnlySpan<byte> data, int offset)
    {
        int result = 0;
        int shift = 0;
        int bytesRead = 0;

        while (offset + bytesRead < data.Length)
        {
            byte b = data[offset + bytesRead];
            bytesRead++;
            result |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return (result, bytesRead);
            }

            shift += 7;
            if (shift >= 35)
            {
                break;
            }
        }

        return (result, bytesRead);
    }

    private static (long value, int bytesRead) ReadVarint64Span(ReadOnlySpan<byte> data, int offset)
    {
        long result = 0;
        int shift = 0;
        int bytesRead = 0;

        while (offset + bytesRead < data.Length)
        {
            byte b = data[offset + bytesRead];
            bytesRead++;
            result |= (long)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return (result, bytesRead);
            }

            shift += 7;
            if (shift >= 70)
            {
                break;
            }
        }

        return (result, bytesRead);
    }

    private static int SkipField(byte[] data, int offset, int wireType)
    {
        switch (wireType)
        {
            case 0: // Varint
            {
                while (offset < data.Length)
                {
                    if ((data[offset++] & 0x80) == 0)
                    {
                        return offset;
                    }
                }

                return -1;
            }
            case 1: // 64-bit
                return offset + 8;
            case 2: // Length-delimited
            {
                (int length, int lenBytes) = ReadVarint32(data, offset);
                return offset + lenBytes + length;
            }
            case 5: // 32-bit
                return offset + 4;
            default:
                return -1;
        }
    }
}
