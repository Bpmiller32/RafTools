namespace Common.Data;

public class SocketResponse
{
    // Universal
    public string DirectoryStatus { get; set; }
    public bool AutoEnabled { get; set; }
    public string AutoDate { get; set; }

    // Specific to Crawler
    public List<BuildInfo> AvailableBuilds { get; set; }

    // Specific to Builder
    public List<BuildInfo> CompiledBuilds { get; set; }
    public string CurrentBuild { get; set; }
    public int Progress { get; set; }
}