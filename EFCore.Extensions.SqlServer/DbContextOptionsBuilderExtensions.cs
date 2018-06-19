using EFCore.Extensions.SqlServer.Query.ExpressionVisitors;
using EFCore.Extensions.SqlServer.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.Sql;
using System;
using System.Data.Common;

namespace Microsoft.EntityFrameworkCore
{
    public static class DbContextOptionsBuilderExtensions
    {
        private static DbContextOptionsBuilder UseSqlServerServices(this DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ReplaceService<IQuerySqlGeneratorFactory, ExtensionsQuerySqlGeneratorFactory>();
            //optionsBuilder.ReplaceService<IResultOperatorHandler, ResultOperatorHandler>();
            //optionsBuilder.ReplaceService<IQueryAnnotationExtractor, QueryAnnotationExtractor>();
            optionsBuilder.ReplaceService<IEntityQueryableExpressionVisitorFactory, ExtensionsRelationalEntityQueryableExpressionVisitorFactory>();
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
    }
}
