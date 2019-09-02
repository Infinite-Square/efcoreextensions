using System;
using System.Linq.Expressions;
using EFCore.Extensions.SqlServer.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Remotion.Linq.Clauses;

namespace EFCore.Extensions.SqlServer.Query.Expressions
{
    public class ForSystemTimeAsOfTableExpression : TableExpression
    {
        public ForSystemTimeAsOfTableExpression(string table, string schema, string alias, IQuerySource querySource
            , DateTime dateTime)
            : base(table, schema, alias, querySource)
        {
            DateTime = dateTime;
        }

        public DateTime DateTime { get; }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            return (visitor ?? throw new ArgumentNullException(nameof(visitor))) is ExtensionsQuerySqlGenerator specificVisitor
                ? specificVisitor.VisitForSystemTimeAsOf(this)
                : base.Accept(visitor);
        }
    }
}
