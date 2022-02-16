namespace Builder.App;

public class CacheManager
{
    public List<string> SmBuilds { get; set; } = new List<string>();
    public List<string> PsBuilds { get; set; } = new List<string>();
    public List<string> RmBuilds { get; set; } = new List<string>();

    private readonly ILogger<CacheManager> logger;
    private readonly DatabaseContext context;

    public CacheManager(ILogger<CacheManager> logger, IServiceScopeFactory factory)
    {
        this.logger = logger;
        this.context = factory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
    }

    public async Task RunTask()
    {
        context.Database.EnsureCreated();

        while (true)
        {
            List<UspsBundle> uspsBundles = context.UspsBundles.Where(x => (x.IsBuildComplete == true)).ToList();
            List<ParaBundle> paraBundles = context.ParaBundles.Where(x => (x.IsBuildComplete == true)).ToList();
            List<RoyalBundle> royalBundles = context.RoyalBundles.Where(x => (x.IsBuildComplete == true)).ToList();

            foreach (UspsBundle bundle in uspsBundles)
            {
                string dataYearMonth = bundle.DataYear.ToString() + bundle.DataMonth.ToString();
                this.SmBuilds.Add(dataYearMonth);
            }
            foreach (ParaBundle bundle in paraBundles)
            {
                string dataYearMonth = bundle.DataYear.ToString() + bundle.DataMonth.ToString();
                this.PsBuilds.Add(dataYearMonth);
            }
            foreach (RoyalBundle bundle in royalBundles)
            {
                string dataYearMonth = bundle.DataYear.ToString() + bundle.DataMonth.ToString();
                this.RmBuilds.Add(dataYearMonth);
            }

            await Task.Delay(TimeSpan.FromMinutes(5));
        }
    }
}
