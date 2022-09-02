using EFCore.Extensions.Internal;
using EFCore.Extensions.SqlCommandCatching;
using EFCore.Extensions.SqlServer.Query;
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
using System.Data.Common;

namespace Microsoft.EntityFrameworkCore
{
    public static class DbContextOptionsBuilderExtensions
    {
        private static readonly ServiceReplacementCollection _baseServiceReplacements = ServiceReplacement.Collection(s => s
            .Add<IQuerySqlGeneratorFactory, ExtensionsQuerySqlGeneratorFactory>()
            .Add<IEntityQueryableExpressionVisitorFactory, ExtensionsRelationalEntityQueryableExpressionVisitorFactory>()
            .Add<IResultOperatorHandler, ResultOperatorHandler>()
            .Add<IQueryAnnotationExtractor, QueryAnnotationExtractor>()
            .Add<ICompiledQueryCacheKeyGenerator, ExtensionsCompiledQueryCacheKeyGenerator>()
        );

        private static readonly ServiceReplacementCollection _commandCatcherReplacements = ServiceReplacement.Collection(s => s
            .Add<IRelationalConnection, ExtensionsSqlServerConnection>()
            .Add<IRelationalTransactionFactory, ExtensionsRelationalTransactionFactory>()
            .Add<IModificationCommandBatchFactory, ExtensionsSqlServerModificationCommandBatchFactory>()
        );

        private static readonly ServiceReplacementCollection _modificationCommandBatchEventsReplacements = ServiceReplacement.Collection(s => s
            .Add<IModificationCommandBatchFactory, ExtensionsSqlServerModificationCommandBatchFactory>()
        );

        public static IServiceCollection AddEntityFrameworkSqlServerExtensions(this IServiceCollection services, Action<SqlServerExtensionsOptions> options = null)
        {
            services.AddEntityFrameworkExtensions();
            _baseServiceReplacements.Apply(services);
            options?.Invoke(new SqlServerExtensionsOptions(services));
            return services;
        }

        private static DbContextOptionsBuilder UseSqlServerServices(this DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsInternalServiceProviderConfigured())
                _baseServiceReplacements.Apply(optionsBuilder);
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

        public static SqlServerExtensionsOptions EnableSqlServerCommandCatcher(this SqlServerExtensionsOptions self)
        {
            _commandCatcherReplacements.Apply(self.Services);
            return self;
        }

        public static void EnableSqlServerCommandCatcher(this ExtensionsDbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.OptionsBuilder.IsInternalServiceProviderConfigured())
                _commandCatcherReplacements.Apply(optionsBuilder.OptionsBuilder);
            var infra = (IDbContextOptionsBuilderInfrastructure)optionsBuilder.OptionsBuilder;
            infra.AddOrUpdateExtension(new SqlServerCommandCatcherExtension());
        }

        public static SqlServerExtensionsOptions EnableSqlServerModificationCommandBatchEvents(this SqlServerExtensionsOptions self
            , Func<ISqlServerModificationCommandBatchEvents> events)
        {
            _modificationCommandBatchEventsReplacements.Apply(self.Services);
            self.Services.AddScoped(_ => events());
            return self;
        }

        public static SqlServerExtensionsOptions EnableSqlServerModificationCommandBatchEvents<T>(this SqlServerExtensionsOptions self
            , T ctx
            , Func<T, ISqlServerModificationCommandBatchEvents> events)
        {
            _modificationCommandBatchEventsReplacements.Apply(self.Services);
            self.Services.AddScoped(_ => events(ctx));
            return self;
        }

        public static void EnableSqlServerModificationCommandBatchEvents(this ExtensionsDbContextOptionsBuilder optionsBuilder
            , Func<ISqlServerModificationCommandBatchEvents> events)
        {
            if (optionsBuilder.OptionsBuilder.IsInternalServiceProviderConfigured())
                throw new InvalidOperationException();

            _modificationCommandBatchEventsReplacements.Apply(optionsBuilder.OptionsBuilder);
            var infra = (IDbContextOptionsBuilderInfrastructure)optionsBuilder.OptionsBuilder;
            infra.AddOrUpdateExtension(new SqlServerModificationCommandBatchEventsExtension(events));
        }

        public static void EnableSqlServerModificationCommandBatchEvents<T>(this ExtensionsDbContextOptionsBuilder optionsBuilder
            , T ctx
            , Func<T, ISqlServerModificationCommandBatchEvents> events)
        {
            if (optionsBuilder.OptionsBuilder.IsInternalServiceProviderConfigured())
                throw new InvalidOperationException();

            _modificationCommandBatchEventsReplacements.Apply(optionsBuilder.OptionsBuilder);
            var infra = (IDbContextOptionsBuilderInfrastructure)optionsBuilder.OptionsBuilder;
            infra.AddOrUpdateExtension(new SqlServerModificationCommandBatchEventsExtension<T>(ctx, events));
        }

        private class SqlServerModificationCommandBatchEventsExtension : IDbContextOptionsExtension
        {
            private readonly Func<ISqlServerModificationCommandBatchEvents> _events;

            public SqlServerModificationCommandBatchEventsExtension(Func<ISqlServerModificationCommandBatchEvents> events)
            {
                _events = events;
            }

            public string LogFragment => string.Empty;

            public bool ApplyServices(IServiceCollection services)
            {
                var builder = new EntityFrameworkServicesBuilder(services)
                    .TryAddProviderSpecificServices(map => map
                        .TryAddScoped(_ => _events()));
                //services.AddScoped(_ => _events());
                return true;
            }

            public long GetServiceProviderHashCode() => 0;// _events.GetHashCode();

            public void Validate(IDbContextOptions options)
            {
            }
        }

        private class SqlServerModificationCommandBatchEventsExtension<T> : IDbContextOptionsExtension
        {
            private readonly T _ctx;
            private readonly Func<T, ISqlServerModificationCommandBatchEvents> _events;

            public SqlServerModificationCommandBatchEventsExtension(T ctx, Func<T, ISqlServerModificationCommandBatchEvents> events)
            {
                _ctx = ctx;
                _events = events;
            }

            public string LogFragment => string.Empty;

            public bool ApplyServices(IServiceCollection services)
            {
                var builder = new EntityFrameworkServicesBuilder(services)
                    .TryAddProviderSpecificServices(map => map
                        .TryAddScoped(_ => _events(_ctx)));
                //services.AddScoped(_ => _events());
                return true;
            }

            public long GetServiceProviderHashCode() => 0;// _events.GetHashCode();

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

        public class SqlServerExtensionsOptions
        {
            public SqlServerExtensionsOptions(IServiceCollection services)
            {
                Services = services;
            }

            internal IServiceCollection Services { get; }
        }
    }
}
