using System;
using System.Globalization;

namespace AisFileLoader;

public static class CsvParser
{
    public static AisRecord ParseAisRecord(string line)
    {
        var fields = line.Split(',');
        return new AisRecord(
            Timestamp: DateTime.ParseExact(fields[0], "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
            Mmsi: int.Parse(fields[2]),
            Latitude: ParseDouble(fields[3]),
            Longitude: ParseDouble(fields[4]),
            NavigationalStatus: string.IsNullOrWhiteSpace(fields[5]) ? null : fields[5],
            Rot: ParseDouble(fields[6]),
            Sog: ParseDouble(fields[7]),
            Cog: ParseDouble(fields[8]),
            Heading: ParseInt(fields[9]));
    }

    private static double ParseDouble(string s) =>
        double.TryParse(s, out var result) ? result : 0;

    private static int ParseInt(string s) =>
        string.IsNullOrWhiteSpace(s) ? -1 : int.Parse(s);
}
