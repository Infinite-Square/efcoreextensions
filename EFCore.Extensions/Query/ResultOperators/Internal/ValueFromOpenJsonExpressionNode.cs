using Microsoft.EntityFrameworkCore;
using Remotion.Linq.Clauses;
using Remotion.Linq.Parsing.Structure.IntermediateModel;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace EFCore.Extensions.Query.ResultOperators.Internal
{
    public class ValueFromOpenJsonExpressionNode : ResultOperatorExpressionNodeBase
    {
        public static IEnumerable<MethodInfo> GetSupportedMethods()
            => new[] { ExtensionsDbFunctionsExtensions.ValueFromOpenJsonMethod };

        private readonly MethodCallExpressionParseInfo _parseInfo;
        private readonly Expression _sql;
        private readonly Expression _arguments;

        public ValueFromOpenJsonExpressionNode(MethodCallExpressionParseInfo parseInfo
            , Expression sql
            , Expression arguments)
            : base(parseInfo, null, null)
        {
            _parseInfo = parseInfo;
            _sql = sql;
            _arguments = arguments;
        }

        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext)
        {
            return new ValueFromOpenJsonOperator(_parseInfo, _sql, _arguments);
        }

        public override Expression Resolve(
            ParameterExpression inputParameter,
            Expression expressionToBeResolved,
            ClauseGenerationContext clauseGenerationContext)
        {
            return Source.Resolve(inputParameter, expressionToBeResolved, clauseGenerationContext);
        }
    }
}
