namespace AisLoader;

using System;
using System.Collections.Generic;

public static class ArgumentParser
{
    public static (Options?, int) Parse(string[] args)
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
                    Console.WriteLine("AisLoader 0.3.6");
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

    private static void PrintHelp()
    {
        Console.WriteLine("""
            AisLoader - Filter Automatic Identification System (AIS) CSV data by vessel MMSI

            USAGE:
                AisLoader [OPTIONS]

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
}
