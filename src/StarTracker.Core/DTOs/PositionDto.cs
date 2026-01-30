namespace StarTracker.Core.DTOs;

public record PositionResponseDto(
    string Target,
    double RightAscensionDegrees,
    double DeclinationDegrees,
    double AzimuthDegrees,
    double AltitudeDegrees,
    string Guidance,
    DateTimeOffset At);