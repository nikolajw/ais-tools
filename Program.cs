using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using AisReplay;

var cacheDir = Path.Combine(Path.GetTempPath(), "AisReplay");

string file = "", date = "", mmsi = "";
int speed = 1;
bool gps = false, skipMoored = false;

for (int i = 0; i < args.Length;)
{
    switch (args[i])
    {
        case "--file" when i + 1 < args.Length:
            file = args[i + 1]; i += 2; break;
        case "--date" when i + 1 < args.Length:
            date = args[i + 1]; i += 2; break;
        case "--mmsi" when i + 1 < args.Length:
            mmsi = args[i + 1]; i += 2; break;
        case "--x-speed" when i + 1 < args.Length:
            speed = int.Parse(args[i + 1]); i += 2; break;
        case "--gps":
            gps = true; i += 1; break;
        case "--skip-moored":
            skipMoored = true; i += 1; break;
        case "--purge-cache":
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
                Console.WriteLine($"Cache purged: {cacheDir}");
            }
            else
                Console.WriteLine("No cache to purge");
            return 0;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            Console.Error.WriteLine("Usage: AisReplay (--file <csv> | --date <YYYY-MM-DD>) [--mmsi <mmsi>] [--x-speed <multiplier>] [--gps] [--skip-moored]");
            return 1;
    }
}

if (string.IsNullOrEmpty(file) && string.IsNullOrEmpty(date))
{
    Console.Error.WriteLine("Error: --file or --date is required");
    Console.Error.WriteLine("Usage: AisReplay (--file <csv> | --date <YYYY-MM-DD>) [--mmsi <mmsi>] [--x-speed <multiplier>] [--gps] [--skip-moored]");
    return 1;
}

string csvPath;
if (!string.IsNullOrEmpty(file))
{
    csvPath = file;
}
else
{
    csvPath = DownloadAndExtract(date);
}

int mmsiFilter = string.IsNullOrEmpty(mmsi) ? 0 : int.Parse(mmsi);
Console.WriteLine($"Replaying from {csvPath}{(mmsiFilter > 0 ? $", MMSI {mmsiFilter}" : ", all vessels")} at {speed}x speed");

using var udp = new UdpClient();
var endpoint = new IPEndPoint(IPAddress.Loopback, 10110);

var prevTimestamp = DateTime.MinValue;
int count = 0;

using var reader = new StreamReader(csvPath);
reader.ReadLine(); // skip header

while (reader.ReadLine() is { } line)
{
    var record = CsvParser.ParseAisRecord(line);
    var isMoored = skipMoored
                   && record.NavigationalStatus != null
                   && record.NavigationalStatus.Contains("moored", StringComparison.OrdinalIgnoreCase);

    if (!isMoored && (mmsiFilter == 0 || record.Mmsi == mmsiFilter))
    {
        count++;
        if (count % speed == 0)
        {
            if (prevTimestamp != DateTime.MinValue)
            {
                var delay = record.Timestamp - prevTimestamp;
                if (delay.TotalMilliseconds > 0.0)
                    Thread.Sleep(delay / speed);
            }

            var sentence = gps ? NmeaEncoder.ToGprmc(record) : NmeaEncoder.ToNmea0183(record);
            var bytes = Encoding.ASCII.GetBytes(sentence + "\r\n");
            udp.Send(bytes, bytes.Length, endpoint);
            Console.WriteLine(record);
            prevTimestamp = record.Timestamp;
        }
    }
}

return 0;

string DownloadAndExtract(string date)
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
