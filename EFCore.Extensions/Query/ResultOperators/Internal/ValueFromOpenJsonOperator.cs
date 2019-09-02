using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.ResultOperators;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Clauses.StreamedData;
using Remotion.Linq.Parsing.Structure.IntermediateModel;
using System;
using System.Linq.Expressions;

namespace EFCore.Extensions.Query.ResultOperators.Internal
{
    public class ValueFromOpenJsonOperator : SequenceTypePreservingResultOperatorBase, ICloneableQueryAnnotation
    {
        public MethodCallExpressionParseInfo ParseInfo { get; }
        public Expression Json { get; private set; }
        public Expression Path { get; }
        public CompilationContext Context { get; }

        public ValueFromOpenJsonOperator(MethodCallExpressionParseInfo parseInfo, Expression json, Expression path)
        {
            ParseInfo = parseInfo;
            Json = json;
            Path = path;
            Context = new CompilationContext();
        }

        ICloneableQueryAnnotation ICloneableQueryAnnotation.Clone(IQuerySource querySource, QueryModel queryModel)
            => new ValueFromOpenJsonOperator(ParseInfo, Json, Path) { QuerySource = querySource, QueryModel = queryModel };

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual IQuerySource QuerySource { get; set; }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual QueryModel QueryModel { get; set; }

        /// <summary>
        /////     This API supports the Entity Framework Core infrastructure and is not intended to be used
        /////     directly from your code. This API may change or be removed in future releases.
        ///// </summary>
        //public virtual Expression Arguments { get; }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override string ToString() => $"ValueFromOpenJson({Json}, {Path})";

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override ResultOperatorBase Clone(CloneContext cloneContext)
            => new ValueFromOpenJsonOperator(ParseInfo, Json, Path);

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override void TransformExpressions(Func<Expression, Expression> transformation)
        {
            Json = transformation(Json);
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input)
        {
            return input;
        }

        public class CompilationContext
        {
            public Expression Json { get; set; }
            public SelectExpression SelectExpression { get; set; }
            public ISqlTranslatingExpressionVisitorFactory SqlTranslatingExpressionVisitorFactory { get; set; }
        }
    }
}
