using CommandLine;

namespace AisReplay;

public class Options
{
    [Option('f', "file", HelpText = "Path to a CSV file with AIS records")]
    public string? File { get; set; }

    [Option('d', "date", HelpText = "Download data for a specific date (YYYY-MM-DD)")]
    public string? Date { get; set; }

    [Option('m', "mmsi", HelpText = "Filter to a specific vessel by MMSI (Maritime Mobile Service Identity)")]
    public string? Mmsi { get; set; }

    [Option('x', "x-speed", Default = 1, HelpText = "Playback speed multiplier (default: 1)")]
    public int Speed { get; set; }

    [Option('g', "gps", Default = false, HelpText = "Output GPS format (GPRMC) instead of NMEA 0183")]
    public bool Gps { get; set; }

    [Option('s', "skip-moored", Default = false, HelpText = "Skip moored/stationary vessels")]
    public bool SkipMoored { get; set; }

    [Option('c', "purge-cache", Default = false, HelpText = "Clear cached downloads and exit")]
    public bool PurgeCache { get; set; }

    [Option('h', "host", Default = "127.0.0.1", HelpText = "UDP host/IP address to send events to (default: 127.0.0.1)")]
    public string Host { get; set; } = "127.0.0.1";

    [Option('p', "port", Default = 10110, HelpText = "UDP port to send events to (default: 10110)")]
    public int Port { get; set; }
}
