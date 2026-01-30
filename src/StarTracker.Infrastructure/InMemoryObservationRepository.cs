using StarTracker.Core.DTOs;

namespace StarTracker.Infrastructure;

public class InMemoryObservationRepository(IEncryptionService? encryption = null) : IObservationRepository
{
    private record EncryptedRecord(Guid Id, string Target, DateTimeOffset ObservedAt, string EncryptedLocationPayload, string Observer, string? Notes);

    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, EncryptedRecord> _store = new();
    private readonly IEncryptionService _encryption = encryption ?? new NoopEncryptionService();

    public Task<ObservationDto> CreateObservationAsync(ObservationDto observation, CancellationToken cancellationToken = default)
    {
        var id = observation.Id == Guid.Empty ? Guid.NewGuid() : observation.Id;
        // Serialize RA/Dec into a small payload and encrypt it.
        var locationPayload = System.Text.Json.JsonSerializer.Serialize(new { observation.RightAscensionDegrees, observation.DeclinationDegrees });
        var encrypted = _encryption.Protect(locationPayload);

        var rec = new EncryptedRecord(id, observation.Target, observation.ObservedAt, encrypted, observation.Observer, observation.Notes);
        _store[id] = rec;

        var toReturn = observation with { Id = id };
        return Task.FromResult(toReturn);
    }

    public Task<ObservationDto?> GetObservationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(id, out var rec)) return Task.FromResult<ObservationDto?>(null);

        var locJson = _encryption.Unprotect(rec.EncryptedLocationPayload);
        var loc = System.Text.Json.JsonSerializer.Deserialize<LocationPayload>(locJson) ?? new LocationPayload { RightAscensionDegrees = 0, DeclinationDegrees = 0 };

        var dto = new ObservationDto(rec.Id, rec.Target, rec.ObservedAt, loc.RightAscensionDegrees, loc.DeclinationDegrees, rec.Observer, rec.Notes);
        return Task.FromResult<ObservationDto?>(dto);
    }

    public Task<IEnumerable<ObservationDto>> GetObservationsAsync(string target, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default)
    {
        var query = _store.Values.Where(o => string.Equals(o.Target, target, StringComparison.OrdinalIgnoreCase));
        if (from is not null) query = query.Where(o => o.ObservedAt >= from.Value);
        if (to is not null) query = query.Where(o => o.ObservedAt <= to.Value);

        var list = query.OrderBy(o => o.ObservedAt).Select(rec =>
        {
            var locJson = _encryption.Unprotect(rec.EncryptedLocationPayload);
            var loc = System.Text.Json.JsonSerializer.Deserialize<LocationPayload>(locJson) ?? new LocationPayload { RightAscensionDegrees = 0, DeclinationDegrees = 0 };
            return new ObservationDto(rec.Id, rec.Target, rec.ObservedAt, loc.RightAscensionDegrees, loc.DeclinationDegrees, rec.Observer, rec.Notes);
        }).ToList();

        return Task.FromResult((IEnumerable<ObservationDto>)list);
    }

    // Internal helper for tests to inspect raw encrypted payloads
    internal IEnumerable<string> GetRawEncryptedPayloads() => _store.Values.Select(s => s.EncryptedLocationPayload);

    private class LocationPayload { public double RightAscensionDegrees { get; set; } public double DeclinationDegrees { get; set; } }
}