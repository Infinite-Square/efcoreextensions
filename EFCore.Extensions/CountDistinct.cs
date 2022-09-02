using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Clauses.StreamedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

//https://stackoverflow.com/questions/56803706/how-to-get-count-distinct-in-translated-sql-with-ef-core/56831483#56831483

namespace Remotion.Linq.Parsing.Structure.IntermediateModel
{
    public sealed class CountDistinctExpressionNode : ResultOperatorExpressionNodeBase
    {
        public static IEnumerable<MethodInfo> GetSupportedMethods()
            => typeof(ExtensionsDbFunctionsExtensions).GetTypeInfo().GetDeclaredMethods(nameof(ExtensionsDbFunctionsExtensions.CountDistinct));

        public CountDistinctExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression optionalSelector)
            : base(parseInfo, null, optionalSelector) { }
        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
            => throw CreateResolveNotSupportedException();
        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext)
            => new CountDistinctResultOperator();
    }
}

namespace Remotion.Linq.Clauses.ResultOperators
{
    public sealed class CountDistinctResultOperator : ValueFromSequenceResultOperatorBase
    {
        public override ResultOperatorBase Clone(CloneContext cloneContext) => new CountDistinctResultOperator();
        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input) => throw new NotSupportedException();
        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo) => new StreamedScalarValueInfo(typeof(int));
        public override string ToString() => "CountDistinct()";
        public override void TransformExpressions(Func<Expression, Expression> transformation) { }
    }
}

namespace EFCore.Extensions.Query.Internal
{
    public partial class ExtensionsRelationalResultOperatorHandler
    {
        private static readonly ISet<Type> _aggregateResultOperators = (ISet<Type>)
            typeof(RequiresMaterializationExpressionVisitor).GetField("_aggregateResultOperators", BindingFlags.NonPublic | BindingFlags.Static)
            .GetValue(null);

        static ExtensionsRelationalResultOperatorHandler()
            => _aggregateResultOperators.Add(typeof(CountDistinctResultOperator));

        private Expression HandleCountDistinct(EntityQueryModelVisitor entityQueryModelVisitor, QueryModel queryModel)
        {
            var queryModelVisitor = (RelationalQueryModelVisitor)entityQueryModelVisitor;
            var selectExpression = queryModelVisitor.TryGetQuery(queryModel.MainFromClause);
            var inputType = queryModel.SelectClause.Selector.Type;
            if (CanEvalOnServer(queryModelVisitor)
                && selectExpression != null
                && selectExpression.Projection.Count == 1)
            {
                PrepareSelectExpressionForAggregate(selectExpression, queryModel);
                var expression = selectExpression.Projection[0];
                var subExpression = new SqlFunctionExpression(
                    "DISTINCT", inputType, new[] { expression.UnwrapAliasExpression() });
                selectExpression.SetProjectionExpression(new SqlFunctionExpression(
                    "COUNT", typeof(int), new[] { subExpression }));
                return new ResultTransformingExpressionVisitor<int>(
                    queryModelVisitor.QueryCompilationContext, false)
                    .Visit(queryModelVisitor.Expression);
            }
            else
            {
                queryModelVisitor.RequiresClientResultOperator = true;
                var typeArgs = new[] { inputType };
                var distinctCall = Expression.Call(
                    typeof(Enumerable), "Distinct", typeArgs,
                    queryModelVisitor.Expression);
                return Expression.Call(
                    typeof(Enumerable), "Count", typeArgs,
                    distinctCall);
            }
        }

        private static bool CanEvalOnServer(RelationalQueryModelVisitor queryModelVisitor) =>
            !queryModelVisitor.RequiresClientEval && !queryModelVisitor.RequiresClientSelectMany &&
            !queryModelVisitor.RequiresClientJoin && !queryModelVisitor.RequiresClientFilter &&
            !queryModelVisitor.RequiresClientOrderBy && !queryModelVisitor.RequiresClientResultOperator &&
            !queryModelVisitor.RequiresStreamingGroupResultOperator;
    }
}
