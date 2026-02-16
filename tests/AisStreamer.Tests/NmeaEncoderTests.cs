using System;

namespace AisStreamer.Tests;

public class NmeaEncoderTests
{
    [Theory]
    [InlineData("Under way using engine", 0)]
    [InlineData("At anchor", 1)]
    [InlineData("Not under command", 2)]
    [InlineData("Restricted manoeuvrability", 3)]
    [InlineData("Constrained by her draught", 4)]
    [InlineData("Moored", 5)]
    [InlineData("Aground", 6)]
    [InlineData("Engaged in fishing", 7)]
    [InlineData("Under way sailing", 8)]
    [InlineData("Unknown status", 15)]
    [InlineData(null, 15)]
    [InlineData("", 15)]
    public void NavStatusToCode_ReturnsCorrectCode(string? status, int expectedCode)
    {
        // Act
        var result = NmeaEncoder.NavStatusToCode(status);

        // Assert
        result.ShouldBe(expectedCode);
    }

    [Fact]
    public void NavStatusToCode_IsCaseInsensitive()
    {
        // Act
        var result1 = NmeaEncoder.NavStatusToCode("UNDER WAY USING ENGINE");
        var result2 = NmeaEncoder.NavStatusToCode("Under way using engine");
        var result3 = NmeaEncoder.NavStatusToCode("under way using engine");

        // Assert
        result1.ShouldBe(0);
        result2.ShouldBe(0);
        result3.ShouldBe(0);
    }

    [Fact]
    public void ToNmea0183_WithValidRecord_ReturnsFormattedString()
    {
        // Arrange
        var record = new AisRecord(
            Timestamp: new DateTime(2024, 1, 15, 10, 30, 45),
            Mmsi: 220382000,
            Latitude: 55.1234,
            Longitude: -2.5678,
            NavigationalStatus: "Under way using engine",
            Rot: 0,
            Sog: 12.5,
            Cog: 90.0,
            Heading: 180);

        // Act
        var result = NmeaEncoder.ToNmea0183(record);

        // Assert
        result.ShouldStartWith("!");
        result.ShouldContain("AIVDM");
        result.ShouldContain("*");
        result.Length.ShouldBeGreaterThan(20);
    }

    [Fact]
    public void ToNmeaCoord_WithPositiveLatitude_FormatsCorrectly()
    {
        // Act
        var result = NmeaEncoder.ToNmeaCoord(55.1234);

        // Assert
        result.ShouldStartWith("55");
        result.ShouldContain(".");
    }

    [Fact]
    public void ToNmeaCoord_WithNegativeLongitude_UsesAbsoluteValue()
    {
        // Act
        var result = NmeaEncoder.ToNmeaCoord(-2.5678);

        // Assert
        result.ShouldStartWith("02");
        result.ShouldNotStartWith("-");
    }

    [Fact]
    public void ToGprmc_WithValidRecord_ReturnsFormattedString()
    {
        // Arrange
        var record = new AisRecord(
            Timestamp: new DateTime(2024, 1, 15, 10, 30, 45),
            Mmsi: 220382000,
            Latitude: 55.1234,
            Longitude: -2.5678,
            NavigationalStatus: "Under way using engine",
            Rot: 0,
            Sog: 12.5,
            Cog: 90.0,
            Heading: 180);

        // Act
        var result = NmeaEncoder.ToGprmc(record);

        // Assert
        result.ShouldStartWith("$");
        result.ShouldContain("GPRMC");
        result.ShouldContain("103045");
        result.ShouldContain("A"); // Status: Valid
        result.ShouldContain("*");
    }

    [Fact]
    public void ToGprmc_WithNorthLatitude_IncludesN()
    {
        // Arrange
        var record = new AisRecord(
            Timestamp: new DateTime(2024, 1, 15, 10, 30, 45),
            Mmsi: 220382000,
            Latitude: 55.1234,
            Longitude: -2.5678,
            NavigationalStatus: "Under way using engine",
            Rot: 0,
            Sog: 12.5,
            Cog: 90.0,
            Heading: 180);

        // Act
        var result = NmeaEncoder.ToGprmc(record);

        // Assert
        result.ShouldContain(",N,");
    }

    [Fact]
    public void ToGprmc_WithSouthLatitude_IncludesS()
    {
        // Arrange
        var record = new AisRecord(
            Timestamp: new DateTime(2024, 1, 15, 10, 30, 45),
            Mmsi: 220382000,
            Latitude: -33.8688,
            Longitude: 151.2093,
            NavigationalStatus: "Under way using engine",
            Rot: 0,
            Sog: 12.5,
            Cog: 90.0,
            Heading: 180);

        // Act
        var result = NmeaEncoder.ToGprmc(record);

        // Assert
        result.ShouldContain(",S,");
    }

    [Fact]
    public void ToGprmc_WithWestLongitude_IncludesW()
    {
        // Arrange
        var record = new AisRecord(
            Timestamp: new DateTime(2024, 1, 15, 10, 30, 45),
            Mmsi: 220382000,
            Latitude: 40.7128,
            Longitude: -74.0060,
            NavigationalStatus: "Under way using engine",
            Rot: 0,
            Sog: 12.5,
            Cog: 90.0,
            Heading: 180);

        // Act
        var result = NmeaEncoder.ToGprmc(record);

        // Assert
        result.ShouldContain(",W,");
    }

    [Fact]
    public void ToGprmc_WithEastLongitude_IncludesE()
    {
        // Arrange
        var record = new AisRecord(
            Timestamp: new DateTime(2024, 1, 15, 10, 30, 45),
            Mmsi: 220382000,
            Latitude: -33.8688,
            Longitude: 151.2093,
            NavigationalStatus: "Under way using engine",
            Rot: 0,
            Sog: 12.5,
            Cog: 90.0,
            Heading: 180);

        // Act
        var result = NmeaEncoder.ToGprmc(record);

        // Assert
        result.ShouldContain(",E,");
    }
}