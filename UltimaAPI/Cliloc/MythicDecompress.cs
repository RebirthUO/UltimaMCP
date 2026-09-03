using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace UltimaAPI.Cliloc;

internal static class MythicDecompress
{
    private const uint HeaderXorKey = 0x8E2C9A3D;
    private const int FrequencyHeaderSize = 1024;

    public static uint PeekDecompressedLength(ReadOnlySpan<byte> source)
    {
        if (source.Length < 4)
        {
            return 0;
        }

        return BinaryPrimitives.ReadUInt32LittleEndian(source) ^ HeaderXorKey;
    }

    public static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int written)
    {
        written = 0;

        if (source.Length < 4)
        {
            return false;
        }

        uint dataLength = BinaryPrimitives.ReadUInt32LittleEndian(source) ^ HeaderXorKey;
        if (destination.Length < (int)dataLength)
        {
            return false;
        }

        ReadOnlySpan<byte> mtfInput = source.Slice(4);
        byte[] rented = ArrayPool<byte>.Shared.Rent(mtfInput.Length);
        try
        {
            Span<byte> mtfBuffer = rented.AsSpan(0, mtfInput.Length);
            MoveToFrontCoding.Decode(mtfInput, mtfBuffer);

            if (!TryInternalDecompress(mtfBuffer, destination, out written))
            {
                return false;
            }

            return written == (int)dataLength;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static bool TryInternalDecompress(ReadOnlySpan<byte> input, Span<byte> destination, out int written)
    {
        written = 0;

        try
        {
            if (input.Length < FrequencyHeaderSize)
            {
                return false;
            }

            Span<byte> symbolTable = stackalloc byte[256];
            Span<byte> frequency = stackalloc byte[256];
            Span<int> partialInput = stackalloc int[256 * 3];
            partialInput.Clear();

            for (int i = 0; i < 256; i++)
            {
                symbolTable[i] = (byte)i;
            }

            input.Slice(0, FrequencyHeaderSize).CopyTo(MemoryMarshal.AsBytes(partialInput));

            int sum = 0;
            for (int i = 0; i < 256; i++)
            {
                sum += partialInput[i];
            }

            if (sum == 0)
            {
                written = 0;
                return true;
            }

            if (destination.Length < sum)
            {
                return false;
            }

            int nonZeroCount = 0;
            for (int i = 0; i < 256; i++)
            {
                if (partialInput[i] != 0)
                {
                    nonZeroCount++;
                }
            }

            Frequency(partialInput, frequency);

            for (int i = 0, m = 0; i < nonZeroCount; ++i)
            {
                byte freq = frequency[i];
                symbolTable[input[m + FrequencyHeaderSize]] = freq;
                partialInput[freq + 256] = m + 1;
                m += partialInput[freq];
                partialInput[freq + 512] = m;
            }

            byte val = symbolTable[0];
            int count = 0;

            do
            {
                ref int firstValRef = ref partialInput[val + 256];
                destination[count] = val;

                if (firstValRef < partialInput[val + 512])
                {
                    byte idx = input[firstValRef + FrequencyHeaderSize];
                    firstValRef++;

                    if (idx != 0)
                    {
                        ShiftLeft(symbolTable, idx);
                        symbolTable[idx] = val;
                        val = symbolTable[0];
                    }
                }
                else if (nonZeroCount-- > 0)
                {
                    ShiftLeft(symbolTable, nonZeroCount);
                    val = symbolTable[0];
                }

                count++;
            }
            while (count < sum);

            written = sum;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void Frequency(Span<int> input, Span<byte> output)
    {
        Span<int> tmp = stackalloc int[256];
        input.Slice(0, tmp.Length).CopyTo(tmp);

        for (int i = 0; i < 256; i++)
        {
            uint value = 0;
            byte index = 0;

            for (int j = 0; j < 256; j++)
            {
                if (tmp[j] > value)
                {
                    index = (byte)j;
                    value = (uint)tmp[j];
                }
            }

            if (value == 0)
            {
                break;
            }

            output[i] = index;
            tmp[index] = 0;
        }
    }

    private static void ShiftLeft(Span<byte> input, int element)
    {
        for (int i = 0; i < element; ++i)
        {
            input[i] = input[i + 1];
        }
    }
}
