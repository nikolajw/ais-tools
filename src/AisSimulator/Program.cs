using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeoTools;

namespace AisSimulator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Option<FileInfo> gpxOption = new("gpx")
        {
            Description = "Path to GPX file containing the route"
        };
        Option<string> mmsiOption = new("-m", "--mmsi")
        {
            Description = "MMSI number for the simulated vessel",
            DefaultValueFactory =  _ => "000000000"
        };
        Option<string> nameOption = new("-n", "--name")
        {
            Description = "Vessel name",
            DefaultValueFactory = _ => "Simulated Vessel"
        };
        Option<DateTime> startTimeOption = new("-s", "--start-time")
        {
            Description = "Start time for simulation (default: now)",
            DefaultValueFactory = _ => DateTime.Now
        };
        Option<double> intervalOption = new("-i", "--interval")
        {
            Description = "Time interval in seconds between position reports (default: 60)",
            DefaultValueFactory = _ => 60.0
        };
        Option<double> speedOption = new("-x", "--x-speed")
        {
            Description = "Speed multiplier (affects playback speed, not vessel speed)",
            DefaultValueFactory = _ => 1.0
        };

        RootCommand rootCommand = new();

        rootCommand.Options.Add(gpxOption);
        rootCommand.Options.Add(mmsiOption);
        rootCommand.Options.Add(nameOption);
        rootCommand.Options.Add(startTimeOption);
        rootCommand.Options.Add(intervalOption);
        rootCommand.Options.Add(speedOption);

        rootCommand.SetAction(async parseResult =>
        {
            var gpxFile = parseResult.GetValue(gpxOption)!;
            var mmsi = parseResult.GetValue(mmsiOption);
            var name = parseResult.GetValue(nameOption)!;
            var startTime = parseResult.GetValue(startTimeOption);
            var interval = parseResult.GetValue(intervalOption);
            var speed = parseResult.GetValue(speedOption);

            return await GeneratePositions(gpxFile, mmsi, name, startTime, interval, speed);
        });

        var parseResult = rootCommand.Parse(args);
        await parseResult.InvokeAsync();

        return 0;
    }

    private static async Task<int> GeneratePositions(FileInfo gpxFile, string mmsi, string name,
        DateTime startTime, double interval, double speedMultiplier)
    {
        try
        {
            if (!gpxFile.Exists)
            {
                Console.Error.WriteLine($"Error: GPX file not found: {gpxFile.FullName}");
                return 1;
            }

            Route route = GpxParser.ParseGpxFile(gpxFile.FullName);

            Console.Error.WriteLine($"Loaded route: {route.Name}");
            Console.Error.WriteLine($"Waypoints: {route.Waypoints.Count}");
            Console.Error.WriteLine($"Generating positions for vessel {name} (MMSI: {mmsi})");
            Console.Error.WriteLine($"Start time: {startTime:dd/MM/yyyy HH:mm:ss}");
            Console.Error.WriteLine($"Interval: {interval} seconds");
            Console.Error.WriteLine($"Speed multiplier: {speedMultiplier}");

            // Write CSV header
            Console.WriteLine("Timestamp,Vessel Name,MMSI,Latitude,Longitude,NavigationalStatus,ROT,SOG,COG,Heading");

            await GenerateRealTimePositions(route, mmsi, name, startTime, interval, route.PlannedSpeed, speedMultiplier);

            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error: {e.Message}");
            Console.Error.WriteLine($"Stack trace: {e.StackTrace}");
            return 1;
        }
    }

    private static async Task GenerateRealTimePositions(Route route, string mmsi, string name,
        DateTime currentTime, double interval, double speedKnots, double speedMultiplier)
    {
        var legs = route.GetLegs();
        var currentLegIndex = 0;
        var distanceAlongLeg = 0.0; // Distance traveled along current leg in nautical miles

        // Real-world delay between positions, adjusted by speed multiplier
        var delayMilliseconds = (int)(interval * 1000 / speedMultiplier);

        while (currentLegIndex < legs.Count)
        {
            var leg = legs[currentLegIndex];

            // Distance to travel in this interval (in nautical miles)
            var distanceInInterval = (speedKnots * interval) / 3600.0;

            Position nextPosition;
            double course = leg.Course;
            double sog = speedKnots;

            // Check if we can complete this interval within the current leg
            if (distanceAlongLeg + distanceInInterval <= leg.DistanceNauticalMiles)
            {
                // Position is within current leg
                distanceAlongLeg += distanceInInterval;
                nextPosition = LatLongCalculations.DestinationPoint(
                    leg.Start,
                    leg.Course,
                    distanceAlongLeg);
            }
            else
            {
                // Next position would exceed current leg
                // Use the endpoint of current leg and move to next leg
                nextPosition = leg.End;

                // Calculate remaining distance for next leg
                var remainingDistance = distanceInInterval - (leg.DistanceNauticalMiles - distanceAlongLeg);
                currentLegIndex++;

                if (currentLegIndex < legs.Count)
                {
                    distanceAlongLeg = remainingDistance;
                    course = legs[currentLegIndex].Course;
                }
                else
                {
                    break; // Reached end of route
                }
            }

            // Output position in CSV format
            var timestamp = currentTime.ToString("dd/MM/yyyy HH:mm:ss");
            Console.WriteLine($"{timestamp},{name},{mmsi},{nextPosition.Latitude},{nextPosition.Longitude},0,0,{sog},{course},0");

            currentTime = currentTime.AddSeconds(interval);
            await Task.Delay(delayMilliseconds);
        }
    }
}