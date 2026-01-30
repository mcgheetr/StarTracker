using StarTracker.Core.DTOs;

namespace StarTracker.Core.Interfaces;

public interface IObservationRepository
{
    Task<ObservationDto?> GetObservationAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<ObservationDto>> GetObservationsAsync(string target, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default);

    Task<ObservationDto> CreateObservationAsync(ObservationDto observation, CancellationToken cancellationToken = default);
}