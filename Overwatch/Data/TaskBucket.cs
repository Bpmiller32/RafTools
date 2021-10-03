using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OverwatchApi.Data
{
    public class TaskBucket
    {
        // Creates singleton
        public static readonly TaskBucket instance = new TaskBucket();
        private TaskBucket(){}

        public static Dictionary<string, Task> Bucket { get; set; } = new Dictionary<string, Task>();
        
        public static int SmPercent { get; set; }
        public static Progress<int> SmProgress { get; set; } = new Progress<int>((percent) =>
        {
            SmPercent += percent;
            System.Console.WriteLine(DateTime.Now + " : SM Progress : " + SmPercent);
        });
        public static int PsPercent { get; set; }
        public static Progress<int> PsProgress { get; set; } = new Progress<int>((percent) =>
        {
            PsPercent += percent;
            System.Console.WriteLine(DateTime.Now + " : PS Progress : " + PsPercent);
        });
        public static int RmPercent { get; set; }
        public static Progress<int> RmProgress { get; set; } = new Progress<int>((percent) =>
        {
            RmPercent += percent;
            System.Console.WriteLine(DateTime.Now + " : RM Progress : " + RmPercent);
        });
    }
}