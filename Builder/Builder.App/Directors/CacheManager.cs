namespace Builder.App;

public class CacheManager
{
    public List<string> smBuilds { get; set; }
    public List<string> psBuilds { get; set; }
    public List<string> rmBuilds { get; set; }

    private readonly ILogger<BuildManager> logger;
    private readonly DatabaseContext context;

    public CacheManager(ILogger<BuildManager> logger, IServiceScopeFactory factory)
    {
        this.logger = logger;
        this.context = factory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
    }

    public async Task RunTask()
    {
        context.Database.EnsureCreated();

        while (true)
        {
            List<UspsBundle> uspsBundles = context.UspsBundles.Where(x => (x.IsReadyForBuild == true)).ToList();
            List<ParaBundle> paraBundles = context.ParaBundles.Where(x => (x.IsReadyForBuild == true)).ToList();
            List<RoyalBundle> royalBundles = context.RoyalBundles.Where(x => (x.IsReadyForBuild == true)).ToList();

            foreach (UspsBundle bundle in uspsBundles)
            {
                string dataYearMonth = bundle.DataYear.ToString() + bundle.DataMonth.ToString();
                smBuilds.Add(dataYearMonth);
            }
            foreach (ParaBundle bundle in paraBundles)
            {
                string dataYearMonth = bundle.DataYear.ToString() + bundle.DataMonth.ToString();
                psBuilds.Add(dataYearMonth);
            }
            foreach (RoyalBundle bundle in royalBundles)
            {
                string dataYearMonth = bundle.DataYear.ToString() + bundle.DataMonth.ToString();
                rmBuilds.Add(dataYearMonth);
            }

            await Task.Delay(TimeSpan.FromMinutes(5));
        }
    }
}
