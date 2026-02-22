using System;
using GeoTools;

namespace AisSimulator;

public record RoutePoint(
    Position Position,
    DateTime Time,
    string Name,
    string Symbol,
    string Type)
{
    public double Latitude => Position.Latitude;
    public double Longitude => Position.Longitude;
}