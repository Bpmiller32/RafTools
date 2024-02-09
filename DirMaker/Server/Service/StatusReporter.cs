using System.Text.Json;
using Server.Builders;
using Server.Common;
using Server.Crawlers;
using Server.Tester;

namespace Server.Service;

public class StatusReporter
{
    private readonly DatabaseContext context;

    private readonly Dictionary<string, BaseModule> modules = [];
    private readonly Dictionary<string, string> dbBuilds = [];

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
        dbBuilds.Add("smNReadytoBuild", string.Join("|", context.UspsBundles.Where(x => x.IsReadyForBuild == true && x.Cycle == "Cycle-N").Select(x => x.DataYearMonth).ToList()));
        dbBuilds.Add("smNBuildComplete", string.Join("|", context.UspsBundles.Where(x => x.IsBuildComplete == true && x.Cycle == "Cycle-N").Select(x => x.DataYearMonth).ToList()));
        dbBuilds.Add("smOReadytoBuild", string.Join("|", context.UspsBundles.Where(x => x.IsReadyForBuild == true && x.Cycle == "Cycle-O").Select(x => x.DataYearMonth).ToList()));
        dbBuilds.Add("smOBuildComplete", string.Join("|", context.UspsBundles.Where(x => x.IsBuildComplete == true && x.Cycle == "Cycle-O").Select(x => x.DataYearMonth).ToList()));

        dbBuilds.Add("psReadytoBuild", string.Join("|", context.ParaBundles.Where(x => x.IsReadyForBuild == true).Select(x => x.DataYearMonth).ToList()));
        dbBuilds.Add("psBuildComplete", string.Join("|", context.ParaBundles.Where(x => x.IsBuildComplete == true).Select(x => x.DataYearMonth).ToList()));

        dbBuilds.Add("rmReadytoBuild", string.Join("|", context.RoyalBundles.Where(x => x.IsReadyForBuild == true).Select(x => x.DataYearMonth).ToList()));
        dbBuilds.Add("rmBuildComplete", string.Join("|", context.RoyalBundles.Where(x => x.IsBuildComplete == true).Select(x => x.DataYearMonth).ToList()));
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
                dbBuilds["smNReadytoBuild"] = string.Join("|", context.UspsBundles.Where(x => x.IsReadyForBuild == true && x.Cycle == "Cycle-N").Select(x => x.DataYearMonth).ToList());
                dbBuilds["smNBuildComplete"] = string.Join("|", context.UspsBundles.Where(x => x.IsBuildComplete == true && x.Cycle == "Cycle-N").Select(x => x.DataYearMonth).ToList());

                dbBuilds["smOReadytoBuild"] = string.Join("|", context.UspsBundles.Where(x => x.IsReadyForBuild == true && x.Cycle == "Cycle-O").Select(x => x.DataYearMonth).ToList());
                dbBuilds["smOBuildComplete"] = string.Join("|", context.UspsBundles.Where(x => x.IsBuildComplete == true && x.Cycle == "Cycle-O").Select(x => x.DataYearMonth).ToList());
            }
            else if (module.Key.Contains("parascript"))
            {
                dbBuilds["psReadytoBuild"] = string.Join("|", context.ParaBundles.Where(x => x.IsReadyForBuild == true).Select(x => x.DataYearMonth).ToList());
                dbBuilds["psBuildComplete"] = string.Join("|", context.ParaBundles.Where(x => x.IsBuildComplete == true).Select(x => x.DataYearMonth).ToList());
            }
            else if (module.Key.Contains("royalMail"))
            {
                dbBuilds["psReadytoBuild"] = string.Join("|", context.ParaBundles.Where(x => x.IsReadyForBuild == true).Select(x => x.DataYearMonth).ToList());
                dbBuilds["psBuildComplete"] = string.Join("|", context.ParaBundles.Where(x => x.IsBuildComplete == true).Select(x => x.DataYearMonth).ToList());
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
                    modules["smartMatchCrawler"].Message
                },
                Builder = new
                {
                    modules["smartMatchBuilder"].Status,
                    modules["smartMatchBuilder"].Progress,
                    modules["smartMatchBuilder"].Message,
                    modules["smartMatchBuilder"].CurrentTask
                },

                IsReadyForBuildN = dbBuilds["smNReadytoBuild"],
                IsBuildCompleteN = dbBuilds["smNBuildComplete"],
                IsReadyForBuildO = dbBuilds["smOReadytoBuild"],
                IsBuildCompleteO = dbBuilds["smOBuildComplete"]
            },
            Parascript = new
            {
                Crawler = new
                {
                    modules["parascriptCrawler"].Status,
                    modules["parascriptCrawler"].Progress,
                    modules["parascriptCrawler"].Message
                },
                Builder = new
                {
                    modules["parascriptBuilder"].Status,
                    modules["parascriptBuilder"].Progress,
                    modules["parascriptBuilder"].Message
                },

                IsReadyForBuild = dbBuilds["psReadytoBuild"],
                IsBuildComplete = dbBuilds["psBuildComplete"]
            },
            RoyalMail = new
            {
                Crawler = new
                {
                    modules["royalMailCrawler"].Status,
                    modules["royalMailCrawler"].Progress,
                    modules["royalMailCrawler"].Message
                },
                Builder = new
                {
                    modules["royalMailBuilder"].Status,
                    modules["royalMailBuilder"].Progress,
                    modules["royalMailBuilder"].Message,
                },

                IsReadyForBuild = dbBuilds["rmReadytoBuild"],
                IsBuildComplete = dbBuilds["rmBuildComplete"]
            },
            DirTester = new
            {
                modules["dirTester"].Status,
                modules["dirTester"].Progress,
                modules["dirTester"].Message,
                modules["dirTester"].CurrentTask
            }
        };

        return JsonSerializer.Serialize(jsonObject);
    }
}
