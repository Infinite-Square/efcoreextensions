using EFCore.Extensions.SqlServer.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Remotion.Linq.Clauses;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace EFCore.Extensions.SqlServer.Query.Expressions
{
    public class ValueFromOpenJsonExpression : TableExpressionBase
    {
        public Dictionary<Expression, IProperty> PropertyMapping { get; } = new Dictionary<Expression, IProperty>();

        public ValueFromOpenJsonExpression(IQuerySource querySource
            , Expression json
            , Expression path
            , string alias)
            : base(querySource, alias)
        {
            Json = json;
            Path = path;
        }

        public Expression Json { get; }
        public Expression Path { get; }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var fix = new FixVisitor();
            var newJsonExpression = visitor.Visit(fix.Visit(Json));
            var newPathExpression = visitor.Visit(fix.Visit(Path));

            return newJsonExpression != Json
                   || newPathExpression != Path
                ? new ValueFromOpenJsonExpression(QuerySource, newJsonExpression, newPathExpression, Alias)
                : this;
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            return (visitor ?? throw new ArgumentNullException(nameof(visitor))) is ExtensionsQuerySqlGenerator specificVisitor
                ? specificVisitor.VisitValueFromOpenJson(this)
                : base.Accept(visitor);
        }

        private class FixVisitor : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node.Name == null)
                    return Expression.Parameter(node.Type, string.Empty);
                return base.VisitParameter(node);
            }
        }
    }
}
