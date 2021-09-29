using System.Diagnostics;
using System.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OverwatchApi.Data
{
    public class TaskBucket
    {
        // Creates singleton
        public static readonly TaskBucket instance = new TaskBucket();
        private TaskBucket(){}

        public static Stopwatch sw = Stopwatch.StartNew();

        public static Dictionary<string, Task> Bucket { get; set; } = new Dictionary<string, Task>();
        
        public static int SmPercent { get; set; }
        public static Progress<int> SmProgress { get; set; } = new Progress<int>((percent) =>
        {
            SmPercent += percent;
            System.Console.WriteLine("SM Progress: " + SmPercent + " : " + sw.Elapsed);
        });
        public static int PsPercent { get; set; }
        public static Progress<int> PsProgress { get; set; } = new Progress<int>((percent) =>
        {
            PsPercent += percent;
            System.Console.WriteLine("PS Progress: " + PsPercent + " : " + sw.Elapsed);
        });
        public static int RmPercent { get; set; }
        public static Progress<int> RmProgress { get; set; } = new Progress<int>((percent) =>
        {
            RmPercent += percent;
            System.Console.WriteLine("RM Progress: " + RmPercent + " : " + sw.Elapsed);
            File.AppendAllText(@"C:\Users\billy\Desktop\RM-Log.txt", "RM Progress: " + RmPercent + " : " + sw.Elapsed + Environment.NewLine);
        });
    }
}