using System.CommandLine;
using System.IO;
using System.Linq;

namespace AisLoader;

public static class ArgsParser
{
    public static Options Parse(string[] args)
    {
        var inputOption = new Option<FileInfo[]>("-i", "--input")
        {
            Arity = ArgumentArity.ZeroOrMore,
            Description = "Input CSV file path(s) (can be specified multiple times)"
        };

        var outputOption = new Option<FileInfo?>("-o", "--output")
        {
            Description = "Output CSV file path (default: write to stdout)"
        };

        var mmsiFileOption = new Option<FileInfo?>("-m", "--mmsi-file")
        {
            Description = "File containing MMSI numbers to filter (one per line)"
        };

        var mmsiListOption = new Option<string?>("-l", "--mmsi-list")
        {
            Description = "Comma-separated list of MMSI numbers to filter"
        };

        var mmsiStdinOption = new Option<bool>("--mmsi-stdin")
        {
            Description = "Read MMSI numbers from stdin (one per line)"
        };

        var excludeOption = new Option<bool>("-e", "--exclude")
        {
            Description = "Exclude the specified MMSIs instead of including only them"
        };

        var dateOption = new Option<string[]>("-d", "--date")
        {
            Arity = ArgumentArity.ZeroOrMore,
            Description = "Download data from ais.dk for specific date(s) (YYYY-MM-DD, can be specified multiple times)"
        };

        var cmd = new RootCommand();
        cmd.Options.Add(inputOption);
        cmd.Options.Add(outputOption);
        cmd.Options.Add(mmsiFileOption);
        cmd.Options.Add(mmsiListOption);
        cmd.Options.Add(mmsiStdinOption);
        cmd.Options.Add(excludeOption);
        cmd.Options.Add(dateOption);

        var result = cmd.Parse(args);

        var inputs = result.CommandResult.GetValue(inputOption) ?? [];
        var output = result.CommandResult.GetValue(outputOption);
        var mmsiFile = result.CommandResult.GetValue(mmsiFileOption);
        var mmsiList = result.CommandResult.GetValue(mmsiListOption);
        var mmsiStdin = result.CommandResult.GetValue(mmsiStdinOption);
        var exclude = result.CommandResult.GetValue(excludeOption);
        var dates = result.CommandResult.GetValue(dateOption) ?? [];

        return new Options
        {
            Inputs = inputs.Select(f => f.FullName).ToArray(),
            Output = output?.FullName,
            MmsiFile = mmsiFile?.FullName,
            MmsiList = mmsiList,
            MmsiStdin = mmsiStdin,
            Exclude = exclude,
            Dates = dates
        };
    }
}