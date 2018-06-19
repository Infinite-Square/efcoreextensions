using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Remotion.Linq.Clauses;
using System;
using System.Linq.Expressions;

namespace EFCore.Extensions.SqlServer.Query.ExpressionVisitors
{
    public class ExtensionsRelationalEntityQueryableExpressionVisitorFactory : RelationalEntityQueryableExpressionVisitorFactory
    {
        public ExtensionsRelationalEntityQueryableExpressionVisitorFactory(RelationalEntityQueryableExpressionVisitorDependencies dependencies) 
            : base(dependencies)
        {
        }

        public override ExpressionVisitor Create(EntityQueryModelVisitor queryModelVisitor, IQuerySource querySource)
        {
            return new SqlServerExtensionsRelationalEntityQueryableExpressionVisitor(
                Dependencies,
                queryModelVisitor as RelationalQueryModelVisitor ?? throw new ArgumentNullException(nameof(queryModelVisitor)),
                querySource);
        }
    }
}
