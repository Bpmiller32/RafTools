namespace Server.Common;

public class DirModule
{
    public ModuleStatus Status { get; set; }
    public int Progress { get; set; }
    public string Message { get; set; }
    public ModuleSettings Settings { get; set; } = new();
}
