using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OverwatchApi.Data;

namespace OverwatchApi.Controllers
{
    // - Fix errror logging for if/else on PS, it returns the status if it is empty. Also just rework this
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


                if (TaskBucket.Bucket.ContainsKey("SmartMatch"))
                {
                    if (TaskBucket.Bucket["SmartMatch"].Status == TaskStatus.RanToCompletion)
                    {
                        TaskBucket.Bucket.Remove("SmartMatch");
                        TaskBucket.SmPercent = 0;
                    }
                }
                if (TaskBucket.Bucket.ContainsKey("Parascript"))
                {
                    if (TaskBucket.Bucket["Parascript"].Status == TaskStatus.RanToCompletion)
                    {
                        TaskBucket.Bucket.Remove("Parascript");
                        TaskBucket.PsPercent = 0;
                    }
                }
                if (TaskBucket.Bucket.ContainsKey("RoyalMail"))
                {
                    if (TaskBucket.Bucket["RoyalMail"].Status == TaskStatus.RanToCompletion)
                    {
                        TaskBucket.Bucket.Remove("RoyalMail");
                        TaskBucket.RmPercent = 0;
                    }
                }

                // Parascript task
                if ((bundle.Parascript == true) && (TaskBucket.Bucket.ContainsKey("Parascript") == false))
                {
                    TaskBucket.Bucket.Add("Parascript", Task.Run(() =>
                    {
                        string psInputPath = Directory.GetCurrentDirectory() + @"\PS-Input";
                        string psWorkingPath = Directory.GetCurrentDirectory() + @"\PS-Working";
                        string psOutputPath = Directory.GetCurrentDirectory() + @"\PS-Output";

                        ParascriptWorker ps = new ParascriptWorker(psInputPath, psWorkingPath, psOutputPath, TaskBucket.PsProgress);
                        
                        if (!ps.Cleanup())
                        {
                            result.Status = "PS Failed Cleanup";
                        }
                        if (!ps.FindDate())
                        {
                            result.Status = "PS Failed FindDate";
                        }
                        if (!ps.Extract().Result)
                        {
                            result.Status = "PS Failed Extract";
                        }
                        if (!ps.Archive().Result)
                        {
                            result.Status = "PS Failed Archive";
                        }
                    }));

                    // Represents PS task was simply created
                    result.Parascript = true;
                }
                // if ((bundle.Parascript == true) && (TaskBucket.Bucket.ContainsKey("Parascript") == true))
                // {
                //     result.Status = "Parascript task already exists";
                // }


                // RoyalMail task
                if ((bundle.RoyalMail == true) && (TaskBucket.Bucket.ContainsKey("RoyalMail") == false))
                {
                    TaskBucket.Bucket.Add("RoyalMail", Task.Run(() =>
                    {
                        string rmInputPath = Directory.GetCurrentDirectory() + @"\RM-Input";
                        string rmWorkingPath = Directory.GetCurrentDirectory() + @"\RM-Working";
                        string rmOutputPath = Directory.GetCurrentDirectory() + @"\RM-Output";

                        RoyalWorker rm = new RoyalWorker(rmInputPath, rmWorkingPath, rmOutputPath, TaskBucket.RmProgress);

                        if (!rm.Cleanup())
                        {
                            result.Status = "RM Failed Cleanup";
                        }
                        if (!rm.FindDate())
                        {
                            result.Status = "RM Failed FindDate";
                        }
                        if (!rm.UpdateSmiFiles())
                        {
                            result.Status = "RM Failed UpdateSmiFiles";
                        }
                        if (!rm.ConvertPafData())
                        {
                            result.Status = "RM Failed ConvertPafData";
                        }
                        if (!rm.Compile().Result)
                        {
                            result.Status = "RM Failed Compile";
                        }
                        if (!rm.Output().Result)
                        {
                            result.Status = "RM Failed Output";
                        }
                    }));

                    // Reprensets RM task was simply created
                    result.RoyalMail = true;
                }
                // if ((bundle.RoyalMail == true) && (TaskBucket.Bucket.ContainsKey("RoyalMail") == true))
                // {
                //     result.Status = "RoyalMail task already exists";
                // }

                return result;
            }
        }
    }
}
