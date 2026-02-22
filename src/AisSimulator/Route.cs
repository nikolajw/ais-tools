using System;
using System.Collections.Generic;

namespace AisSimulator;

public record Route(
    string Name,
    string? Start,
    string? End,
    double PlannedSpeed,
    DateTime PlannedDeparture,
    List<RoutePoint> Waypoints);
