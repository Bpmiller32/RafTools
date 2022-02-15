public class ParaFile
{
    public int Id { get; set; }

    public string FileName { get; set; }
    public string Size { get; set; }

    public string DataMonth { get; set; }
    public string DataYear { get; set; }
    public bool OnDisk { get; set; }
    public DateTime DateDownloaded { get; set; }
}
