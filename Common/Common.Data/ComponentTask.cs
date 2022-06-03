namespace Common.Data;

public class ComponentTask
{
    public ComponentStatus Email { get; set; }
    public ComponentStatus SmartMatch { get; set; }
    public ComponentStatus Parascript { get; set; }
    public ComponentStatus RoyalMail { get; set; }

    public int ProgressSmartMatch { get; set; }
    public int ProgressParascript { get; set; }
    public int ProgressRoyalMail { get; set; }

    public void ChangeProgress(DirectoryType directory, int changeAmount)
    {
        if (directory == DirectoryType.SmartMatch)
        {
            ProgressSmartMatch = ProgressSmartMatch + changeAmount;
        }
        else if (directory == DirectoryType.Parascript)
        {
            ProgressParascript = ProgressParascript + changeAmount;
        }
        else if (directory == DirectoryType.RoyalMail)
        {
            ProgressRoyalMail = ProgressRoyalMail + changeAmount;
        }
    }
}