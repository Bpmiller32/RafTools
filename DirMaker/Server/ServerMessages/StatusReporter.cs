using System.Text.Json;
using Server.Builders;
using DataObjects;
using Server.Crawlers;
using Server.Tester;
using Microsoft.EntityFrameworkCore;

namespace Server.ServerMessages;

public class StatusReporter
{
    private readonly DatabaseContext context;
    private readonly SmartMatchCrawler smartMatchCrawler;
    private readonly SmartMatchBuilder smartMatchBuilder;
    private readonly ParascriptCrawler parascriptCrawler;
    private readonly ParascriptBuilder parascriptBuilder;
    private readonly RoyalMailCrawler royalMailCrawler;
    private readonly RoyalMailBuilder royalMailBuilder;
    private readonly DirTester dirTester;

    private readonly Dictionary<string, BaseModule> modules = [];

    private Dictionary<string, ModuleReporter> jsonObject = new() { { "SmartMatch", new() }, { "Parascript", new() }, { "RoyalMail", new() } };

    public StatusReporter(DatabaseContext context, SmartMatchCrawler smartMatchCrawler, SmartMatchBuilder smartMatchBuilder, ParascriptCrawler parascriptCrawler, ParascriptBuilder parascriptBuilder, RoyalMailCrawler royalMailCrawler, RoyalMailBuilder royalMailBuilder, DirTester dirTester)
    {
        this.context = context;
        this.smartMatchCrawler = smartMatchCrawler;
        this.smartMatchBuilder = smartMatchBuilder;
        this.parascriptCrawler = parascriptCrawler;
        this.parascriptBuilder = parascriptBuilder;
        this.royalMailCrawler = royalMailCrawler;
        this.royalMailBuilder = royalMailBuilder;
        this.dirTester = dirTester;

        // Add all modules to a dictionary for looping later
        modules.Add("smartMatchCrawler", smartMatchCrawler);
        modules.Add("smartMatchBuilder", smartMatchBuilder);

        modules.Add("parascriptCrawler", parascriptCrawler);
        modules.Add("parascriptBuilder", parascriptBuilder);

        modules.Add("royalMailCrawler", royalMailCrawler);
        modules.Add("royalMailBuilder", royalMailBuilder);

        modules.Add("dirTester", dirTester);


        // Initial population of db values
        UpdateAndStringifyDbValues("SmartMatch");
        UpdateAndStringifyDbValues("Parascript");
        UpdateAndStringifyDbValues("RoyalMail");
    }

