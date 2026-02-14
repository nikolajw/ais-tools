namespace AisFileLoader;

public class Options
{
    public string[] Inputs { get; set; } = [];
    public string? Output { get; set; }
    public string? MmsiFile { get; set; }
    public string? MmsiList { get; set; }
    public bool MmsiStdin { get; set; }
    public bool Exclude { get; set; }
    public string[] Dates { get; set; } = [];
}
