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

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            return (visitor ?? throw new ArgumentNullException(nameof(visitor))) is ExtensionsQuerySqlGenerator specificVisitor
                ? specificVisitor.VisitValueFromOpenJson(this)
                : base.Accept(visitor);
        }
    }
}
