namespace GeoTools;

/// <summary>
/// Latitude/Longitude calculations using the haversine formula and spherical trigonometry.
/// Ported from https://www.movable-type.co.uk/scripts/latlong.html
/// </summary>
public static class LatLongCalculations
{
    private const double EarthRadiusMetres = 6371e3;
    private const double EarthRadiusNauticalMiles = 3440.065;

    /// <summary>
    /// Calculates the great-circle distance between two positions using the haversine formula.
    /// </summary>
    /// <returns>Distance in metres</returns>
    public static double DistanceMetres(Position pos1, Position pos2)
    {
        double φ1 = ToRadians(pos1.Latitude);
        double φ2 = ToRadians(pos2.Latitude);
        double Δφ = ToRadians(pos2.Latitude - pos1.Latitude);
        double Δλ = ToRadians(pos2.Longitude - pos1.Longitude);

        double a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                   Math.Cos(φ1) * Math.Cos(φ2) *
                   Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMetres * c;
    }

    /// <summary>
    /// Calculates the great-circle distance between two positions in kilometres.
    /// </summary>
    public static double DistanceKilometres(Position pos1, Position pos2)
    {
        return DistanceMetres(pos1, pos2) / 1000.0;
    }

    /// <summary>
    /// Calculates the great-circle distance between two positions in nautical miles.
    /// </summary>
    public static double DistanceNauticalMiles(Position pos1, Position pos2)
    {
        double φ1 = ToRadians(pos1.Latitude);
        double φ2 = ToRadians(pos2.Latitude);
        double Δφ = ToRadians(pos2.Latitude - pos1.Latitude);
        double Δλ = ToRadians(pos2.Longitude - pos1.Longitude);

        double a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                   Math.Cos(φ1) * Math.Cos(φ2) *
                   Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusNauticalMiles * c;
    }

    /// <summary>
    /// Calculates the initial bearing from one position to another in degrees (0-360).
    /// </summary>
    public static double InitialBearing(Position pos1, Position pos2)
    {
        double φ1 = ToRadians(pos1.Latitude);
        double φ2 = ToRadians(pos2.Latitude);
        double λ1 = ToRadians(pos1.Longitude);
        double λ2 = ToRadians(pos2.Longitude);

        double y = Math.Sin(λ2 - λ1) * Math.Cos(φ2);
        double x = Math.Cos(φ1) * Math.Sin(φ2) -
                   Math.Sin(φ1) * Math.Cos(φ2) * Math.Cos(λ2 - λ1);
        double θ = Math.Atan2(y, x);
        return (ToDegrees(θ) + 360) % 360;
    }

    /// <summary>
    /// Calculates the final bearing when arriving at a destination position.
    /// </summary>
    public static double FinalBearing(Position pos1, Position pos2)
    {
        return (InitialBearing(pos2, pos1) + 180) % 360;
    }

    /// <summary>
    /// Calculates the midpoint between two positions.
    /// </summary>
    public static Position Midpoint(Position pos1, Position pos2)
    {
        double φ1 = ToRadians(pos1.Latitude);
        double φ2 = ToRadians(pos2.Latitude);
        double λ1 = ToRadians(pos1.Longitude);
        double λ2 = ToRadians(pos2.Longitude);

        double Bx = Math.Cos(φ2) * Math.Cos(λ2 - λ1);
        double By = Math.Cos(φ2) * Math.Sin(λ2 - λ1);

        double φ3 = Math.Atan2(Math.Sin(φ1) + Math.Sin(φ2),
                               Math.Sqrt((Math.Cos(φ1) + Bx) * (Math.Cos(φ1) + Bx) + By * By));
        double λ3 = λ1 + Math.Atan2(By, Math.Cos(φ1) + Bx);

        // Normalise longitude to ±180°
        λ3 = ((λ3 + Math.PI) % (2 * Math.PI)) - Math.PI;

        return new Position(ToDegrees(φ3), ToDegrees(λ3));
    }

    /// <summary>
    /// Calculates a destination position given a starting position, bearing, and distance in metres.
    /// </summary>
    public static Position DestinationPoint(Position start, double bearing, double distanceMetres)
    {
        double φ1 = ToRadians(start.Latitude);
        double λ1 = ToRadians(start.Longitude);
        double θ = ToRadians(bearing);
        double δ = distanceMetres / EarthRadiusMetres;

        double φ2 = Math.Asin(Math.Sin(φ1) * Math.Cos(δ) +
                              Math.Cos(φ1) * Math.Sin(δ) * Math.Cos(θ));
        double λ2 = λ1 + Math.Atan2(Math.Sin(θ) * Math.Sin(δ) * Math.Cos(φ1),
                                    Math.Cos(δ) - Math.Sin(φ1) * Math.Sin(φ2));

        // Normalise longitude to ±180°
        λ2 = ((λ2 + Math.PI) % (2 * Math.PI)) - Math.PI;

        return new Position(ToDegrees(φ2), ToDegrees(λ2));
    }

    /// <summary>
    /// Calculates a destination position given a starting position, bearing, and distance in kilometres.
    /// </summary>
    public static Position DestinationPointKm(Position start, double bearing, double distanceKm)
    {
        return DestinationPoint(start, bearing, distanceKm * 1000.0);
    }

    /// <summary>
    /// Calculates the cross-track distance from a point to a great-circle path.
    /// Negative value indicates the point is to the left of the path.
    /// </summary>
    public static double CrossTrackDistance(Position point, Position pathStart, Position pathEnd)
    {
        double δ13 = DistanceMetres(point, pathStart) / EarthRadiusMetres;
        double θ13 = ToRadians(InitialBearing(pathStart, point));
        double θ12 = ToRadians(InitialBearing(pathStart, pathEnd));

        double dXt = Math.Asin(Math.Sin(δ13) * Math.Sin(θ13 - θ12));
        return EarthRadiusMetres * dXt;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static double ToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }
}