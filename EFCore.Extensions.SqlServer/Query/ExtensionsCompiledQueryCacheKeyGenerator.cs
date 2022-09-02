using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace EFCore.Extensions.SqlServer.Query
{
    public class ExtensionsCompiledQueryCacheKeyGenerator : SqlServerCompiledQueryCacheKeyGenerator
    {
        private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;

        public ExtensionsCompiledQueryCacheKeyGenerator(IDiagnosticsLogger<DbLoggerCategory.Query> logger
           , CompiledQueryCacheKeyGeneratorDependencies dependencies
           , RelationalCompiledQueryCacheKeyGeneratorDependencies relationalDependencies)
           : base(dependencies, relationalDependencies)
        {
            _logger = logger;
        }

        public override object GenerateCacheKey(Expression query, bool async)
        {
            var fixer = new ExpressionFixer();
            var newQuery = fixer.Visit(query);
            if (newQuery != query)
                _logger.Logger.LogCritical("expression with dbContext ref found : {0}", query.ToString());
            var key =  base.GenerateCacheKey(newQuery, async);
            return key;
        }

        private class ExpressionFixer : ExpressionVisitor
        {
            protected override Expression VisitConstant(ConstantExpression node)
            {
                if (typeof(DbContext).IsAssignableFrom(node.Type) && node.Value != null)
                    return Expression.Constant(null, node.Type);
                return base.VisitConstant(node);
            }
        }
    }
}
