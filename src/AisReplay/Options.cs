namespace AisReplay;

public class Options
{
    public string? File { get; set; }
    public string? Date { get; set; }
    public string? Mmsi { get; set; }
    public int Speed { get; set; } = 1;
    public bool Gps { get; set; }
    public bool SkipMoored { get; set; }
    public bool PurgeCache { get; set; }
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 10110;
}
