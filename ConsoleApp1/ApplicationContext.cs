using EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace ConsoleApp1
{
    public class Person
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string KindsList { get; set; }
        public string Values { get; set; }
    }

    public class Order
    {
        public Guid Id { get; set; }
        public Guid PersonId { get; set; }
        public Person Person { get; set; }
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<Person> Persons { get; set; }
        public DbSet<Order> Orders { get; set; }

        public ApplicationContext()
        {
        }

        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=testdbapp3;Trusted_Connection=True;");
        //}

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    var lf = new LoggerFactory();
        //    lf.AddProvider(new SimpleConsoleLoggerProvider());
        //    optionsBuilder.UseLoggerFactory(lf);

        //    optionsBuilder.UseExtensions(extensions =>
        //    {
        //        extensions.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=testdbapp2;Trusted_Connection=True;");
        //        //extensions.UseInMemoryDatabase("inmemory");
        //    });
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Person>().Property(p => p.KindsList).HasColumnName("Kinds");
        }
    }
}
