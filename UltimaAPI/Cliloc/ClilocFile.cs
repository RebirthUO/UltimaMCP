using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace UltimaAPI.Cliloc;

internal sealed record ClilocEntry(int Number, string Text, byte Flag);

internal sealed class ClilocFile
{
    private readonly Dictionary<int, ClilocEntry> _entriesByNumber;

    private ClilocFile(string language, IReadOnlyList<ClilocEntry> entries)
    {
        Language = language;
        Entries = entries;
        _entriesByNumber = entries.ToDictionary(static entry => entry.Number);
    }

    public string Language { get; }

    public IReadOnlyList<ClilocEntry> Entries { get; }

    public ClilocEntry? TryGetEntry(int number)
        => _entriesByNumber.GetValueOrDefault(number);

    public static ClilocFile Load(string language, string path)
    {
        byte[] buffer = File.ReadAllBytes(path);

        ParseResult primary = TryParse(buffer, decompress: true);
        if (primary.Success)
        {
            return new ClilocFile(language, primary.Entries);
        }

        ParseResult fallback = TryParse(buffer, decompress: false);
        if (fallback.Success)
        {
            return new ClilocFile(language, fallback.Entries);
        }

        ParseResult best = primary.EntriesParsed >= fallback.EntriesParsed ? primary : fallback;
        if (best.EntriesParsed > 0)
        {
            return new ClilocFile(language, best.Entries);
        }

        throw new InvalidDataException(
            $"Failed to parse cliloc file '{path}' in either compressed or uncompressed format." +
            $"{Environment.NewLine}  As compressed: {primary.ErrorMessage}" +
            $"{Environment.NewLine}  As uncompressed: {fallback.ErrorMessage}");
    }

    private sealed class ParseResult
    {
        public bool Success { get; init; }
        public int EntriesParsed { get; init; }
        public IReadOnlyList<ClilocEntry> Entries { get; init; } = [];
        public string ErrorMessage { get; init; } = string.Empty;
    }

    private static ParseResult TryParse(byte[] buffer, bool decompress)
    {
        byte[]? rented = null;
        try
        {
            ReadOnlySpan<byte> data;

            if (decompress)
            {
                uint expectedLen = MythicDecompress.PeekDecompressedLength(buffer);
                if (expectedLen == 0 || expectedLen > int.MaxValue)
                {
                    return new ParseResult { ErrorMessage = "decompression failed: invalid header." };
                }

                rented = ArrayPool<byte>.Shared.Rent((int)expectedLen);
                if (!MythicDecompress.TryDecompress(buffer, rented.AsSpan(0, (int)expectedLen), out int written))
                {
                    return new ParseResult { ErrorMessage = "decompression failed." };
                }

                data = rented.AsSpan(0, written);
            }
            else
            {
                data = buffer;
            }

            if (data.Length < 6)
            {
                return new ParseResult { ErrorMessage = $"file is {data.Length} bytes, smaller than the 6-byte header." };
            }

            List<ClilocEntry> entries = [];
            int cursor = 6;
            int lastNumber = -1;

            while (cursor < data.Length)
            {
                int entryStart = cursor;
                int remaining = data.Length - cursor;

                if (remaining < 7)
                {
                    return new ParseResult
                    {
                        Entries = entries,
                        EntriesParsed = entries.Count,
                        ErrorMessage =
                            $"unexpected {remaining} trailing byte(s) at offset 0x{entryStart:X} after entry #{lastNumber}; " +
                            "need 7 bytes for the next entry header."
                    };
                }

                int number = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(cursor));
                byte flag = data[cursor + 4];
                int length = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(cursor + 5));
                cursor += 7;

                int bodyRemaining = data.Length - cursor;
                if (length > bodyRemaining)
                {
                    return new ParseResult
                    {
                        Entries = entries,
                        EntriesParsed = entries.Count,
                        ErrorMessage =
                            $"entry #{number} at offset 0x{entryStart:X} declares length {length}, " +
                            $"but only {bodyRemaining} byte(s) remain in the file " +
                            $"(previous entry was #{lastNumber}, parsed {entries.Count} so far)."
                    };
                }

                string text;
                try
                {
                    text = Encoding.UTF8.GetString(data.Slice(cursor, length));
                }
                catch (Exception ex)
                {
                    return new ParseResult
                    {
                        Entries = entries,
                        EntriesParsed = entries.Count,
                        ErrorMessage =
                            $"entry #{number} at offset 0x{entryStart:X} has {length} body bytes that are not valid UTF-8: {ex.Message}"
                    };
                }

                cursor += length;
                entries.Add(new ClilocEntry(number, text, flag));
                lastNumber = number;
            }

            return new ParseResult
            {
                Success = true,
                Entries = entries,
                EntriesParsed = entries.Count
            };
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}
