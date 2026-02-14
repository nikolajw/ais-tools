using System.IO;

namespace AisLoader;

using System.CommandLine;

public static class RootCommandFactory
{
    public static RootCommand Create()
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

        return cmd;
    }
}