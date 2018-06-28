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
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<Person> Persons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var lf = new LoggerFactory();
            lf.AddProvider(new LoggerProvider());
            optionsBuilder.UseLoggerFactory(lf);

            optionsBuilder.UseExtensions(extensions =>
            {
                extensions.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=testdbapp2;Trusted_Connection=True;");
                //extensions.UseInMemoryDatabase("inmemory");
            });
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<JsonResult<string>>();
            modelBuilder.Entity<JsonResult<int>>();
            modelBuilder.Entity<JsonResult<bool>>();
            modelBuilder.Entity<Person>().Property(p => p.KindsList).HasColumnName("Kinds");
        }

        private class LoggerProvider : ILoggerProvider
        {
            public ILogger CreateLogger(string categoryName)
            {
                return new Logger(categoryName);
            }

            public void Dispose()
            {
            }

            private class Logger : ILogger
            {
                private readonly string _categoryName;

                public Logger(string categoryName)
                {
                    _categoryName = categoryName;
                }

                public IDisposable BeginScope<TState>(TState state)
                {
                    return null;
                }

                public bool IsEnabled(LogLevel logLevel)
                {
                    return true;
                }

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                    Console.WriteLine(formatter(state, exception));
                }
            }
        }
    }
}
