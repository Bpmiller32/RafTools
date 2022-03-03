namespace Common.Data;

public class BuildTask
{
    public string Name { get; set; }
    public Task Task { get; set; }
    public ComponentStatus Status { get; set; }
    public int Progress { get; set; }
    public string CurrentBuild { get; set; }
    
    public void ChangeProgress(int changeAmount)
    {
        Progress = Progress + changeAmount;
    }
}
