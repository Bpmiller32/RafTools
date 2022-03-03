using System;

namespace Common.Data;

public class CrawlTask
{
    public ComponentStatus Email { get; set; }
    public ComponentStatus SmartMatch { get; set; }
    public ComponentStatus Parascript { get; set; }
    public ComponentStatus RoyalMail { get; set; }
}