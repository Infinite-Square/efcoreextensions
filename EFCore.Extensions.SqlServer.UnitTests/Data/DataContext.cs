using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EFCore.Extensions.SqlServer.UnitTests.Data
{
    public class DataContext : DbContext
    {
        public DbSet<Person> Persons { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupPerson> GroupPersons { get; set; }

        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GroupPerson>().HasOne(gp => gp.Person).WithMany(p => p.Groups).HasForeignKey(gp => gp.PersonId);
            modelBuilder.Entity<GroupPerson>().HasOne(gp => gp.Group).WithMany(p => p.Persons).HasForeignKey(gp => gp.GroupId);
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Person>().HasQueryFilter(p => p.TenantId == Guid.Empty);
            modelBuilder.Entity<GroupPerson>().HasQueryFilter(p => p.TenantId == Guid.Empty);
            modelBuilder.Entity<Group>().HasQueryFilter(p => p.TenantId == Guid.Empty);
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
