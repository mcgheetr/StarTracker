namespace StarTracker.Core.DTOs;

public record CreateObservationRequest(
    DateTimeOffset ObservedAt,
    double RightAscensionDegrees,
    double DeclinationDegrees,
    string Observer,
    string? Notes);