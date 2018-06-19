using EFCore.Extensions.Query;
using EFCore.Extensions.Query.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System;

namespace Microsoft.EntityFrameworkCore
{
    public static class DbContextOptionsBuilderExtensions
    {
        private static DbContextOptionsBuilder UseInMemoryDatabaseServices(this DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ReplaceService<INodeTypeProviderFactory, ExtensionsMethodInfoBasedNodeTypeRegistryFactory>();
            optionsBuilder.ReplaceService<IResultOperatorHandler, ExtensionsResultOperatorHandler>();
            optionsBuilder.ReplaceService<IQueryAnnotationExtractor, ExtensionsQueryAnnotationExtractor>();
            return optionsBuilder;
        }

        private static DbContextOptionsBuilder<TContext> UseInMemoryDatabaseServices<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder)
            where TContext : DbContext
        {
            ((DbContextOptionsBuilder)optionsBuilder).UseInMemoryDatabaseServices();
            return optionsBuilder;
        }

        public static void UseInMemoryDatabase<TContext>(this ExtensionsDbContextOptionsBuilder<TContext> optionsBuilder, string databaseName, Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null) where TContext : DbContext
        {
            optionsBuilder.OptionsBuilder
                .UseInMemoryDatabaseServices()
                .UseInMemoryDatabase(databaseName, inMemoryOptionsAction);
        }

        public static void UseInMemoryDatabase(this ExtensionsDbContextOptionsBuilder optionsBuilder, string databaseName, Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null)
        {
            optionsBuilder.OptionsBuilder
                .UseInMemoryDatabaseServices()
                .UseInMemoryDatabase(databaseName, inMemoryOptionsAction);
        }

        public static void UseInMemoryDatabase<TContext>(this ExtensionsDbContextOptionsBuilder<TContext> optionsBuilder, string databaseName, InMemoryDatabaseRoot databaseRoot, Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null) where TContext : DbContext
        {
            optionsBuilder.OptionsBuilder
                .UseInMemoryDatabaseServices()
                .UseInMemoryDatabase(databaseName, databaseRoot, inMemoryOptionsAction);
        }

        public static void UseInMemoryDatabase(this ExtensionsDbContextOptionsBuilder optionsBuilder, string databaseName, InMemoryDatabaseRoot databaseRoot, Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null)
        {
            optionsBuilder.OptionsBuilder
                .UseInMemoryDatabaseServices()
                .UseInMemoryDatabase(databaseName, databaseRoot, inMemoryOptionsAction);
        }
    }
}
