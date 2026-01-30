using StarTracker.Core.Interfaces;

namespace StarTracker.Core.Services;

public class SimpleGuidanceService : IGuidanceService
{
    // Map azimuth degrees to a compass direction (16-point compass)
    private static readonly string[] _directions = new[]
    {
        "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
        "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"
    };

    public string GenerateGuidance(double azimuthDegrees, double altitudeDegrees, string target)
    {
        // Normalize
        var az = ((azimuthDegrees % 360) + 360) % 360;
        var sector = (int)Math.Round(az / 22.5) % 16;
        var dir = _directions[sector];

        // Friendly compass phrasing
        var compass = dir == "N" ? "due north" : dir;

        // Altitude phrasing
        var altRounded = Math.Round(altitudeDegrees);

        // Special-case for Polaris: we prefer a clearer message
        if (string.Equals(target, "Polaris", StringComparison.OrdinalIgnoreCase))
        {
            // If azimuth is near north, say due north
            if (Math.Abs(az - 0) < 10 || Math.Abs(az - 360) < 10)
                return $"Look {compass} at about {altRounded}° up for the brightest object — that's Polaris!";

            return $"Face {compass} (approx {Math.Round(az)}°), then look {altRounded}° up to see Polaris.";
        }

        return $"Face {compass} (≈{Math.Round(az)}°), then look {altRounded}° up to find {target}.";
    }
}