using EFCore.Extensions.ChangeTracker;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace EFCore.Extensions.UnitTests
{
    public static class ChangeTrackerTransactionTests
    {
        [Fact]
        public static void Test1()
        {
            using var dbContext = GetTestDbContext();
            Assert.True(dbContext.ChangeTracker.AutoDetectChangesEnabled);
            Assert.False(dbContext.ChangeTracker.HasChanges());
            var watcher = new ChangeTrackerWatcher(dbContext);
            watcher.Start();
            Assert.False(dbContext.ChangeTracker.HasChanges());
            watcher.End();
            Assert.False(dbContext.ChangeTracker.HasChanges());
            watcher.Revert();
            Assert.False(dbContext.ChangeTracker.HasChanges());
        }

        [Fact]
        public static void Test2()
        {
            using var dbContext = GetTestDbContext();
            var watcher = new ChangeTrackerWatcher(dbContext);
            watcher.Start();
            Assert.False(dbContext.ChangeTracker.HasChanges());
            dbContext.Add(new OneEntity());
            Assert.True(dbContext.ChangeTracker.HasChanges());
            watcher.End();
            Assert.True(dbContext.ChangeTracker.HasChanges());
            watcher.Revert();
            Assert.False(dbContext.ChangeTracker.HasChanges());
        }

        [Fact]
        public static void Test3()
        {
            using var dbContext = GetTestDbContext();
            dbContext.Add(new OneEntity());
            var watcher = new ChangeTrackerWatcher(dbContext);
            watcher.Start();
            dbContext.Add(new SecondEntity());
            watcher.End();
            var entries = dbContext.ChangeTracker.Entries().ToList();
            Assert.Equal(2, entries.Count);
            watcher.Revert();

            entries = dbContext.ChangeTracker.Entries().ToList();
            var e = Assert.Single(entries);
            Assert.Equal(EntityState.Added, e.State);
        }

        [Fact]
        public static void Test4()
        {
            using var dbContext = GetTestDbContext();
            var e = new OneEntity
            {
                ValueInt = 1,
                ValueString = "1"
            };
            dbContext.Add(e);
            Assert.Equal(1, dbContext.SaveChanges());
            var watcher = new ChangeTrackerWatcher(dbContext);
            watcher.Start();
            e.ValueInt = 2;
            watcher.End();
            watcher.Revert();
            Assert.Equal(0, dbContext.SaveChanges());
        }

        [Fact]
        public static void Test5()
        {
            using var dbContext = GetTestDbContext();
            var oe = new OneEntity
            {
                ValueInt = 1,
                ValueString = "1"
            };
            var e = dbContext.Add(oe);
            Assert.Equal(1, dbContext.SaveChanges());
            e.Entity.ValueInt = 2;

            var watcher = new ChangeTrackerWatcher(dbContext);
            watcher.Start();
            e.Entity.ValueInt = 3;
            watcher.End();
            watcher.Revert();

            Assert.Equal(2, e.CurrentValues.GetValue<int>(nameof(OneEntity.ValueInt)));
            Assert.Equal(2, e.Entity.ValueInt);
            Assert.Equal(2, oe.ValueInt);
            Assert.Equal(1, dbContext.SaveChanges());
            Assert.Equal(2, e.CurrentValues.GetValue<int>(nameof(OneEntity.ValueInt)));
            Assert.Equal(2, e.Entity.ValueInt);
            Assert.Equal(2, oe.ValueInt);
        }

        [Fact]
        public static void Test6()
        {
            using (var dbContext = GetTestDbContext())
            {
                var oe = new OneEntity
                {
                    ValueInt = 1,
                    ValueString = "1"
                };
                dbContext.Add(oe);
                Assert.Equal(1, dbContext.SaveChanges());
            }

            using (var dbContext = GetTestDbContext())
            {
                var oe = dbContext.OneEntities.First();
                oe.ValueInt = 2;
                var e = dbContext.Entry(oe);
                Assert.Equal(EntityState.Modified, e.State);
                Assert.False(e.Property(nameof(OneEntity.Id)).IsModified);
                Assert.True(e.Property(nameof(OneEntity.ValueInt)).IsModified);
                Assert.False(e.Property(nameof(OneEntity.ValueString)).IsModified);

                var watcher = new ChangeTrackerWatcher(dbContext);
                watcher.Start();
                dbContext.Remove(oe);
                Assert.Equal(EntityState.Deleted, e.State);
                watcher.End();
                watcher.Revert();

                Assert.Equal(EntityState.Modified, e.State);
                Assert.False(e.Property(nameof(OneEntity.Id)).IsModified);
                Assert.True(e.Property(nameof(OneEntity.ValueInt)).IsModified);
                Assert.False(e.Property(nameof(OneEntity.ValueString)).IsModified);
            }
        }

        [Fact]
        public static void Test7()
        {
            using (var dbContext = GetTestDbContext())
            {
                var oe = new OneEntity
                {
                    ValueInt = 1,
                    ValueString = "1"
                };
                dbContext.Add(oe);
                Assert.Equal(1, dbContext.SaveChanges());
            }

            using (var dbContext = GetTestDbContext())
            {
                var oe = dbContext.OneEntities.First();

                var watcher = new ChangeTrackerWatcher(dbContext);
                watcher.Start();
                dbContext.Remove(oe);
                watcher.End();

                Assert.Equal(1, dbContext.SaveChanges());
            }
        }

        [Fact]
        public static void Test8()
        {
            using var dbContext = GetTestDbContext();
            var w1 = new ChangeTrackerWatcher(dbContext);
            var w2 = new ChangeTrackerWatcher(dbContext);

            w1.Start();
            dbContext.Add(new OneEntity());
            w1.End();

            w2.Start();
            w2.End();

            w1.Revert();
            w2.Revert(); //we want to check w2 can safely revert

            Assert.False(dbContext.ChangeTracker.HasChanges());
        }

        [Fact]
        public static void Test9()
        {
            using (var dbContextInit = GetTestDbContext())
            {
                dbContextInit.Add(new OneEntity
                {
                    ValueInt = 1,
                    ValueString = "1"
                });
                Assert.Equal(1, dbContextInit.SaveChanges());
            }

            using var dbContext = GetTestDbContext();
            var oe = dbContext.OneEntities.Single();

            var w1 = new ChangeTrackerWatcher(dbContext);
            var w2 = new ChangeTrackerWatcher(dbContext);
            Assert.False(dbContext.ChangeTracker.HasChanges());

            w1.Start();
            oe.ValueInt = 2;
            w1.End();
            Assert.True(dbContext.ChangeTracker.HasChanges());

            w2.Start();
            oe.ValueString = "2";
            w2.End();
            Assert.True(dbContext.ChangeTracker.HasChanges());

            Assert.Equal(2, oe.ValueInt);
            Assert.Equal("2", oe.ValueString);

            w2.Revert();
            Assert.Equal(2, oe.ValueInt);
            Assert.Equal("1", oe.ValueString);
            Assert.Equal(EntityState.Modified, dbContext.Entry(oe).State);
            Assert.True(dbContext.ChangeTracker.HasChanges());

            w1.Revert();
            Assert.Equal(1, oe.ValueInt);
            Assert.Equal("1", oe.ValueString);
            Assert.Equal(EntityState.Unchanged, dbContext.Entry(oe).State);
            Assert.False(dbContext.ChangeTracker.HasChanges());
        }

        private static TestDbContext GetTestDbContext([CallerMemberName] string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(dbName ?? throw new ArgumentNullException(nameof(dbName)))
                .Options;
            return new TestDbContext(options);
        }

        private class TestDbContext : DbContext
        {
            public DbSet<OneEntity> OneEntities { get; set; } = default!;
            public DbSet<SecondEntity> SecondEntities { get; set; } = default!;

            public TestDbContext(DbContextOptions<TestDbContext> options)
                : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<OneEntity>();
                modelBuilder.Entity<SecondEntity>();
            }
        }

        private class OneEntity
        {
            public int Id { get; set; }
            public string? ValueString { get; set; }
            public int ValueInt { get; set; }
        }

        private class SecondEntity
        {
            public int Id { get; set; }
            public string? ValueString { get; set; }
            public int ValueInt { get; set; }
        }
    }
}
