using System.Collections.Generic;
using System.IO;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OverwatchApi.Data;

namespace OverwatchApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DirectoryBuilderController : ControllerBase
    {
        private static object lockObj = new object();

        [HttpPost]
        public DirectoryStatus Parascript(DirectoryStatus bundle)
        {
            lock (lockObj)
            {
                DirectoryStatus result = new DirectoryStatus()
                {
                    Status = "",
                    SmartMatch = false,
                    Parascript = false,
                    RoyalMail = false
                };

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

                        ParascriptWorker ps = new ParascriptWorker(psInputPath, psWorkingPath, psOutputPath, TaskBucket.PsProgress);

                        try
                        {
                            if (!ps.CheckInput())
                            {
                                throw new Exception("PS Failed Input files/Utils");
                            }
                            if (!ps.Cleanup())
                            {
                                throw new Exception("PS Failed Cleanup");
                            }
                            if (!ps.FindDate())
                            {
                                throw new Exception("PS Failed FindDate");
                            }
                            if (!ps.Extract().Result)
                            {
                                throw new Exception("PS Failed Extract");
                            }
                            if (!ps.Archive().Result)
                            {
                                throw new Exception("PS Failed Archive");
                            }
                        }
                        catch (System.Exception e)
                        {
                            System.Console.WriteLine(e.Message);
                        }
                    }));

                    // Represents PS task was simply created
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

                    // Reprensets RM task was simply created
                    result.RoyalMail = true;
                }

                return result;
            }
        }
    }
}
