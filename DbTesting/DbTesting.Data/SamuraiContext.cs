using Microsoft.EntityFrameworkCore;
using DbTesting.Core;
using System;
using Microsoft.Extensions.Logging;

namespace DbTesting.Data
{
    public class SamuraiContext:DbContext
    {
        public DbSet<Samurai> Samurais { get; set; }
        public DbSet<Quote> Quotes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(@"Filename=.\BillyDb.db")
                .LogTo(Console.WriteLine, new[] {DbLoggerCategory.Database.Command.Name}, LogLevel.Information)
                .EnableSensitiveDataLogging();
        }
    }
}
