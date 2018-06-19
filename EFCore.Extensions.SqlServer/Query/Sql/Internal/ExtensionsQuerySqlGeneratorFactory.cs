using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Sql.Internal;
using System;

namespace EFCore.Extensions.SqlServer.Query.Sql.Internal
{
    public class ExtensionsQuerySqlGeneratorFactory : SqlServerQuerySqlGeneratorFactory
    {
        private readonly ISqlServerOptions _sqlServerOptions;

        public ExtensionsQuerySqlGeneratorFactory(QuerySqlGeneratorDependencies dependencies
            , ISqlServerOptions sqlServerOptions)
            : base(dependencies, sqlServerOptions)
        {
            _sqlServerOptions = sqlServerOptions;
        }

        public override IQuerySqlGenerator CreateDefault(SelectExpression selectExpression)
        {
            return new ExtensionsQuerySqlGenerator(Dependencies
                , selectExpression ?? throw new ArgumentNullException(nameof(selectExpression))
                , _sqlServerOptions.RowNumberPagingEnabled);
        }
    }
}
