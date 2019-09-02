using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query.ResultOperators;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Parsing.Structure.IntermediateModel;
using Microsoft.EntityFrameworkCore.Internal;
using Remotion.Linq.Clauses.StreamedData;

namespace EFCore.Extensions.SqlServer.Query.ResultOperators.Internal
{
    public class ForSystemTimeAsOfExpressionNode : ResultOperatorExpressionNodeBase
    {
        public static readonly IReadOnlyCollection<MethodInfo> SupportedMethods = new[]
        {
            EntityFrameworkQueryableExtensions.ForSystemTimeAsOfMethodInfo
        };

        private readonly ConstantExpression _dateTime;

        public ForSystemTimeAsOfExpressionNode(MethodCallExpressionParseInfo parseInfo
            , ConstantExpression dateTime)
            : base(parseInfo, null, null)
        {
            _dateTime = dateTime;
        }

        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
        {
            return Source.Resolve(inputParameter, expressionToBeResolved, clauseGenerationContext);
        }

        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext)
        {
            var prm = Expression.Parameter(typeof(object));
            var pathFromQuerySource = Resolve(prm, prm, clauseGenerationContext);

            var @operator = new ForSystemTimeAsOfResultOperator(pathFromQuerySource, _dateTime);
            clauseGenerationContext.AddContextInfo(this, @operator);
            return @operator;
        }
    }

    public class ForSystemTimeAsOfResultOperator : SequenceTypePreservingResultOperatorBase, IQueryAnnotation
    {
        private IQuerySource _querySource;
        private readonly Expression _pathFromQuerySource;
        private readonly ConstantExpression _dateTime;

        public DateTime DateTime => (DateTime)_dateTime.Value;

        public ForSystemTimeAsOfResultOperator(Expression pathFromQuerySource, ConstantExpression dateTime)
        {
            _pathFromQuerySource = pathFromQuerySource;
            _dateTime = dateTime;
        }

        private static IQuerySource GetQuerySource(Expression expression)
            => expression.TryGetReferencedQuerySource()
               ?? (expression is MemberExpression memberExpression
                   ? GetQuerySource(memberExpression.Expression.RemoveConvert())
                   : null);

        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input)
        {
            return input;
        }

        public override ResultOperatorBase Clone(CloneContext cloneContext)
        {
            return new ForSystemTimeAsOfResultOperator(_pathFromQuerySource, _dateTime);
        }

        public override void TransformExpressions(Func<Expression, Expression> transformation)
        {
        }

        public virtual IQuerySource QuerySource
        {
            get => _querySource ?? (_querySource = GetQuerySource(_pathFromQuerySource));
            set => _querySource = value;
        }

        public QueryModel QueryModel { get; set; }
    }
}
