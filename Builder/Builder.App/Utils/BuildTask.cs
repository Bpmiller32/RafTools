namespace Builder.App.Utils;

public class BuildTask
{
    public string Name { get; set; }
    public Task Task { get; set; }
    public BuildStatus Status { get; set; }
    public int Progress { get; set; }
    public List<string> AvailableBuilds { get; set; }
}
