using System.Globalization;

namespace HSB.Utils;

internal static class SizeUtils
{
    private static readonly Dictionary<string, long> UnitMultipliers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["B"] = 1L,
        ["KB"] = 1024L,
        ["MB"] = 1024L * 1024L,
        ["GB"] = 1024L * 1024L * 1024L,
        ["TB"] = 1024L * 1024L * 1024L * 1024L
    };

    public static long ParseBytes(string? rawValue, long fallbackValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return fallbackValue;
        }

        var value = rawValue.Trim();
        var index = 0;
        while (index < value.Length && (char.IsDigit(value[index]) || value[index] == '.'))
        {
            index++;
        }

        if (index == 0)
        {
            return fallbackValue;
        }

        if (!double.TryParse(value[..index], NumberStyles.Float, CultureInfo.InvariantCulture, out var numericValue))
        {
            return fallbackValue;
        }

        var unit = index == value.Length ? "B" : value[index..].Trim().ToUpperInvariant();
        if (!UnitMultipliers.TryGetValue(unit, out var multiplier))
        {
            return fallbackValue;
        }

        var parsed = numericValue * multiplier;
        if (double.IsNaN(parsed) || double.IsInfinity(parsed) || parsed < 0)
        {
            return fallbackValue;
        }

        return (long)Math.Min(parsed, long.MaxValue);
    }

    public static int ClampToInt(long value, int minValue, int maxValue = int.MaxValue)
    {
        if (value < minValue)
        {
            return minValue;
        }

        if (value > maxValue)
        {
            return maxValue;
        }

        return (int)value;
    }
}
