using System;

namespace Common.Data;

// public class SocketMessage
// {
//     public bool BuildSmartMatch { get; set; }
//     public bool BuildParascript { get; set; }
//     public bool BuildRoyalMail { get; set; }

//     public string Month { get; set; }
//     public string Year { get; set; }
//     public string SmUser { get; set; }
//     public string SmPass { get; set; }
//     public string Key { get; set; }

//     public bool CheckStatus { get; set; }
// }
public class SocketMessage
{
    public string Crawler { get; set; }
    public string Property { get; set; }
    public string Value { get; set; }
}