using EFCore.Extensions.Query.ResultOperators.Internal;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.ResultOperators;
using Remotion.Linq;
using Remotion.Linq.Clauses.Expressions;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EFCore.Extensions.Query.Internal
{
    public class ExtensionsQueryAnnotationExtractor : IQueryAnnotationExtractor
    {
        public virtual IReadOnlyCollection<IQueryAnnotation> ExtractQueryAnnotations(QueryModel queryModel)
        {
            var queryAnnotations = new List<IQueryAnnotation>();

            ExtractQueryAnnotations(queryModel, queryAnnotations);

            return queryAnnotations;
        }

        private static void ExtractQueryAnnotations(
            QueryModel queryModel, ICollection<IQueryAnnotation> queryAnnotations)
        {
            queryModel
                .TransformExpressions(e =>
                    ExtractQueryAnnotations(e, queryAnnotations));

            foreach (var resultOperator in queryModel.ResultOperators.ToList())
            {
                if (resultOperator is IQueryAnnotation queryAnnotation)
                {
                    queryAnnotations.Add(queryAnnotation);

                    queryAnnotation.QueryModel = queryModel;

                    if (queryAnnotation.QuerySource == null)
                        queryAnnotation.QuerySource = queryModel.MainFromClause;

                    if (!(resultOperator is ValueFromOpenJsonOperator))
                        queryModel.ResultOperators.Remove(resultOperator);
                }
            }
        }

        private static Expression ExtractQueryAnnotations(
            Expression expression, ICollection<IQueryAnnotation> queryAnnotations)
        {
            new QueryAnnotationExtractingVisitor(queryAnnotations).Visit(expression);

            return expression;
        }

        private class QueryAnnotationExtractingVisitor : ExpressionVisitorBase
        {
            private readonly ICollection<IQueryAnnotation> _queryAnnotations;

            public QueryAnnotationExtractingVisitor(ICollection<IQueryAnnotation> queryAnnotations)
            {
                _queryAnnotations = queryAnnotations;
            }

            protected override Expression VisitSubQuery(SubQueryExpression expression)
            {
                ExtractQueryAnnotations(expression.QueryModel, _queryAnnotations);

                return expression;
            }
        }
    }
}
