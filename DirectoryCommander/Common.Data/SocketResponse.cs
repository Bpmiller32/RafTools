namespace Common.Data;

public class SocketResponse
{
    // Used by Crawler + Builder
    public string DirectoryStatus { get; set; }
    public bool AutoEnabled { get; set; }
    public string AutoDate { get; set; }

    // Used by Builder + Tester
    public int Progress { get; set; }
    public string CurrentBuild { get; set; }

    // Specific to Crawler
    public List<BuildInfo> AvailableBuilds { get; set; }

    // Specific to Builder
    public List<BuildInfo> CompiledBuilds { get; set; }
}