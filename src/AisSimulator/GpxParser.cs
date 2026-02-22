using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using GeoTools;

namespace AisSimulator;

public static class GpxParser
{
    private static readonly XNamespace Gpx = "http://www.topografix.com/GPX/1/1";
    private static readonly XNamespace OpenCpn = "http://www.opencpn.org";

    public static Route ParseGpxFile(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var rte = doc.Root?.Element(Gpx + "rte");

        return rte == null 
            ? throw new InvalidOperationException("No route found in GPX file") 
            : ParseRoute(rte);
    }

    private static Route ParseRoute(XElement rteElement)
    {
        var name = rteElement.Element(Gpx + "name")?.Value ?? "Unknown Route";

        var extensions = rteElement.Element(Gpx + "extensions");
        var start = extensions?.Element(OpenCpn + "start")?.Value;
        var end = extensions?.Element(OpenCpn + "end")?.Value;

        var plannedSpeed = 0.0;
        if (double.TryParse(extensions?.Element(OpenCpn + "planned_speed")?.Value, out var speed))
            plannedSpeed = speed;

        var plannedDeparture = DateTime.MinValue;
        if (DateTime.TryParse(extensions?.Element(OpenCpn + "planned_departure")?.Value, out var departure))
            plannedDeparture = departure;

        var waypoints = rteElement
            .Elements(Gpx + "rtept")
            .Select(ParseWaypoint)
            .ToList();

        return new Route(name, start, end, plannedSpeed, plannedDeparture, waypoints);
    }

    private static RoutePoint ParseWaypoint(XElement rteptElement)
    {
        string latStr = rteptElement.Attribute("lat")?.Value ?? "0";
        string lonStr = rteptElement.Attribute("lon")?.Value ?? "0";

        if (!double.TryParse(latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude))
            throw new InvalidOperationException($"Invalid latitude value: {latStr}");
        if (!double.TryParse(lonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude))
            throw new InvalidOperationException($"Invalid longitude value: {lonStr}");

        var position = new Position(latitude, longitude);

        string name = rteptElement.Element(Gpx + "name")?.Value ?? "";
        string symbol = rteptElement.Element(Gpx + "sym")?.Value ?? "";
        string type = rteptElement.Element(Gpx + "type")?.Value ?? "";

        DateTime time = DateTime.MinValue;
        if (DateTime.TryParse(rteptElement.Element(Gpx + "time")?.Value, out DateTime parsedTime))
            time = parsedTime;

        return new RoutePoint(position, time, name, symbol, type);
    }
}