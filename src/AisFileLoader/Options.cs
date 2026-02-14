using CommandLine;

namespace AisFileLoader;

public class Options
{
    [Option('i', "input", Required = true, HelpText = "Input CSV file path (or URL to ais.dk data)")]
    public string Input { get; set; } = "";

    [Option('o', "output", HelpText = "Output CSV file path (default: write to stdout)")]
    public string? Output { get; set; }

    [Option('m', "mmsi-file", HelpText = "File containing MMSI numbers to filter (one per line)")]
    public string? MmsiFile { get; set; }

    [Option('l', "mmsi-list", HelpText = "Comma-separated list of MMSI numbers to filter")]
    public string? MmsiList { get; set; }

    [Option("mmsi-stdin", Default = false, HelpText = "Read MMSI numbers from stdin (one per line)")]
    public bool MmsiStdin { get; set; }

    [Option('e', "exclude", Default = false, HelpText = "Exclude the specified MMSIs instead of including only them")]
    public bool Exclude { get; set; }

    [Option('d', "date", HelpText = "Download data from ais.dk for a specific date (YYYY-MM-DD)")]
    public string? Date { get; set; }
}
