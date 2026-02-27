using StarTracker.Core.DTOs;
using StarTracker.Core.Interfaces;
using StarTracker.Core.Services;

namespace StarTracker.Infrastructure.Repositories;

public sealed class InMemoryAstronomyCatalogService : IAstronomyCatalogService
{
    private const int MaxRepeatedErrors = 5;

    private static readonly StarCatalogEntry[] Entries =
    [
        new("HIP 32349", "Sirius", -16.7161, 101.2875, ["alpha CMa", "Dog Star"]),
        new("HIP 30438", "Canopus", -52.6957, 95.9879, ["alpha Car"]),
        new("HIP 71683", "Rigil Kentaurus", -60.8339, 219.9021, ["Alpha Centauri", "alpha Cen"]),
        new("HIP 69673", "Arcturus", 19.1824, 213.9154, ["alpha Boo"]),
        new("HIP 91262", "Vega", 38.7837, 279.2347, ["alpha Lyr"]),
        new("HIP 11767", "Polaris", 89.2641, 37.9546, ["alpha UMi", "North Star"]),
        new("HIP 24436", "Rigel", -8.2016, 78.6345, ["beta Ori"]),
        new("HIP 27989", "Betelgeuse", 7.4071, 88.7929, ["alpha Ori"]),
        new("HIP 37279", "Procyon", 5.2249, 114.8255, ["alpha CMi"]),
        new("HIP 97649", "Altair", 8.8683, 297.6958, ["alpha Aql"])
    ];

    public async Task<AstronomyPositionResult?> GetPositionAsync(
        string target,
        double observerLat,
        double observerLon,
        DateTimeOffset at,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(target)) return null;

        var normalized = Normalize(target);
        var entry = Entries.FirstOrDefault(x => x.Matches(normalized));
        if (entry is null) return null;

        Exception? lastError = null;
        for (var attempt = 1; attempt <= MaxRepeatedErrors; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await Task.Yield(); // keep async shape for future MCP client implementation
                var (az, alt) = AstronomyMapper.ComputeAzAlt(observerLat, observerLon, entry.RightAscensionDegrees, entry.DeclinationDegrees, at);

                return new AstronomyPositionResult(
                    entry.CanonicalName,
                    entry.ObjectId,
                    entry.RightAscensionDegrees,
                    entry.DeclinationDegrees,
                    az,
                    alt);
            }
            catch (Exception ex) when (attempt < MaxRepeatedErrors)
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException($"Astronomy lookup failed after {MaxRepeatedErrors} repeated errors.", lastError);
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private sealed record StarCatalogEntry(
        string ObjectId,
        string CanonicalName,
        double DeclinationDegrees,
        double RightAscensionDegrees,
        string[] Aliases)
    {
        public bool Matches(string target)
        {
            if (Normalize(CanonicalName) == target) return true;
            if (Normalize(ObjectId) == target) return true;
            return Aliases.Any(alias => Normalize(alias) == target);
        }
    }
}
