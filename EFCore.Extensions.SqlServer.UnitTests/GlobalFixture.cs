using EFCore.Extensions.SqlServer.UnitTests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace EFCore.Extensions.SqlServer.UnitTests
{
    public class GlobalFixture : IAsyncLifetime, IDisposable
    {
        private readonly ServiceProvider _services;

        public IServiceProvider Services => _services;

        public GlobalFixture()
        {
            var appSettings = AppSettings.Load();
            var services = new ServiceCollection()
                .AddDbContext<DataContext>(options =>
                    options.UseExtensions(extensions =>
                        extensions.UseSqlServer(appSettings.ConnectionString))
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

    [CollectionDefinition("global")]
    public class GlobalCollection : ICollectionFixture<GlobalFixture>
    {
    }
}
