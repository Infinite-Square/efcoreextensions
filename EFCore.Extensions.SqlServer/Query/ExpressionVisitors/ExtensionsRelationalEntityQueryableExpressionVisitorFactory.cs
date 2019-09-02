using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Remotion.Linq.Clauses;
using System;
using System.Linq.Expressions;

namespace EFCore.Extensions.SqlServer.Query.ExpressionVisitors
{
    public class ExtensionsRelationalEntityQueryableExpressionVisitorFactory : RelationalEntityQueryableExpressionVisitorFactory
    {
        private readonly ISqlTranslatingExpressionVisitorFactory _sqlTranslatingExpressionVisitorFactory;
        private readonly IQueryModelGenerator _queryModelGenerator;

        public ExtensionsRelationalEntityQueryableExpressionVisitorFactory(RelationalEntityQueryableExpressionVisitorDependencies dependencies
            , ISqlTranslatingExpressionVisitorFactory sqlTranslatingExpressionVisitorFactory
            , IQueryModelGenerator queryModelGenerator) 
            : base(dependencies)
        {
            _sqlTranslatingExpressionVisitorFactory = sqlTranslatingExpressionVisitorFactory;
            _queryModelGenerator = queryModelGenerator;
        }

        public override ExpressionVisitor Create(EntityQueryModelVisitor queryModelVisitor, IQuerySource querySource)
        {
            return new SqlServerExtensionsRelationalEntityQueryableExpressionVisitor(
                Dependencies,
                queryModelVisitor as RelationalQueryModelVisitor ?? throw new ArgumentNullException(nameof(queryModelVisitor)),
                querySource,
                _sqlTranslatingExpressionVisitorFactory,
                _queryModelGenerator);
        }
    }
}
