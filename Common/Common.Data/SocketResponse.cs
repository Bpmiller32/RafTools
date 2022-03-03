using System;
using System.Collections.Generic;

namespace Common.Data;

public class SocketResponse
{
    public ComponentStatus Status { get; set; }
    public int Progress { get; set; }
    public List<string> AvailableBuilds { get; set; }
    public string CurrentBuild { get; set; }
}