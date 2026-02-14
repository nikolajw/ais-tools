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
using CommandLine;

var cacheDir = Path.Combine(Path.GetTempPath(), "AisReplay");

return await Parser.Default.ParseArguments<Options>(args)
    .MapResult(
        options => RunAsync(options, cacheDir),
        _ => Task.FromResult(1)
    );

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
        ? DownloadAndExtract(options.Date!, cacheDir)
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

    string DownloadAndExtract(string date, string cacheDir)
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
        using var response = client.GetAsync(url).Result;
        response.EnsureSuccessStatusCode();
        using (var fs = File.Create(zipPath))
        {
            response.Content.CopyToAsync(fs).Wait();
        }
        Console.WriteLine("Extracting ...");
        ZipFile.ExtractToDirectory(zipPath, cacheDir, true);
        File.Delete(zipPath);
        if (!File.Exists(csv))
            throw new Exception($"Expected CSV not found after extraction: {csv}");
        Console.WriteLine($"Ready: {csv}");
        return csv;
    }
}
