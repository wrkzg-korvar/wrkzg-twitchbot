using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Wrkzg.Infrastructure.Import;

/// <summary>
/// Parses DeepBot chanmsgconfig*.bin save files (.NET BinaryFormatter / MS-NRBF format).
/// Extracts custom commands (NewChanMessage) and quotes (QuoteMessage).
/// </summary>
public static class DeepbotBinConfigParser
{
    /// <summary>Result of parsing a chanmsgconfig file.</summary>
    public record ParseResult(
        List<ImportCommandRecord> Commands,
        List<ImportQuoteRecord> Quotes,
        List<ImportTimedMessageRecord> TimedMessages);

    // MS-NRBF Record Types
    private const byte SerializationHeader = 0x00;
    private const byte ClassWithId = 0x01;
    private const byte ClassWithMembersAndTypes = 0x05;
    private const byte BinaryObjectString = 0x06;
    private const byte MemberReference = 0x09;
    private const byte ObjectNull = 0x0A;
    private const byte MessageEnd = 0x0B;
    private const byte BinaryLibrary = 0x0C;
    private const byte ObjectNullMultiple256 = 0x0E;
    private const byte ArraySingleString = 0x0F;

    // MS-NRBF BinaryTypeEnumeration
    private const byte BinaryType_Primitive = 0;
    private const byte BinaryType_String = 1;
    private const byte BinaryType_Object = 2;
    private const byte BinaryType_SystemClass = 3;
    private const byte BinaryType_Class = 4;

    // MS-NRBF PrimitiveTypeEnumeration → byte size
    private static int PrimitiveSize(byte primitiveType) => primitiveType switch
    {
        1 => 1,   // Boolean
        2 => 1,   // Byte
        3 => 2,   // Char
        5 => 16,  // Decimal
        6 => 8,   // Double
        7 => 2,   // Int16
        8 => 4,   // Int32
        9 => 8,   // Int64
        10 => 1,  // SByte
        11 => 4,  // Single
        12 => 8,  // TimeSpan
        13 => 8,  // DateTime
        14 => 2,  // UInt16
        15 => 4,  // UInt32
        16 => 8,  // UInt64
        _ => 0
    };

    /// <summary>Describes one field in a BinaryFormatter class.</summary>
    private readonly record struct FieldDef(string Name, bool IsString, byte PrimitiveType, int PrimitiveSize);

    /// <summary>Parses a DeepBot chanmsgconfig binary file.</summary>
    public static Task<ParseResult> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        using MemoryStream ms = new();
        stream.CopyTo(ms);
        byte[] data = ms.ToArray();

        List<ImportCommandRecord> commands = new();
        List<ImportQuoteRecord> quotes = new();
        List<ImportTimedMessageRecord> timers = new();
        Dictionary<int, string> stringTable = new();

        // Find and parse NewChanMessage records
        int cmdClassPos = FindClassDefinition(data, "NewChanMessage");
        if (cmdClassPos >= 0)
        {
            FieldDef[] layout = ParseClassDefinition(data, cmdClassPos, out int dataStart);
            ParseCommandRecords(data, dataStart, layout, commands, stringTable);
        }

        // Find and parse QuoteMessage records
        int quoteClassPos = FindClassDefinition(data, "QuoteMessage");
        if (quoteClassPos >= 0)
        {
            FieldDef[] layout = ParseClassDefinition(data, quoteClassPos, out int dataStart);
            ParseQuoteRecords(data, dataStart, layout, quotes, stringTable);
        }

