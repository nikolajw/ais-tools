using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AisReplay;

var cacheDir = Path.Combine(Path.GetTempPath(), "AisReplay");
var (options, exitCode) = ParseArguments(args);

if (options == null)
    return exitCode;

return await RunAsync(options, cacheDir);

static (Options?, int) ParseArguments(string[] args)
{
    var options = new Options();

    for (int i = 0; i < args.Length; i++)
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
                Console.WriteLine("AisReplay 0.3.5");
                return (null, 0);
            default:
                Console.Error.WriteLine($"Unknown argument: {args[i]}");
                PrintHelp();
                return (null, 1);
        }
    }

    return (options, 0);
}

static void PrintHelp()
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

async Task<int> RunAsync(Options options, string cacheDir)
{
    if (options.PurgeCache)
    {
        if (Directory.Exists(cacheDir))
        {
            Directory.Delete(cacheDir, true);
            Console.WriteLine($"Cache purged: {cacheDir}");
        }
        else
            Console.WriteLine("No cache to purge");
        return 0;
    }

    if (string.IsNullOrEmpty(options.File) && string.IsNullOrEmpty(options.Date))
    {
        Console.Error.WriteLine("Error: --file or --date is required");
        return 1;
    }

    var csvPath = string.IsNullOrEmpty(options.File)
        ? await DownloadAndExtract(options.Date!, cacheDir)
        : options.File;

    var mmsiFilter = string.IsNullOrEmpty(options.Mmsi) ? 0 : int.Parse(options.Mmsi);
    Console.WriteLine($"Replaying from {csvPath}{(mmsiFilter > 0 ? $", MMSI {mmsiFilter}" : ", all vessels")} at {options.Speed}x speed");
    Console.WriteLine($"Sending to {options.Host}:{options.Port}");

    using var udp = new UdpClient();
    var endpoint = new IPEndPoint(IPAddress.Parse(options.Host), options.Port);

    var prevTimestamp = DateTime.MinValue;
    var count = 0;

    using var reader = new StreamReader(csvPath);
    reader.ReadLine(); // skip header

    while (reader.ReadLine() is { } line)
    {
        var record = CsvParser.ParseAisRecord(line);
        var isMoored = options.SkipMoored
                       && record.NavigationalStatus != null
                       && record.NavigationalStatus.Contains("moored", StringComparison.OrdinalIgnoreCase);

        if (!isMoored && (mmsiFilter == 0 || record.Mmsi == mmsiFilter))
        {
            count++;
            if (count % options.Speed == 0)
            {
                if (prevTimestamp != DateTime.MinValue)
                {
                    var delay = record.Timestamp - prevTimestamp;
                    if (delay.TotalMilliseconds > 0.0)
                        Thread.Sleep(delay / options.Speed);
                }

                var sentence = options.Gps ? NmeaEncoder.ToGprmc(record) : NmeaEncoder.ToNmea0183(record);
                var bytes = Encoding.ASCII.GetBytes(sentence + "\r\n");
                udp.Send(bytes, bytes.Length, endpoint);
                Console.WriteLine(record);
                prevTimestamp = record.Timestamp;
            }
        }
    }

    return 0;
}

async Task<string> DownloadAndExtract(string date, string cacheDir)
{
    Directory.CreateDirectory(cacheDir);
    var csv = Path.Combine(cacheDir, $"aisdk-{date}.csv");
    if (File.Exists(csv))
    {
        Console.WriteLine($"Using cached {csv}");
        return csv;
    }

    var url = $"http://aisdata.ais.dk/aisdk-{date}.zip";
    var zipPath = Path.Combine(cacheDir, $"aisdk-{date}.zip");
    Console.WriteLine($"Downloading {url} ...");
    using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
    using var response = await client.GetAsync(url);
    response.EnsureSuccessStatusCode();
    using (var fs = File.Create(zipPath))
    {
        await response.Content.CopyToAsync(fs);
    }
    Console.WriteLine("Extracting ...");
    ZipFile.ExtractToDirectory(zipPath, cacheDir, true);
    File.Delete(zipPath);
    if (!File.Exists(csv))
        throw new Exception($"Expected CSV not found after extraction: {csv}");
    Console.WriteLine($"Ready: {csv}");
    return csv;
}
