using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EFCore.Extensions.SqlServer.UnitTests.Data
{
    public class DataContext : DbContext
    {
        public DbSet<Person> Persons { get; set; }

        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }
    }

    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            return new DataContext(new DbContextOptionsBuilder<DataContext>()
                .UseSqlServer(AppSettings.Load().ConnectionString)
                .Options);
        }
    }
}
