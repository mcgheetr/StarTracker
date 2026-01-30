namespace StarTracker.Core.DTOs;

public record ObservationDto(
    Guid Id,
    string Target,
    DateTimeOffset ObservedAt,
    double RightAscensionDegrees,
    double DeclinationDegrees,
    string Observer,
    string? Notes);