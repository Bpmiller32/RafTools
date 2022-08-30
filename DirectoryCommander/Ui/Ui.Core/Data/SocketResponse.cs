namespace Ui.Core.Data;

public class SocketResponse
{
    public BuildStatus Status { get; set; }
    public int Progress { get; set; }
    public List<string> AvailableBuilds { get; set; }
    public string CurrentBuild { get; set; }
}
