using System;
using System.Linq;
using DbTesting.Data;
using DbTesting.Core;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DbTesting.UI
{
    class Program
    {
        static private SamuraiContext context = new SamuraiContext();

        static void AddSamurai()
        {
            Samurai samurai = new Samurai {Name = "James"};
            context.Samurais.Add(samurai);
            context.SaveChanges();
        }

        static void GetSamurais(string text)
        {
            List<Samurai> samurais = context.Samurais.TagWith("Hello froom GetSamurais method").ToList();
            System.Console.WriteLine($"{text}: Samurai count is {samurais.Count}");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        
            context.Database.EnsureCreated();
            GetSamurais("Before Add");
            AddSamurai();
            GetSamurais("After Add");
        }
    }
}
