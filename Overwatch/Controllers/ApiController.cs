using System.Collections.Generic;
using System.IO;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OverwatchApi.Workers;

namespace OverwatchApi.Controllers
{
    // / Redesign: add everything to a task and .Wait() for the task to finish, this allows for cancelationTokens to be introduced
    // / Better redesign: instead add CancelationTokens to TaskBucket.Bucket, logic to deal with a cancel (logic from Cleanup()?) 
    // ✔ Better redesign: removed try/catch inside the workers and bool if statements in controller. After thinking: these are a leftover from when the workers were developed standalone and no longer needed. Exceptions should always be caught and stop the procedure flow
    // ✔ Rename DirectoryBuilder to DirBuild, setting up for DirTest and DirBurn
    // - Create interface for Workers, add them to scoped services for dependency inversion
    [ApiController]
    [Route("api/[controller]")]
    public class DirBuildController : ControllerBase
    {
        private static object lockObj = new object();

        [HttpPost]
        public DirectoryStatus BuildDirs(DirectoryStatus bundle)
        {
            lock (lockObj)
            {
                // Create an object that will return if the directory task was created
                DirectoryStatus result = new DirectoryStatus()
                {
                    SmartMatch = false,
                    Parascript = false,
                    RoyalMail = false
                };

                // Clear all tasks in the TaskBucket that ran to completion
                List<string> tasks = new List<string>
                {
                    "SmartMatch", "Parascript", "RoyalMail"
                };
                foreach (var task in tasks)
                {
                    if (Jobs.Bucket.ContainsKey(task))
                    {
                        if (Jobs.Bucket[task].Status != TaskStatus.Running)
                        {
                            Jobs.Bucket.Remove(task);
                        }
                    }
                }

                // Parascript task
                if ((bundle.Parascript == true) && (Jobs.Bucket.ContainsKey("Parascript") == true))
                {
                    System.Console.WriteLine("Parascript task already exists");
                }
                if ((bundle.Parascript == true) && (Jobs.Bucket.ContainsKey("Parascript") == false))
                {
                    Jobs.PsPercent = 0;

                    Jobs.Bucket.Add("Parascript", Task.Run(async () =>
                    {
                        string psInputPath = Directory.GetCurrentDirectory() + @"\PS-Input";
                        string psWorkingPath = Directory.GetCurrentDirectory() + @"\PS-Working";
                        string psOutputPath = Directory.GetCurrentDirectory() + @"\PS-Output";

                        ParascriptWorker ps = new ParascriptWorker(psInputPath, psWorkingPath, psOutputPath, Jobs.PsProgress);

                        try
                        {
                            ps.CheckInput();
                            ps.Cleanup();
                            ps.FindDate();
                            await ps.Extract();
                            await ps.Archive();
                        }
                        catch (System.Exception e)
                        {
                            System.Console.WriteLine(DateTime.Now + " [PS] " + e.Message);
                        }
                    }));

                    // Represents PS task was simply created, nothing more
                    result.Parascript = true;
                }

                // RoyalMail task
                if ((bundle.RoyalMail == true) && (Jobs.Bucket.ContainsKey("RoyalMail") == true))
                {
                    System.Console.WriteLine("RoyalMail task already exists");
                }
                if ((bundle.RoyalMail == true) && (Jobs.Bucket.ContainsKey("RoyalMail") == false))
                {
                    Jobs.RmPercent = 0;

                    Jobs.Bucket.Add("RoyalMail", Task.Run(async () =>
                    {
                        string rmInputPath = Directory.GetCurrentDirectory() + @"\RM-Input";
                        string rmWorkingPath = Directory.GetCurrentDirectory() + @"\RM-Working";
                        string rmOutputPath = Directory.GetCurrentDirectory() + @"\RM-Output";

                        RoyalWorker rm = new RoyalWorker(rmInputPath, rmWorkingPath, rmOutputPath, Jobs.RmProgress);

                        try
                        {
                            rm.CheckInput();
                            rm.Cleanup();
                            rm.FindDate();
                            rm.UpdateSmiFiles();
                            rm.ConvertPafData();
                            await rm.Compile();
                            await rm.Output();
                        }
                        catch (System.Exception e)
                        {
                            System.Console.WriteLine(DateTime.Now + " [RM] " + e.Message);
                        }
                    }));

                    // Reprensets RM task was simply created, nothing more
                    result.RoyalMail = true;
                }

                return result;
            }
        }
    }
}
