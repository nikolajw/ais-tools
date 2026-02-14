namespace AisReplay;

using System;

public static class ArgumentParser
{
    public static (Options?, int) Parse(string[] args)
    {
        var options = new Options();

        for (var i = 0; i < args.Length; i++)
        {
            var result = ParseArgument(args, options, i);
            if (result.options == null)
                return (null, result.exitCode);
            options = result.options;
        }

        return (options, 0);
    }

    private static (Options? options, int exitCode) ParseArgument(string[] args, Options options, int i)
    {
        switch (args[i])
        {
            case "-f" or "--file":
                if (i + 1 < args.Length)
                    options.File = args[++i];
                break;
            case "-d" or "--date":
                if (i + 1 < args.Length)
                    options.Date = args[++i];
                break;
            case "-m" or "--mmsi":
                if (i + 1 < args.Length)
                    options.Mmsi = args[++i];
                break;
            case "-x" or "--x-speed":
                if (i + 1 < args.Length && int.TryParse(args[++i], out var speed))
                    options.Speed = speed;
                break;
            case "-g" or "--gps":
                options.Gps = true;
                break;
            case "-s" or "--skip-moored":
                options.SkipMoored = true;
                break;
            case "-c" or "--purge-cache":
                options.PurgeCache = true;
                break;
            case "--host":
                if (i + 1 < args.Length)
                    options.Host = args[++i];
                break;
            case "-p" or "--port":
                if (i + 1 < args.Length && int.TryParse(args[++i], out var port))
                    options.Port = port;
                break;
            case "-h" or "--help":
                PrintHelp();
                return (null, 0);
            case "--version":
                Console.WriteLine("AisReplay 0.3.6");
                return (null, 0);
            default:
                Console.Error.WriteLine($"Unknown argument: {args[i]}");
                PrintHelp();
                return (null, 1);
        }

        return (options, 0);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            AisReplay - Replay Automatic Identification System (AIS) vessel tracking data via UDP

            USAGE:
                AisReplay [OPTIONS]

            OPTIONS:
                -f, --file <FILE>          Path to a CSV file with AIS records
                -d, --date <DATE>          Download data for a specific date (YYYY-MM-DD)
                -m, --mmsi <MMSI>          Filter to a specific vessel by MMSI
                -x, --x-speed <SPEED>      Playback speed multiplier (default: 1)
                -g, --gps                  Output GPS format (GPRMC) instead of NMEA 0183
                -s, --skip-moored          Skip moored/stationary vessels
                -c, --purge-cache          Clear cached downloads and exit
                    --host <HOST>          UDP host/IP address to send events to (default: 127.0.0.1)
                -p, --port <PORT>          UDP port to send events to (default: 10110)
                -h, --help                 Show this help message
                    --version              Show version information
            """);
    }
}
