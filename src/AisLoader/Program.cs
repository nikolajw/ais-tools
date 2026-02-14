using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AisLoader;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var options = ArgsParser.Parse(args);
        var cacheDir = Path.Combine(Path.GetTempPath(), "AisReplay");

        var mmsiFilter = await GetMmsiList(options);
        var csvPaths = await GetCsvPaths(cacheDir, options);

        await Console.Error.WriteLineAsync($"Reading from {csvPaths.Count} file(s):");
        foreach (var path in csvPaths)
            await Console.Error.WriteLineAsync($"  {path}");

        var outputDest = GetOutputDestination(options);
        await Console.Error.WriteLineAsync($"Writing to: {outputDest}");


        await using var outputWriter = string.IsNullOrEmpty(outputDest)
            ? new StreamWriter(Console.OpenStandardOutput(), Encoding.UTF8)
            : new StreamWriter(outputDest, false, Encoding.UTF8);

        return await WriteRecords(csvPaths, outputWriter, mmsiFilter, options);
    }

    private static async Task<int> WriteRecords(List<string> csvPaths, StreamWriter outputWriter, HashSet<int> mmsiFilter, Options options)
    {
        var totalRecords = 0;
        var filteredRecords = 0;
        var headerWritten = false;

        foreach (var csvPath in csvPaths)
        {
            using var inputReader = new StreamReader(csvPath);
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

            while (await inputReader.ReadLineAsync() is { } line)
            {
                totalRecords++;
                var record = CsvParser.ParseAisRecord(line);

                var shouldInclude = mmsiFilter.Contains(record.Mmsi);
                if (options.Exclude)
                    shouldInclude = !shouldInclude;

                if (!shouldInclude) continue;

                await outputWriter.WriteLineAsync(line);
                filteredRecords++;
            }
        }

        await Console.Error.WriteLineAsync(
            $"Processed {totalRecords} records from {csvPaths.Count} file(s), wrote {filteredRecords} records");
        return 0;
    }

    private static string GetOutputDestination(Options options)
    {
        return string.IsNullOrEmpty(options.Output) ? "stdout" : options.Output;
    }

    private static async Task<List<string>> GetCsvPaths(string cacheDir, Options options)
    {
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
            return csvPaths;
        }
        
        foreach (var csvPath in csvPaths)
        {
            if (!File.Exists(csvPath))
            {
                Console.Error.WriteLine($"Error: CSV file not found: {csvPath}");
                return new List<string>();
            }
        }


        return csvPaths;
    }

    private static async Task<HashSet<int>> GetMmsiList(Options options)
    {
        var mmsiFilter = new HashSet<int>();

        if (!string.IsNullOrEmpty(options.MmsiFile))
        {
            if (!File.Exists(options.MmsiFile))
            {
                Console.Error.WriteLine($"Error: MMSI file not found: {options.MmsiFile}");
                return mmsiFilter;
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
            Console.Error.WriteLine(
                "Error: No MMSI numbers specified. Use --mmsi-file, --mmsi-list, or pipe MMSI numbers to stdin");
        }

        return mmsiFilter;
    }

    private static async Task<string> DownloadAndExtract(string date, string cacheDir)
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
}