    private void UpdateAndStringifyDbValues(string directoryType)
    {
        // For Crawler
        jsonObject[directoryType].Crawler.ReadyToBuild.DataYearMonth = "";
        jsonObject[directoryType].Crawler.ReadyToBuild.FileCount = "";
        jsonObject[directoryType].Crawler.ReadyToBuild.DownloadDate = "";
        jsonObject[directoryType].Crawler.ReadyToBuild.DownloadTime = "";
        if (directoryType == "SmartMatch")
        {
            context.UspsBundles.Where(x => x.IsReadyForBuild == true && x.Cycle == "Cycle-O").ForEachAsync((bundle) =>
             {
                 jsonObject[directoryType].Crawler.ReadyToBuild.DataYearMonth += $"{bundle.DataYearMonth}|";
                 jsonObject[directoryType].Crawler.ReadyToBuild.FileCount += $"{bundle.FileCount}|";
                 jsonObject[directoryType].Crawler.ReadyToBuild.DownloadDate += $"{bundle.DownloadDate}|";
                 jsonObject[directoryType].Crawler.ReadyToBuild.DownloadTime += $"{bundle.DownloadTime}|";
             });
        }
        else
        {
            context.UspsBundles.Where(x => x.IsReadyForBuild == true).ForEachAsync((bundle) =>
            {
                jsonObject[directoryType].Crawler.ReadyToBuild.DataYearMonth += $"{bundle.DataYearMonth}|";
                jsonObject[directoryType].Crawler.ReadyToBuild.FileCount += $"{bundle.FileCount}|";
                jsonObject[directoryType].Crawler.ReadyToBuild.DownloadDate += $"{bundle.DownloadDate}|";
                jsonObject[directoryType].Crawler.ReadyToBuild.DownloadTime += $"{bundle.DownloadTime}|";
            });
        }
        if (jsonObject[directoryType].Crawler.ReadyToBuild.DataYearMonth.Length > 0)
        {
            jsonObject[directoryType].Crawler.ReadyToBuild.DataYearMonth = jsonObject[directoryType].Crawler.ReadyToBuild.DataYearMonth.Remove(jsonObject[directoryType].Crawler.ReadyToBuild.DataYearMonth.Length - 1);
            jsonObject[directoryType].Crawler.ReadyToBuild.FileCount = jsonObject[directoryType].Crawler.ReadyToBuild.FileCount.Remove(jsonObject[directoryType].Crawler.ReadyToBuild.FileCount.Length - 1);
            jsonObject[directoryType].Crawler.ReadyToBuild.DownloadDate = jsonObject[directoryType].Crawler.ReadyToBuild.DownloadDate.Remove(jsonObject[directoryType].Crawler.ReadyToBuild.DownloadDate.Length - 1);
            jsonObject[directoryType].Crawler.ReadyToBuild.DownloadTime = jsonObject[directoryType].Crawler.ReadyToBuild.DownloadTime.Remove(jsonObject[directoryType].Crawler.ReadyToBuild.DownloadTime.Length - 1);
        }

        // For Builder
        jsonObject[directoryType].Builder.BuildComplete.DataYearMonth = "";
        if (directoryType == "SmartMatch")
        {
            context.UspsBundles.Where(x => x.IsBuildComplete == true && x.Cycle == "Cycle-O").ForEachAsync((bundle) =>
            {
                jsonObject[directoryType].Builder.BuildComplete.DataYearMonth += $"{bundle.DataYearMonth}|";
            });
        }
        else
        {
            context.UspsBundles.Where(x => x.IsBuildComplete == true).ForEachAsync((bundle) =>
            {
                jsonObject[directoryType].Builder.BuildComplete.DataYearMonth += $"{bundle.DataYearMonth}|";
            });
        }
        if (jsonObject[directoryType].Builder.BuildComplete.DataYearMonth.Length > 0)
        {
            jsonObject[directoryType].Builder.BuildComplete.DataYearMonth = jsonObject[directoryType].Builder.BuildComplete.DataYearMonth.Remove(jsonObject[directoryType].Builder.BuildComplete.DataYearMonth.Length - 1);
        }
    }

    public string UpdateReport()
    {
        foreach (var module in modules)
        {
            // Update db's only if nessasary, otherwise use stored values
            if (!module.Value.SendDbUpdate)
            {
                continue;
            }

            if (module.Key.Contains("smartMatch"))
            {
                UpdateAndStringifyDbValues("SmartMatch");
            }
            else if (module.Key.Contains("parascript"))
            {
                UpdateAndStringifyDbValues("Parascript");
            }
            else if (module.Key.Contains("royalMail"))
            {
                UpdateAndStringifyDbValues("RoyalMail");
            }

            // Turn off the flag
            module.Value.SendDbUpdate = false;
        }

        jsonObject["SmartMatch"].Crawler.Status = smartMatchCrawler.Status;
        jsonObject["SmartMatch"].Crawler.Progress = smartMatchCrawler.Progress;
        jsonObject["SmartMatch"].Crawler.Message = smartMatchCrawler.Message;

        jsonObject["Parascript"].Crawler.Status = parascriptCrawler.Status;
        jsonObject["Parascript"].Crawler.Progress = parascriptCrawler.Progress;
        jsonObject["Parascript"].Crawler.Message = parascriptCrawler.Message;

        jsonObject["RoyalMail"].Crawler.Status = royalMailCrawler.Status;
        jsonObject["RoyalMail"].Crawler.Progress = royalMailCrawler.Progress;
        jsonObject["RoyalMail"].Crawler.Message = royalMailCrawler.Message;

        jsonObject["SmartMatch"].Builder.Status = smartMatchBuilder.Status;
        jsonObject["SmartMatch"].Builder.Progress = smartMatchBuilder.Progress;
        jsonObject["SmartMatch"].Builder.Message = smartMatchBuilder.Message;

        jsonObject["Parascript"].Builder.Status = parascriptBuilder.Status;
        jsonObject["Parascript"].Builder.Progress = parascriptBuilder.Progress;
        jsonObject["Parascript"].Builder.Message = parascriptBuilder.Message;

        jsonObject["RoyalMail"].Builder.Status = royalMailBuilder.Status;
        jsonObject["RoyalMail"].Builder.Progress = royalMailBuilder.Progress;
        jsonObject["RoyalMail"].Builder.Message = royalMailBuilder.Message;

        return JsonSerializer.Serialize(jsonObject);
    }
}
