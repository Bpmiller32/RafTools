using System.Text.Json;
using Server.Builders;
using Server.Common;
using Server.Crawlers;

namespace Server.Service;

public class StatusReporter
{
    private readonly DatabaseContext context;

    private readonly SmartMatchCrawler smartMatchCrawler;
    private readonly SmartMatchBuilder smartMatchBuilder;
    private readonly ParascriptCrawler parascriptCrawler;
    private readonly ParascriptBuilder parascriptBuilder;
    private readonly RoyalMailCrawler royalMailCrawler;
    private readonly RoyalMailBuilder royalMailBuilder;


    public StatusReporter(DatabaseContext context, SmartMatchCrawler smartMatchCrawler, SmartMatchBuilder smartMatchBuilder, ParascriptCrawler parascriptCrawler, ParascriptBuilder parascriptBuilder, RoyalMailCrawler royalMailCrawler, RoyalMailBuilder royalMailBuilder)
    {
        this.context = context;

        this.smartMatchCrawler = smartMatchCrawler;
        this.smartMatchBuilder = smartMatchBuilder;
        this.parascriptCrawler = parascriptCrawler;
        this.parascriptBuilder = parascriptBuilder;
        this.royalMailCrawler = royalMailCrawler;
        this.royalMailBuilder = royalMailBuilder;
    }

    public string Report()
    {
        var jsonObject = new
        {
            SmartMatch = new
            {
                Crawler = new
                {
                    smartMatchCrawler.Status,
                    smartMatchCrawler.Progress,
                    smartMatchCrawler.Message
                },
                Builder = new
                {
                    smartMatchBuilder.Status,
                    smartMatchBuilder.Progress,
                    smartMatchBuilder.Message
                },
            },
            Parascript = new
            {
                Crawler = new
                {
                    parascriptCrawler.Status,
                    parascriptCrawler.Progress,
                    parascriptCrawler.Message
                },
                Builder = new
                {
                    parascriptBuilder.Status,
                    parascriptBuilder.Progress,
                    parascriptBuilder.Message
                }
            },
            RoyalMail = new
            {
                Crawler = new
                {
                    royalMailCrawler.Status,
                    royalMailCrawler.Progress,
                    royalMailCrawler.Message
                },
                Builder = new
                {
                    royalMailBuilder.Status,
                    royalMailBuilder.Progress,
                    royalMailBuilder.Message
                },
                IsReadyForBuild = string.Join(", ", context.RoyalBundles.Where(x => x.IsReadyForBuild == true).Select(x => x.DataYearMonth).ToList()),
                IsBuildComplete = string.Join(", ", context.RoyalBundles.Where(x => x.IsBuildComplete == true).Select(x => x.DataYearMonth).ToList())
            }
        };

        return JsonSerializer.Serialize(jsonObject);
    }
}
