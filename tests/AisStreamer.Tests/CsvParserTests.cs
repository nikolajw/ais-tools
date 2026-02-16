namespace AisStreamer.Tests;

public class CsvParserTests
{
    [Fact]
    public void ParseAisRecord_WithValidLine_ParsesSuccessfully()
    {
        // Arrange
        var line = "15/01/2024 10:30:45,VESSEL NAME,220382000,55.1234,-2.5678,Under way using engine,0,12.5,90.0,180";

        // Act
        var record = CsvParser.ParseAisRecord(line);

        // Assert
        record.Timestamp.ShouldBe(new System.DateTime(2024, 1, 15, 10, 30, 45));
        record.Mmsi.ShouldBe(220382000);
        record.Latitude.ShouldBe(55.1234);
        record.Longitude.ShouldBe(-2.5678);
        record.NavigationalStatus.ShouldBe("Under way using engine");
        record.Rot.ShouldBe(0);
        record.Sog.ShouldBe(12.5);
        record.Cog.ShouldBe(90.0);
        record.Heading.ShouldBe(180);
    }

    [Fact]
    public void ParseAisRecord_WithEmptyLine_ReturnsNone()
    {
        // Arrange
        var line = "";

        // Act
        var record = CsvParser.ParseAisRecord(line);

        // Assert
        record.ShouldBe(AisRecord.None);
    }

    [Fact]
    public void ParseAisRecord_WithCommentLine_ReturnsNone()
    {
        // Arrange
        var line = "# This is a comment";

        // Act
        var record = CsvParser.ParseAisRecord(line);

        // Assert
        record.ShouldBe(AisRecord.None);
    }

    [Fact]
    public void ParseAisRecord_WithMissingNumericFields_DefaultsToZero()
    {
        // Arrange
        var line = "15/01/2024 10:30:45,VESSEL NAME,220382000,55.1234,-2.5678,,0,,90.0,";

        // Act
        var record = CsvParser.ParseAisRecord(line);

        // Assert
        record.Rot.ShouldBe(0);
        record.Sog.ShouldBe(0);
        record.Heading.ShouldBe(-1);
    }

    [Fact]
    public void ParseAisRecord_WithNullNavigationalStatus_ParsesAsNull()
    {
        // Arrange
        var line = "15/01/2024 10:30:45,VESSEL NAME,220382000,55.1234,-2.5678,,0,12.5,90.0,180";

        // Act
        var record = CsvParser.ParseAisRecord(line);

        // Assert
        record.NavigationalStatus.ShouldBeNull();
    }

    [Theory]
    [InlineData("15/01/2024 10:30:45,VESSEL,123456789,40.7128,-74.0060,At anchor,0,0,0,0")]
    [InlineData("01/12/2025 23:59:59,SHIP,987654321,-33.8688,151.2093,Under way sailing,45.5,25.3,180.0,270")]
    public void ParseAisRecord_WithVariousValidInputs_ParsesSuccessfully(string line)
    {
        // Act
        var record = CsvParser.ParseAisRecord(line);

        // Assert
        record.ShouldNotBe(AisRecord.None);
        record.Mmsi.ShouldBeGreaterThan(0);
    }
}