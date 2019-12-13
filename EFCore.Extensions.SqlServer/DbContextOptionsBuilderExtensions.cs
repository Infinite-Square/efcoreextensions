using EFCore.Extensions.SqlCommandCatching;
using EFCore.Extensions.SqlConnectionUtilities;
using EFCore.Extensions.SqlServer.Query.ExpressionVisitors;
using EFCore.Extensions.SqlServer.Query.Sql.Internal;
using EFCore.Extensions.SqlServer.Storage.Internal;
using EFCore.Extensions.SqlServer.Update.Internal;
using EFCore.Extensions.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Microsoft.EntityFrameworkCore
{
    public static class DbContextOptionsBuilderExtensions
    {
        private static DbContextOptionsBuilder UseSqlServerServices(this DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ReplaceService<IQuerySqlGeneratorFactory, ExtensionsQuerySqlGeneratorFactory>();
            optionsBuilder.ReplaceService<IEntityQueryableExpressionVisitorFactory, ExtensionsRelationalEntityQueryableExpressionVisitorFactory>();

            optionsBuilder.ReplaceService<IResultOperatorHandler, ResultOperatorHandler>();
            optionsBuilder.ReplaceService<IQueryAnnotationExtractor, QueryAnnotationExtractor>();

            return optionsBuilder;
        }

        private static DbContextOptionsBuilder<TContext> UseSqlServerServices<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder)
            where TContext : DbContext
        {
            ((DbContextOptionsBuilder)optionsBuilder).UseSqlServerServices();
            return optionsBuilder;
        }

        public static void UseSqlServer(this ExtensionsDbContextOptionsBuilder optionsBuilder, string connectionString, Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction = null)
        {
            optionsBuilder.OptionsBuilder
                .UseSqlServerServices()
                .UseSqlServer(connectionString, sqlServerOptionsAction);
        }

        public static void UseSqlServer(this ExtensionsDbContextOptionsBuilder optionsBuilder, DbConnection connection, Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction = null)
        {
            optionsBuilder.OptionsBuilder
                .UseSqlServerServices()
                .UseSqlServer(connection, sqlServerOptionsAction);
        }

        public static void UseSqlServer<TContext>(this ExtensionsDbContextOptionsBuilder<TContext> optionsBuilder, string connectionString, Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction = null) where TContext : DbContext
        {
            optionsBuilder.OptionsBuilder
                .UseSqlServerServices()
                .UseSqlServer(connectionString, sqlServerOptionsAction);
        }

        public static void UseSqlServer<TContext>(this ExtensionsDbContextOptionsBuilder<TContext> optionsBuilder, DbConnection connection, Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction = null) where TContext : DbContext
        {
            optionsBuilder.OptionsBuilder
                .UseSqlServerServices()
                .UseSqlServer(connection, sqlServerOptionsAction);
        }

        public static void EnableSqlServerCommandCatcher(this ExtensionsDbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.OptionsBuilder
                .ReplaceService<IRelationalConnection, ExtensionsSqlServerConnection>()
                .ReplaceService<IRelationalTransactionFactory, ExtensionsRelationalTransactionFactory>()
                .ReplaceService<IModificationCommandBatchFactory, ExtensionsSqlServerModificationCommandBatchFactory>();
            var infra = (IDbContextOptionsBuilderInfrastructure)optionsBuilder.OptionsBuilder;
            infra.AddOrUpdateExtension(new SqlServerCommandCatcherExtension());
        }

        public static void EnableSqlServerModificationCommandBatchEvents(this ExtensionsDbContextOptionsBuilder optionsBuilder
            , ISqlServerModificationCommandBatchEvents events)
        {
            optionsBuilder.OptionsBuilder
                .ReplaceService<IModificationCommandBatchFactory, ExtensionsSqlServerModificationCommandBatchFactory>();
            var infra = (IDbContextOptionsBuilderInfrastructure)optionsBuilder.OptionsBuilder;
            infra.AddOrUpdateExtension(new SqlServerModificationCommandBatchEventsExtension(events));
        }

        private class SqlServerModificationCommandBatchEventsExtension : IDbContextOptionsExtension
        {
            private ISqlServerModificationCommandBatchEvents _events;

            public SqlServerModificationCommandBatchEventsExtension(ISqlServerModificationCommandBatchEvents events)
            {
                _events = events;
            }

            public string LogFragment => string.Empty;

            public bool ApplyServices(IServiceCollection services)
            {
                services.AddSingleton(_events);
                return true;
            }

            public long GetServiceProviderHashCode() => 0;

            public void Validate(IDbContextOptions options)
            {
            }
        }

        private class SqlServerCommandCatcherExtension : IDbContextOptionsExtension
        {
            public string LogFragment => string.Empty;

            public bool ApplyServices(IServiceCollection services)
            {
                // scoped to dbcontext
                services.AddScoped<SqlCommandCatcher>();
                services.AddScoped<ISqlCommandCatcher>(_ => _.GetRequiredService<SqlCommandCatcher>());
                services.AddScoped<ISqlCommandCatchingState>(_ => _.GetRequiredService<SqlCommandCatcher>());
                services.AddScoped<ISqlCommandCatchingStore>(_ => _.GetRequiredService<SqlCommandCatcher>());
                return true;
            }

            public long GetServiceProviderHashCode() => 0;

            public void Validate(IDbContextOptions options)
            {
            }
        }
    }
}
