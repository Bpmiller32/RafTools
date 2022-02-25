namespace Ui.Core.Data;

public class SocketMessage
{
    public bool BuildSmartMatch { get; set; }
    public bool BuildParascript { get; set; }
    public bool BuildRoyalMail { get; set; }

    public string Month { get; set; }
    public string Year { get; set; }
    public string SmUser { get; set; }
    public string SmPass { get; set; }    
    public string Key { get; set; }
    
    public bool CheckStatus { get; set; }
}
