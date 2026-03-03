using System;
using System.Collections.Generic;
using GeoTools;

namespace AisSimulator;

/// <summary>
/// Represents a leg of a route between two consecutive positions.
/// </summary>
public record Leg(
    Position Start,
    Position End,
    double Course,
    double DistanceNauticalMiles)
{
    /// <summary>
    /// Duration of this leg in seconds, assuming constant speed in knots.
    /// </summary>
    public double DurationSeconds(double speedKnots)
    {
        if (speedKnots <= 0)
            throw new ArgumentException("Speed must be positive", nameof(speedKnots));
        return (DistanceNauticalMiles / speedKnots) * 3600;
    }

    public IEnumerable<Position> Positions(double speedKnots, double intervalSeconds)
    {
        yield return Start;

        double distanceAlongLeg = 0.0;
        while (distanceAlongLeg < DistanceNauticalMiles)
        {
            // Distance to travel in this interval (in nautical miles)
            distanceAlongLeg += (speedKnots * intervalSeconds) / 3600.0;

            if (distanceAlongLeg < DistanceNauticalMiles)
            {
                yield return LatLongCalculations.DestinationPoint(Start, Course, distanceAlongLeg);
            }
        }

        yield return End;
    }

    public override string ToString() =>
        $"({Start.Latitude:F4}, {Start.Longitude:F4}) → ({End.Latitude:F4}, {End.Longitude:F4}) | {Course:F1}° | {DistanceNauticalMiles:F2} NM";
}