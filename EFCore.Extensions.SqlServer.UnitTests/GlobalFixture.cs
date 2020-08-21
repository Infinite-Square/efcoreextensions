using EFCore.Extensions.SqlServer.UnitTests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Threading.Tasks;
using Xunit;

namespace EFCore.Extensions.SqlServer.UnitTests
{
    public sealed class GlobalTrueDatabaseFixture : IAsyncLifetime, IDisposable
    {
        private readonly ServiceProvider _services;

        public IServiceProvider Services => _services;

        public GlobalTrueDatabaseFixture()
        {
            var appSettings = AppSettings.Load();
            var services = new ServiceCollection()
                .AddDbContext<DataContext>(options =>
                    options.UseLoggerFactory(new LoggerFactory(new[]
                    {
                        new DebugLoggerProvider()
                    })).UseExtensions(extensions =>
                    {
                        extensions.UseSqlServer(appSettings.ConnectionString);
                        //extensions.EnableSqlServerCommandCatcher();
                    })
                    , ServiceLifetime.Transient
                    , ServiceLifetime.Singleton);
            _services = services.BuildServiceProvider(false);
        }

        public async Task InitializeAsync()
        {
            using (var ctx = Services.GetRequiredService<DataContext>())
            {
                await ctx.Database.EnsureDeletedAsync();
                await ctx.Database.MigrateAsync();

                //await ctx.Database.EnsureCreatedAsync();
            }
        }

        public void Dispose()
        {
            _services.Dispose();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }

    public class GlobalFakeDatabaseFixture : IAsyncLifetime, IDisposable
    {
        private readonly ServiceProvider _services;

        public IServiceProvider Services => _services;

        public GlobalFakeDatabaseFixture()
        {
            var services = new ServiceCollection()
                .AddDbContext<DataContext>(options =>
                    options.UseExtensions(extensions =>
                    {
                        extensions.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=fakedb;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
                        extensions.EnableSqlServerCommandCatcher();
                    })
                    , ServiceLifetime.Transient
                    , ServiceLifetime.Singleton);
            _services = services.BuildServiceProvider(false);
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _services.Dispose();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }

    [CollectionDefinition("globaltruedatabase")]
    public class GlobalTrueDatabaseCollection : ICollectionFixture<GlobalTrueDatabaseFixture>
    {
    }

    [CollectionDefinition("globalfakedatabase")]
    public class GlobalFakeDatabaseCollection : ICollectionFixture<GlobalFakeDatabaseFixture>
    {
    }
}
