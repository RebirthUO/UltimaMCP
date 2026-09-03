namespace UltimaAPI.Cliloc;

internal static class MoveToFrontCoding
{
    public static void Decode(ReadOnlySpan<byte> input, Span<byte> output)
    {
        Span<byte> symbols = stackalloc byte[256];

        for (int i = 0; i < 256; i++)
        {
            symbols[i] = (byte)i;
        }

        for (int i = 0; i < input.Length; i++)
        {
            int index = input[i];
            output[i] = symbols[index];
            MoveToFront(symbols, index);
        }
    }

    private static void MoveToFront(Span<byte> array, int elementIndex)
    {
        byte element = array[elementIndex];

        for (int i = elementIndex; i > 0; i--)
        {
            array[i] = array[i - 1];
        }

        array[0] = element;
    }
}
