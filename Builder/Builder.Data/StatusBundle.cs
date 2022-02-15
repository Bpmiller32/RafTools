namespace Builder.App.Utils;

public class StatusBundle
{
    public BuildStatus Status { get; set; }
    public int Progress { get; set; }
    public List<string> AvailableBuilds { get; set; }
}
