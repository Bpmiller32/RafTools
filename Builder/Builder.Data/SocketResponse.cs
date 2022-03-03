namespace Builder.App.Utils;

public class SocketResponse
{
    public BuildStatus Status { get; set; }
    public int Progress { get; set; }
    public string CurrentBuild { get; set; }
    public List<string> AvailableBuilds { get; set; }
}