        return Task.FromResult(new ParseResult(commands, quotes, timers));
    }

    /// <summary>Scans the byte array for a ClassWithMembersAndTypes record with the given class name.</summary>
    private static int FindClassDefinition(byte[] data, string className)
    {
        // Scan for record type 0x05 (ClassWithMembersAndTypes)
        // followed by objectId (4 bytes), then a length-prefixed string containing className
        byte[] needle = Encoding.UTF8.GetBytes(className);

        for (int i = 0; i < data.Length - needle.Length - 10; i++)
        {
            if (data[i] != ClassWithMembersAndTypes)
            {
                continue;
            }

            // Try to read the class name at this position
            int pos = i + 1 + 4; // skip record type + objectId
            if (pos >= data.Length)
            {
                continue;
            }

            try
            {
                string name = Read7BitString(data, ref pos);

                // Must match className but NOT be an array (e.g. "NewChanMessage[]")
                // and NOT be inside a List<> generic (e.g. "List`1[[NewChanMessage]]")
                if (name.Contains(className, StringComparison.Ordinal)
                    && !name.Contains("[]", StringComparison.Ordinal)
                    && !name.Contains("List", StringComparison.Ordinal)
                    && !name.Contains("Generic", StringComparison.Ordinal))
                {
                    // Verify member count is reasonable (> 0, < 200)
                    if (pos + 4 <= data.Length)
                    {
                        int memberCount = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos));
                        if (memberCount > 0 && memberCount < 200)
                        {
                            return i;
                        }
                    }
                }
            }
            catch
            {
                // Not a valid string at this position — continue scanning
            }
        }

        return -1;
    }

    /// <summary>Parses a ClassWithMembersAndTypes record to extract the field layout.</summary>
    private static FieldDef[] ParseClassDefinition(byte[] data, int recordStart, out int dataStart)
    {
        int pos = recordStart + 1; // skip record type byte

        // objectId (4 bytes)
        pos += 4;

        // className (length-prefixed string)
        Read7BitString(data, ref pos);

        // memberCount (4 bytes)
        int memberCount = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos));
        pos += 4;

        // Read member names
        string[] memberNames = new string[memberCount];
        for (int i = 0; i < memberCount; i++)
        {
            memberNames[i] = Read7BitString(data, ref pos);
        }

        // Read BinaryTypeEnum for each member (1 byte each)
        byte[] binaryTypes = new byte[memberCount];
        for (int i = 0; i < memberCount; i++)
        {
            binaryTypes[i] = data[pos++];
        }

        // Read additional type info based on BinaryType
        FieldDef[] fields = new FieldDef[memberCount];
        for (int i = 0; i < memberCount; i++)
        {
            switch (binaryTypes[i])
            {
                case BinaryType_Primitive:
                    byte pt = data[pos++];
                    fields[i] = new FieldDef(memberNames[i], false, pt, PrimitiveSize(pt));
                    break;

                case BinaryType_String:
                    fields[i] = new FieldDef(memberNames[i], true, 0, 0);
                    break;

                case BinaryType_SystemClass:
                    Read7BitString(data, ref pos); // skip system class name
                    fields[i] = new FieldDef(memberNames[i], false, 0, 0); // treat as opaque
                    break;

                case BinaryType_Class:
                    Read7BitString(data, ref pos); // skip class name
                    pos += 4; // skip library id
                    fields[i] = new FieldDef(memberNames[i], false, 0, 0); // treat as opaque
                    break;

                default:
                    fields[i] = new FieldDef(memberNames[i], false, 0, 0);
                    break;
            }
        }

        // Library ID (4 bytes)
        pos += 4;

        dataStart = pos;
        return fields;
    }

    /// <summary>Parses NewChanMessage records into ImportCommandRecords.</summary>
    private static void ParseCommandRecords(
        byte[] data, int startPos, FieldDef[] layout,
        List<ImportCommandRecord> commands, Dictionary<int, string> stringTable)
    {
        int pos = startPos;
        int recordNum = 0;

        for (int attempt = 0; attempt < 500; attempt++)
        {
            if (pos >= data.Length - 10)
            {
                break;
            }

            // First record's values are inline after the class definition.
            // Subsequent records start with ClassWithId (0x01).
            if (attempt > 0)
            {
                byte rt = data[pos];
                if (rt == ClassWithId)
                {
                    pos++; // record type
                    pos += 4; // objectId
                    pos += 4; // classRef
                }
                else if (rt == ObjectNull)
                {
                    pos++;
                    continue;
                }
                else if (rt == ObjectNullMultiple256)
                {
                    pos++;
                    pos++; // count byte
                    continue;
                }
                else if (rt == MessageEnd)
                {
                    break;
                }
                else
                {
                    // Unknown record type — we've likely passed the command array
                    break;
                }
            }

            // Read field values
            Dictionary<string, object?> values = new();
            bool ok = ReadFieldValues(data, ref pos, layout, values, stringTable);
            if (!ok)
            {
                break;
            }

            recordNum++;

            // Extract command fields
            string? trigger = values.GetValueOrDefault("command") as string;
            string? response = values.GetValueOrDefault("message") as string;
            string? group = values.GetValueOrDefault("group") as string;
            bool status = values.GetValueOrDefault("status") is true;
            int cooldown = values.GetValueOrDefault("cooldown") is int cd ? cd : 0;
            int accessLevel = values.GetValueOrDefault("accessLevel") is int al ? al : 0;

            if (!string.IsNullOrWhiteSpace(trigger))
            {
                commands.Add(new ImportCommandRecord
                {
                    Trigger = trigger,
                    Response = response ?? string.Empty,
                    Group = group ?? string.Empty,
                    IsEnabled = status,
                    CooldownSeconds = cooldown,
                    AccessLevel = accessLevel,
                    RecordNumber = recordNum
                });
            }
        }
    }

    /// <summary>Parses QuoteMessage records into ImportQuoteRecords.</summary>
    private static void ParseQuoteRecords(
        byte[] data, int startPos, FieldDef[] layout,
        List<ImportQuoteRecord> quotes, Dictionary<int, string> stringTable)
    {
        int pos = startPos;

        for (int attempt = 0; attempt < 200; attempt++)
        {
            if (pos >= data.Length - 10)
            {
                break;
            }

            if (attempt > 0)
            {
                byte rt = data[pos];
                if (rt == ClassWithId)
                {
                    pos++; pos += 4; pos += 4;
                }
                else if (rt == ObjectNull)
                {
                    pos++;
                    continue;
                }
                else if (rt == ObjectNullMultiple256)
                {
                    pos++; pos++;
                    continue;
                }
                else if (rt == MessageEnd)
                {
                    break;
                }
                else
                {
                    break;
                }
            }

            Dictionary<string, object?> values = new();
            bool ok = ReadFieldValues(data, ref pos, layout, values, stringTable);
            if (!ok)
            {
                break;
            }

            string? text = values.GetValueOrDefault("Msg") as string;
            string? user = values.GetValueOrDefault("User") as string;
            string? addedBy = values.GetValueOrDefault("addedBy") as string;
            int num = values.GetValueOrDefault("Num") is int n ? n : 0;

            // Parse DateTime ticks for addedOn
            DateTimeOffset? addedOn = null;
            if (values.GetValueOrDefault("addedOn") is long ticks && ticks > 0)
            {
                try
                {
                    // .NET DateTime ticks — bits 62-63 are DateTimeKind, lower 62 bits are ticks
                    long cleanTicks = ticks & 0x3FFFFFFFFFFFFFFF;
                    if (cleanTicks > 0 && cleanTicks < DateTime.MaxValue.Ticks)
                    {
                        addedOn = new DateTimeOffset(new DateTime(cleanTicks, DateTimeKind.Utc));
                    }
                }
                catch
                {
                    // Invalid ticks — leave null
                }
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                quotes.Add(new ImportQuoteRecord
                {
                    Number = num,
                    Text = text,
                    User = user ?? string.Empty,
                    AddedBy = addedBy ?? string.Empty,
                    AddedOn = addedOn
                });
            }
        }
    }

    /// <summary>Reads one record's field values according to the layout.</summary>
    private static bool ReadFieldValues(
        byte[] data, ref int pos, FieldDef[] layout,
        Dictionary<string, object?> values, Dictionary<int, string> stringTable)
    {
        foreach (FieldDef field in layout)
        {
            if (pos >= data.Length)
            {
                return false;
            }

            if (field.IsString)
            {
                if (pos >= data.Length)
                {
                    return false;
                }

                byte rt = data[pos];

                if (rt == BinaryObjectString)
                {
                    pos++; // record type
                    int objId = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos));
                    pos += 4;
                    string val = Read7BitString(data, ref pos);
                    stringTable[objId] = val;
                    values[field.Name] = val;
                }
                else if (rt == ObjectNull)
                {
                    pos++;
                    values[field.Name] = null;
                }
                else if (rt == MemberReference)
                {
                    pos++;
                    int refId = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos));
                    pos += 4;
                    values[field.Name] = stringTable.GetValueOrDefault(refId);
                }
                else
                {
                    // Unexpected record type in string field — parsing is out of sync
                    return false;
                }
            }
            else if (field.PrimitiveSize > 0)
            {
                if (pos + field.PrimitiveSize > data.Length)
                {
                    return false;
                }

                // Read the raw primitive value
                object? val = field.PrimitiveType switch
                {
                    1 => (object)(data[pos] != 0),           // Boolean
                    7 => (object)BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(pos)),  // Int16
                    8 => (object)BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos)),  // Int32
                    9 => (object)BinaryPrimitives.ReadInt64LittleEndian(data.AsSpan(pos)),  // Int64
                    13 => (object)BinaryPrimitives.ReadInt64LittleEndian(data.AsSpan(pos)), // DateTime (as ticks)
                    6 => (object)BinaryPrimitives.ReadDoubleLittleEndian(data.AsSpan(pos)), // Double
                    _ => null
                };

                values[field.Name] = val;
                pos += field.PrimitiveSize;
            }
            else
            {
                // Unknown/unsupported field type (SystemClass, Class, etc.)
                // Cannot determine size — parsing is out of sync
                return false;
            }
        }

        return true;
    }

    /// <summary>Reads a 7-bit length-prefixed UTF-8 string.</summary>
    private static string Read7BitString(byte[] data, ref int pos)
    {
        int length = 0;
        int shift = 0;
        while (true)
        {
            byte b = data[pos++];
            length |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                break;
            }
            shift += 7;
        }

        string result = Encoding.UTF8.GetString(data, pos, length);
        pos += length;
        return result;
    }
}
