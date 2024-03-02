using System.Text.Json;
using Server.Builders;
using DataObjects;
using Server.Crawlers;
using Server.Tester;

namespace Server.ServerMessages;

public class StatusReporter
{
    private readonly DatabaseContext context;

    private readonly Dictionary<string, BaseModule> modules = [];
    private readonly Dictionary<string, IQueryable<UspsBundle>> smBuilds = [];
    private readonly Dictionary<string, IQueryable<ParaBundle>> psBuilds = [];
    private readonly Dictionary<string, IQueryable<RoyalBundle>> rmBuilds = [];

    public StatusReporter(DatabaseContext context, SmartMatchCrawler smartMatchCrawler, SmartMatchBuilder smartMatchBuilder, ParascriptCrawler parascriptCrawler, ParascriptBuilder parascriptBuilder, RoyalMailCrawler royalMailCrawler, RoyalMailBuilder royalMailBuilder, DirTester dirTester)
    {
        this.context = context;

        // Add all modules to a dictionary for looping later
        modules.Add("smartMatchCrawler", smartMatchCrawler);
        modules.Add("smartMatchBuilder", smartMatchBuilder);

        modules.Add("parascriptCrawler", parascriptCrawler);
        modules.Add("parascriptBuilder", parascriptBuilder);

        modules.Add("royalMailCrawler", royalMailCrawler);
        modules.Add("royalMailBuilder", royalMailBuilder);

        modules.Add("dirTester", dirTester);


        // Initial population of db values
        smBuilds.Add("readyToBuild", context.UspsBundles.Where(x => x.IsReadyForBuild == true && x.Cycle == "Cycle-O"));
        smBuilds.Add("buildComplete", context.UspsBundles.Where(x => x.IsBuildComplete == true && x.Cycle == "Cycle-O"));

        psBuilds.Add("readyToBuild", context.ParaBundles.Where(x => x.IsReadyForBuild == true));
        psBuilds.Add("buildComplete", context.ParaBundles.Where(x => x.IsBuildComplete == true));

        rmBuilds.Add("readyToBuild", context.RoyalBundles.Where(x => x.IsReadyForBuild == true));
        rmBuilds.Add("buildComplete", context.RoyalBundles.Where(x => x.IsBuildComplete == true));
    }

    public string UpdateReport()
    {
        // Update db's only if nessasary, otherwise use stored values
        foreach (var module in modules)
        {
            if (!module.Value.SendDbUpdate)
            {
                continue;
            }

            if (module.Key.Contains("smartMatch"))
            {
                smBuilds["readytoBuild"] = context.UspsBundles.Where(x => x.IsReadyForBuild == true && x.Cycle == "Cycle-O");
                smBuilds["buildComplete"] = context.UspsBundles.Where(x => x.IsBuildComplete == true && x.Cycle == "Cycle-O");
            }
            else if (module.Key.Contains("parascript"))
            {
                psBuilds["readytoBuild"] = context.ParaBundles.Where(x => x.IsReadyForBuild == true);
                psBuilds["buildComplete"] = context.ParaBundles.Where(x => x.IsBuildComplete == true);
            }
            else if (module.Key.Contains("royalMail"))
            {
                rmBuilds["readytoBuild"] = context.RoyalBundles.Where(x => x.IsReadyForBuild == true);
                rmBuilds["buildComplete"] = context.RoyalBundles.Where(x => x.IsBuildComplete == true);
            }

            // Turn off the flag
            module.Value.SendDbUpdate = false;
        }

        // Create the JSON object for sending
        var jsonObject = new
        {
            SmartMatch = new
            {
                Crawler = new
                {
                    modules["smartMatchCrawler"].Status,
                    modules["smartMatchCrawler"].Progress,
                    modules["smartMatchCrawler"].Message,
                    ReadyToBuild = new
                    {
                        DataYearMonth = string.Join("|", smBuilds["readyToBuild"].Select(x => x.DataYearMonth).ToList()),
                        FileCount = string.Join("|", smBuilds["readyToBuild"].Select(x => x.FileCount).ToList()),
                        DownloadDate = string.Join("|", smBuilds["readyToBuild"].Select(x => x.DownloadDate).ToList()),
                        DownloadTime = string.Join("|", smBuilds["readyToBuild"].Select(x => x.DownloadTime).ToList()),
                    }
                },
                Builder = new
                {
                    modules["smartMatchBuilder"].Status,
                    modules["smartMatchBuilder"].Progress,
                    modules["smartMatchBuilder"].Message,
                },
            },
            Parascript = new
            {
                Crawler = new
                {
                    modules["parascriptCrawler"].Status,
                    modules["parascriptCrawler"].Progress,
                    modules["parascriptCrawler"].Message,
                    ReadyToBuild = new
                    {
                        DataYearMonth = string.Join("|", psBuilds["readyToBuild"].Select(x => x.DataYearMonth).ToList()),
                        FileCount = string.Join("|", psBuilds["readyToBuild"].Select(x => x.FileCount).ToList()),
                        DownloadDate = string.Join("|", psBuilds["readyToBuild"].Select(x => x.DownloadDate).ToList()),
                        DownloadTime = string.Join("|", psBuilds["readyToBuild"].Select(x => x.DownloadTime).ToList()),
                    }
                },
                Builder = new
                {
                    modules["parascriptBuilder"].Status,
                    modules["parascriptBuilder"].Progress,
                    modules["parascriptBuilder"].Message
                },
            },
            RoyalMail = new
            {
                Crawler = new
                {
                    modules["royalMailCrawler"].Status,
                    modules["royalMailCrawler"].Progress,
                    modules["royalMailCrawler"].Message,
                    ReadyToBuild = new
                    {
                        DataYearMonth = string.Join("|", rmBuilds["readyToBuild"].Select(x => x.DataYearMonth).ToList()),
                        FileCount = string.Join("|", rmBuilds["readyToBuild"].Select(x => x.FileCount).ToList()),
                        DownloadDate = string.Join("|", rmBuilds["readyToBuild"].Select(x => x.DownloadDate).ToList()),
                        DownloadTime = string.Join("|", rmBuilds["readyToBuild"].Select(x => x.DownloadTime).ToList()),
                    }
                },
                Builder = new
                {
                    modules["royalMailBuilder"].Status,
                    modules["royalMailBuilder"].Progress,
                    modules["royalMailBuilder"].Message,
                },
            },
            DirTester = new
            {
                modules["dirTester"].Status,
                modules["dirTester"].Progress,
                modules["dirTester"].Message,
            }
        };

        return JsonSerializer.Serialize(jsonObject);
    }
}
