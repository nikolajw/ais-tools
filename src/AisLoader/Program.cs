using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace AisLoader;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Option<string[]> dateOption = new("--date")
        {
            Description = "Date range (can be repeated)"
        };

        Option<uint[]> mmsiOption = new("--mmsi")
        {
            Description = "MMSI number to include in output"
        };

        RootCommand rootCommand = new("AIS Loader");

        rootCommand.Options.Add(dateOption);
        rootCommand.Options.Add(mmsiOption);

        var parseResult = rootCommand.Parse(args);

        if (parseResult.Errors.Count == 0)
        {
            var dates = parseResult.GetValue(dateOption);
            var mmsiFilter = parseResult.GetValue(mmsiOption);
            
            await foreach (var file in DownloadAndExtract(dates))
            {
                await WriteRecordsAsync(file, mmsiFilter);
            }

            return 0;
        }

        foreach (var parseError in parseResult.Errors)
        {
            await Console.Error.WriteLineAsync(parseError.Message);
        }

        return 1;
    }

    private static async Task<long> WriteRecordsAsync(FileInfo file, uint[]? mmsiFilter)
    {
        var totalRecords = 0;
        var filteredRecords = 0;
        var headerWritten = false;

        using var inputReader = file.OpenText();
        if (!headerWritten)
        {
            var header = await inputReader.ReadLineAsync();
            if (header != null)
            {
                await Console.Out.WriteLineAsync(header);
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

            var fields = line.Split(',');
            if (fields.Length < 3)
                continue;

            // Include line if no filter is specified
            if (mmsiFilter is { Length: 0 })
            {
                await Console.Out.WriteLineAsync(line);
                continue;
            }

            // Include line if MMSI matches the filter
            if (uint.TryParse(fields[2], out var mmsi) && mmsiFilter.Contains(mmsi))
            {
                await Console.Out.WriteLineAsync(line);
            }
        }
        
        return filteredRecords;
    }

    private static async IAsyncEnumerable<FileInfo> DownloadAndExtract(string[]? dates)
    {
        if (dates == null || dates.Length == 0) yield break;
        
        var cacheDir = Path.Combine(Path.GetTempPath(), "AisReplay");

        foreach (var date in dates)
        {
            var csv = Path.Combine(cacheDir, $"aisdk-{date}.csv");

            if (File.Exists(csv))
            {
                yield return new FileInfo(csv);
            }
            else
            {
                var zipPath = await DownloadZip(date, cacheDir);

                yield return await ExtractCsv(zipPath, cacheDir, csv);
            }
        }
    }

    private static async Task<FileInfo> ExtractCsv(string zipPath, string cacheDir, string csv)
    {
        await Console.Error.WriteLineAsync($"Extracting {zipPath} to {csv}");
        await ZipFile.ExtractToDirectoryAsync(zipPath, cacheDir, true);

        File.Delete(zipPath);

        if (!File.Exists(csv))
            throw new Exception($"Expected CSV not found after extraction: {csv}");
        await Console.Error.WriteLineAsync($"Ready: {csv}");
        
        return new FileInfo(csv);
    }

    private static async Task<string> DownloadZip(string date, string cacheDir)
    {
        var url = $"http://aisdata.ais.dk/aisdk-{date}.zip";
        var zipPath = Path.Combine(cacheDir, $"aisdk-{date}.zip");
        await Console.Error.WriteLineAsync($"Downloading {url} to {zipPath}...");

        using var client = new HttpClient();
        var stream = await client.GetStreamAsync(url);

        await using var fileStream = File.Create(zipPath);
        await stream.CopyToAsync(fileStream);
        
        return zipPath;
    }
}