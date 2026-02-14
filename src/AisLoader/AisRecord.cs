using System;

namespace AisLoader;

public record AisRecord(
    DateTime Timestamp,
    int Mmsi,
    double Latitude,
    double Longitude,
    string? NavigationalStatus,
    double Rot,
    double Sog,
    double Cog,
    int Heading);
