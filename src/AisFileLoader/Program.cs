using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AisFileLoader;

var cacheDir = Path.Combine(Path.GetTempPath(), "AisReplay");
var (options, exitCode) = ParseArguments(args);

if (options == null)
    return exitCode;

return await RunAsync(options, cacheDir);

static (Options?, int) ParseArguments(string[] args)
{
    var options = new Options();
    var inputs = new List<string>();
    var dates = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-i" or "--input":
                if (i + 1 < args.Length)
                    inputs.Add(args[++i]);
                break;
            case "-o" or "--output":
                if (i + 1 < args.Length)
                    options.Output = args[++i];
                break;
            case "-m" or "--mmsi-file":
                if (i + 1 < args.Length)
                    options.MmsiFile = args[++i];
                break;
            case "-l" or "--mmsi-list":
                if (i + 1 < args.Length)
                    options.MmsiList = args[++i];
                break;
            case "--mmsi-stdin":
                options.MmsiStdin = true;
                break;
            case "-e" or "--exclude":
                options.Exclude = true;
                break;
            case "-d" or "--date":
                if (i + 1 < args.Length)
                    dates.Add(args[++i]);
                break;
            case "-h" or "--help":
                PrintHelp();
                return (null, 0);
            case "--version":
                Console.WriteLine("AisFileLoader 0.3.5");
                return (null, 0);
            default:
                Console.Error.WriteLine($"Unknown argument: {args[i]}");
                PrintHelp();
                return (null, 1);
        }
    }

    options.Inputs = inputs.ToArray();
    options.Dates = dates.ToArray();
    return (options, 0);
}

static void PrintHelp()
{
    Console.WriteLine("""
        AisFileLoader - Filter Automatic Identification System (AIS) CSV data by vessel MMSI

        USAGE:
            AisFileLoader [OPTIONS]

        OPTIONS:
            -i, --input <FILE>         Input CSV file path(s) (can be specified multiple times)
            -o, --output <FILE>        Output CSV file path (default: write to stdout)
            -m, --mmsi-file <FILE>     File containing MMSI numbers to filter (one per line)
            -l, --mmsi-list <LIST>     Comma-separated list of MMSI numbers to filter
                --mmsi-stdin           Read MMSI numbers from stdin (one per line)
            -e, --exclude              Exclude the specified MMSIs instead of including only them
            -d, --date <DATE>          Download data from ais.dk for specific date(s) (YYYY-MM-DD, can be specified multiple times)
            -h, --help                 Show this help message
                --version              Show version information
        """);
}

async Task<int> RunAsync(Options options, string cacheDir)
{
    var mmsiFilter = new HashSet<int>();

    if (!string.IsNullOrEmpty(options.MmsiFile))
    {
        if (!File.Exists(options.MmsiFile))
        {
            Console.Error.WriteLine($"Error: MMSI file not found: {options.MmsiFile}");
            return 1;
        }
        var lines = await File.ReadAllLinesAsync(options.MmsiFile);
        foreach (var line in lines)
        {
            if (int.TryParse(line.Trim(), out var mmsi))
                mmsiFilter.Add(mmsi);
        }
        Console.Error.WriteLine($"Loaded {mmsiFilter.Count} MMSI numbers from file");
    }
    else if (!string.IsNullOrEmpty(options.MmsiList))
    {
        var mmsis = options.MmsiList.Split(',');
        foreach (var mmsi in mmsis)
        {
            if (int.TryParse(mmsi.Trim(), out var value))
                mmsiFilter.Add(value);
        }
        Console.Error.WriteLine($"Loaded {mmsiFilter.Count} MMSI numbers from list");
    }
    else if (options.MmsiStdin || Console.IsInputRedirected)
    {
        Console.Error.WriteLine("Reading MMSI numbers from stdin...");
        string? line;
        var stdinReader = new StreamReader(Console.OpenStandardInput());
        while ((line = await stdinReader.ReadLineAsync()) != null)
        {
            if (int.TryParse(line.Trim(), out var mmsi) && mmsi > 0)
                mmsiFilter.Add(mmsi);
        }
        Console.Error.WriteLine($"Loaded {mmsiFilter.Count} MMSI numbers from stdin");
    }

    if (mmsiFilter.Count == 0)
    {
        Console.Error.WriteLine("Error: No MMSI numbers specified. Use --mmsi-file, --mmsi-list, or pipe MMSI numbers to stdin");
        return 1;
    }

    var csvPaths = new List<string>();

    if (options.Dates.Length > 0)
    {
        foreach (var date in options.Dates)
        {
            csvPaths.Add(await DownloadAndExtract(date, cacheDir));
        }
    }

    if (options.Inputs.Length > 0)
    {
        csvPaths.AddRange(options.Inputs);
    }

    if (csvPaths.Count == 0)
    {
        Console.Error.WriteLine("Error: At least one --input file or --date is required");
        return 1;
    }

    foreach (var csvPath in csvPaths)
    {
        if (!File.Exists(csvPath))
        {
            Console.Error.WriteLine($"Error: CSV file not found: {csvPath}");
            return 1;
        }
    }

    Console.Error.WriteLine($"Reading from {csvPaths.Count} file(s):");
    foreach (var path in csvPaths)
        Console.Error.WriteLine($"  {path}");

    var outputDest = string.IsNullOrEmpty(options.Output) ? "stdout" : options.Output;
    Console.Error.WriteLine($"Writing to: {outputDest}");

    int totalRecords = 0;
    int filteredRecords = 0;
    bool headerWritten = false;

    using (var outputWriter = string.IsNullOrEmpty(options.Output)
        ? new StreamWriter(Console.OpenStandardOutput(), Encoding.UTF8)
        : new StreamWriter(options.Output, false, Encoding.UTF8))
    {
        foreach (var csvPath in csvPaths)
        {
            using (var inputReader = new StreamReader(csvPath))
            {
                if (!headerWritten)
                {
                    var header = await inputReader.ReadLineAsync();
                    if (header != null)
                    {
                        await outputWriter.WriteLineAsync(header);
                        headerWritten = true;
                    }
                }
                else
                {
                    await inputReader.ReadLineAsync();
                }

                string? line;
                while ((line = await inputReader.ReadLineAsync()) != null)
                {
                    totalRecords++;
                    var record = CsvParser.ParseAisRecord(line);

                    bool shouldInclude = mmsiFilter.Contains(record.Mmsi);
                    if (options.Exclude)
                        shouldInclude = !shouldInclude;

                    if (shouldInclude)
                    {
                        await outputWriter.WriteLineAsync(line);
                        filteredRecords++;
                    }
                }
            }
        }
    }

    Console.Error.WriteLine($"Processed {totalRecords} records from {csvPaths.Count} file(s), wrote {filteredRecords} records");
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
