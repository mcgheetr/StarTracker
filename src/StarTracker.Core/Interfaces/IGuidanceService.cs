namespace StarTracker.Core.Interfaces;

public interface IGuidanceService
{
    /// <summary>
    /// Produce human-friendly guidance text from azimuth/altitude and target name.
    /// </summary>
    string GenerateGuidance(double azimuthDegrees, double altitudeDegrees, string target);
}