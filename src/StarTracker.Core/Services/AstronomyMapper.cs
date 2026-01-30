namespace StarTracker.Core.Services;

public static class AstronomyMapper
{
    // Map lat/lon to a deterministic RA/Dec for demo and testing purposes.
    public static (double RightAscensionDegrees, double DeclinationDegrees) FromLatLon(double lat, double lon)
    {
        // RA -> normalized longitude (0..360)
        var ra = (lon + 360) % 360;
        var dec = lat;
        return (ra, dec);
    }

    // Compute Azimuth and Altitude (in degrees) given observer lat/lon (decimal degrees), RA/Dec (degrees) and time (UTC)
    public static (double AzimuthDegrees, double AltitudeDegrees) ComputeAzAlt(double observerLatDeg, double observerLonDeg, double rightAscensionDeg, double declinationDeg, DateTimeOffset atUtc)
    {
        // Convert degrees to radians
        double deg2rad(double d) => d * Math.PI / 180.0;
        double rad2deg(double r) => r * 180.0 / Math.PI;

        var lat = deg2rad(observerLatDeg);
        var lon = observerLonDeg;
        var ra = deg2rad(rightAscensionDeg);
        var dec = deg2rad(declinationDeg);

        // Compute Greenwich Sidereal Time (in degrees)
        // Algorithm: use simplified conversion via Julian Date
        double jd = ToJulianDate(atUtc.UtcDateTime);
        double d = jd - 2451545.0;
        // Greenwich Mean Sidereal Time in degrees
        double GMST = 280.46061837 + 360.98564736629 * d;
        GMST = ((GMST % 360) + 360) % 360;

        // Local Sidereal Time in degrees
        double LST = (GMST + lon) % 360;
        if (LST < 0) LST += 360;

        // Hour angle in radians: HA = LST - RA (both in degrees)
        var HA = deg2rad((LST - rightAscensionDeg + 360) % 360);
        if (HA > Math.PI) HA -= 2 * Math.PI; // normalize to [-pi, pi]

        // Altitude
        var sinAlt = Math.Sin(dec) * Math.Sin(lat) + Math.Cos(dec) * Math.Cos(lat) * Math.Cos(HA);
        var alt = Math.Asin(Math.Clamp(sinAlt, -1.0, 1.0));

        // Azimuth (measured from north towards east)
        var y = Math.Sin(HA);
        var x = Math.Cos(HA) * Math.Sin(lat) - Math.Tan(dec) * Math.Cos(lat);
        var az = Math.Atan2(y, x);
        // Convert to degrees and normalize to 0-360
        var azDeg = (rad2deg(az) + 360) % 360;
        var altDeg = rad2deg(alt);

        return (Math.Round(azDeg, 5), Math.Round(altDeg, 5));
    }

    private static double ToJulianDate(DateTime dt)
    {
        // From https://aa.usno.navy.mil/faq/docs/JD_Formula.php
        int year = dt.Year;
        int month = dt.Month;
        double day = dt.Day + dt.Hour / 24.0 + dt.Minute / 1440.0 + dt.Second / 86400.0 + dt.Millisecond / 86400000.0;

        if (month <= 2)
        {
            year -= 1;
            month += 12;
        }

        int A = year / 100;
        int B = 2 - A + (A / 4);

        double jd = Math.Floor(365.25 * (year + 4716)) + Math.Floor(30.6001 * (month + 1)) + day + B - 1524.5;
        return jd;
    }
}