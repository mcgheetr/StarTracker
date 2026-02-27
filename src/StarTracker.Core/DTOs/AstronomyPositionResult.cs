namespace StarTracker.Core.DTOs;

public sealed record AstronomyPositionResult(
    string CanonicalName,
    string ObjectId,
    double RightAscensionDegrees,
    double DeclinationDegrees,
    double AzimuthDegrees,
    double AltitudeDegrees);
