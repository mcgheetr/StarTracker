namespace StarTracker.Core.Services;

public static class CoordinateNormalizer
{
    // Parses a decimal degree string (optionally with a hemisphere letter) and normalizes to 5 decimal places.
    // Returns true if parse succeeded and value is within valid range for lat/lon (caller should apply specific range checks).
    public static bool TryParseDecimalDegrees(string? input, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var s = input.Trim();

        // Handle hemisphere suffixes (N,S,E,W) optionally appended
        char last = s[^1];
        int? hemisphereSign = null; // 1 for positive, -1 for negative, null for none
        if (last == 'N' || last == 'n') hemisphereSign = 1;
        else if (last == 'S' || last == 's') hemisphereSign = -1;
        else if (last == 'E' || last == 'e') hemisphereSign = 1;
        else if (last == 'W' || last == 'w') hemisphereSign = -1;

        if (hemisphereSign is not null)
        {
            s = s[..^1].Trim();
        }

        // Accept only decimal format for now (digits, optional sign, optional decimal point)
        if (!double.TryParse(s, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed))
            return false;

        if (hemisphereSign is not null)
            parsed = Math.Abs(parsed) * hemisphereSign.Value;

        // Normalize to 5 decimal places. Use AwayFromZero to avoid banker's rounding surprises.
        var normalized = Math.Round(parsed, 5, MidpointRounding.AwayFromZero);

        value = normalized;
        return true;
    }
}