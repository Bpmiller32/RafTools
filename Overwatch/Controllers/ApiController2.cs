using System.Threading;
using System.Collections.Generic;
using System.IO;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OverwatchApi.Data;
using OverwatchApi.Controllers;

namespace OverwatchApi.Controllers
{
    // - Redesign: add everything to a task and .Wait() for the task to finish, this allows for cancelationTokens to be introduced
    // - Rename DirectoryBuilder to DirBuild, setting up for DirTest and DirBurn
    // - Create interface for Workers, add them to scoped services for dependency inversion


    [ApiController]
    [Route("api/[controller]")]
    public class DirBuild2Controller : ControllerBase
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
                    if (TaskBucket.Bucket.ContainsKey(task))
                    {
                        if (TaskBucket.Bucket[task].Status == TaskStatus.RanToCompletion)
                        {
                            TaskBucket.Bucket.Remove(task);
                        }
                    }
                }

                // Parascript task
                if ((bundle.Parascript == true) && (TaskBucket.Bucket.ContainsKey("Parascript") == true))
                {
                    System.Console.WriteLine("Parascript task already exists");
                }
                if ((bundle.Parascript == true) && (TaskBucket.Bucket.ContainsKey("Parascript") == false))
                {
                    TaskBucket.PsPercent = 0;

                    TaskBucket.Bucket.Add("Parascript", Task.Run(() =>
                    {
                        string psInputPath = Directory.GetCurrentDirectory() + @"\PS-Input";
                        string psWorkingPath = Directory.GetCurrentDirectory() + @"\PS-Working";
                        string psOutputPath = Directory.GetCurrentDirectory() + @"\PS-Output";

                        ParascriptWorker2 ps = new ParascriptWorker2(psInputPath, psWorkingPath, psOutputPath, TaskBucket.PsProgress);

                        try
                        {                       
                            Task.Run(ps.CheckInput).Wait();
                            Task.Run(ps.Cleanup).Wait();
                        }
                        catch (System.Exception e)
                        {
                            System.Console.WriteLine(e.InnerException.Message);
                        }
                    }));

                    // Represents PS task was simply created, nothing more
                    result.Parascript = true;
                }

                // RoyalMail task
                if ((bundle.RoyalMail == true) && (TaskBucket.Bucket.ContainsKey("RoyalMail") == true))
                {
                    System.Console.WriteLine("RoyalMail task already exists");
                }
                if ((bundle.RoyalMail == true) && (TaskBucket.Bucket.ContainsKey("RoyalMail") == false))
                {
                    TaskBucket.RmPercent = 0;

                    TaskBucket.Bucket.Add("RoyalMail", Task.Run(() =>
                    {
                        string rmInputPath = Directory.GetCurrentDirectory() + @"\RM-Input";
                        string rmWorkingPath = Directory.GetCurrentDirectory() + @"\RM-Working";
                        string rmOutputPath = Directory.GetCurrentDirectory() + @"\RM-Output";

                        RoyalWorker rm = new RoyalWorker(rmInputPath, rmWorkingPath, rmOutputPath, TaskBucket.RmProgress);

                        try
                        {
                            if (!rm.CheckInput())
                            {
                                throw new Exception("RM Failed Input files/Utils");
                            }
                            if (!rm.Cleanup())
                            {
                                throw new Exception("RM Failed Cleanup");
                            }
                            if (!rm.FindDate())
                            {
                                throw new Exception("RM Failed FindDate");
                            }
                            if (!rm.UpdateSmiFiles())
                            {
                                throw new Exception("RM Failed UpdateSmiFiles");
                            }
                            if (!rm.ConvertPafData())
                            {
                                throw new Exception("RM Failed ConvertPafData");
                            }
                            if (!rm.Compile().Result)
                            {
                                throw new Exception("RM Failed Compile");
                            }
                            if (!rm.Output().Result)
                            {
                                throw new Exception("RM Failed Output");
                            }
                        }
                        catch (System.Exception e)
                        {
                            System.Console.WriteLine(e.Message);
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
