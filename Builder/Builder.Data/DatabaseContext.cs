using Microsoft.EntityFrameworkCore;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

    public DbSet<UspsBundle> UspsBundles { get; set; }
    public DbSet<ParaBundle> ParaBundles { get; set; }
    public DbSet<RoyalBundle> RoyalBundles { get; set; }
}

