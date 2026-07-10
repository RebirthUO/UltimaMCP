namespace UltimaAPI.Services;

internal static class RequestBounds
{
    public static int RequireInRange(int value, int minimum, int maximum, string parameterName)
    {
        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"Value must be between {minimum} and {maximum}.");
        }

        return value;
    }

    public static string RequireSearchTerm(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return value.Trim();
    }

    public static int NormalizeLimit(int? value, int defaultValue, int maximum, string parameterName = "limit")
        => RequireInRange(value ?? defaultValue, 1, maximum, parameterName);
}
