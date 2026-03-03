using System;
using System.Collections.Generic;
using GeoTools;

namespace AisSimulator;

/// <summary>
/// Extension methods for Route-related operations.
/// </summary>
public static class RouteExtensions
{
    /// <summary>
    /// Converts a list of route points into a list of legs between consecutive waypoints.
    /// </summary>
    public static List<Leg> ToLegs(this List<RoutePoint> waypoints)
    {
        if (waypoints.Count < 2)
            throw new ArgumentException("At least 2 waypoints are required to create legs", nameof(waypoints));

        List<Leg> legs = new();

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            RoutePoint current = waypoints[i];
            RoutePoint next = waypoints[i + 1];

            double course = LatLongCalculations.InitialBearing(current.Position, next.Position);
            double distance = LatLongCalculations.DistanceNauticalMiles(current.Position, next.Position);

            Leg leg = new(current.Position, next.Position, course, distance);
            legs.Add(leg);
        }

        return legs;
    }

    /// <summary>
    /// Converts the waypoints in a route into a list of legs.
    /// </summary>
    public static List<Leg> GetLegs(this Route route)
    {
        return route.Waypoints.ToLegs();
    }
}