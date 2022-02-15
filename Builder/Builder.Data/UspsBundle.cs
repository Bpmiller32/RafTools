public class UspsBundle
{
    public int Id { get; set; }

    public int DataMonth { get; set; }
    public int DataYear { get; set; }
    public string Cycle { get; set; }
    public bool IsReadyForBuild { get; set; }
    public bool IsBuildComplete { get; set; }
    public List<UspsFile> BuildFiles { get; set; } = new List<UspsFile>();
}
