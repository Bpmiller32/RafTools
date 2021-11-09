using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crawler.Data
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        public DbSet<UspsBundle> UspsBundles { get; set; }
        public DbSet<UspsFile> UspsFiles { get; set; }
        public DbSet<UspsFile> TempFiles { get; set; }

        // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        // {
        //     optionsBuilder.UseSqlite(@"Filename=.\DirectoryCollection.db")
        //         .LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information)
        //         .EnableSensitiveDataLogging();

        //     base.OnConfiguring(optionsBuilder);
        // }
    }
}
