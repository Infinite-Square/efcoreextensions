using EFCore.Extensions.Query.ResultOperators.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EFCore.Extensions.Query
{
    using ResultHandler = Func<EntityQueryModelVisitor, ResultOperatorBase, QueryModel, Expression>;

    public class ExtensionsResultOperatorHandler : ResultOperatorHandler
    {
        private static readonly Dictionary<Type, ResultHandler> _handlers
            = new Dictionary<Type, ResultHandler>
            {
                { typeof(ValueFromOpenJsonOperator), (v, r, q) => HandleValueFromOpenJson(v, (ValueFromOpenJsonOperator)r, q) },
            };

        public ExtensionsResultOperatorHandler(ResultOperatorHandlerDependencies dependencies)
            : base(dependencies)
        {
        }

        public override Expression HandleResultOperator(EntityQueryModelVisitor entityQueryModelVisitor, ResultOperatorBase resultOperator, QueryModel queryModel)
        {
            if (_handlers.TryGetValue(resultOperator.GetType(), out var handler))
                return handler(entityQueryModelVisitor, resultOperator, queryModel);

            return base.HandleResultOperator(entityQueryModelVisitor, resultOperator, queryModel);
        }

        private static PropertyInfo QuerySourcesRequiringMaterialization = typeof(QueryCompilationContext)
            .GetProperty(nameof(QuerySourcesRequiringMaterialization), BindingFlags.NonPublic | BindingFlags.Instance);

        private static Expression HandleValueFromOpenJson(
            EntityQueryModelVisitor entityQueryModelVisitor,
            ValueFromOpenJsonOperator valueFromOpenJsonOperator,
            QueryModel queryModel)
        {
            var visitor = new MemberAccessBindingExpressionVisitor(entityQueryModelVisitor.QueryCompilationContext.QuerySourceMapping
                , entityQueryModelVisitor
                , false);

            if (valueFromOpenJsonOperator.Json is MemberExpression me)
            {
                if (me.Expression is ParameterExpression pe)
                {
                    if (entityQueryModelVisitor.QueryCompilationContext.QuerySourceMapping.ContainsMapping(queryModel.MainFromClause))
                    {
                        if (QuerySourcesRequiringMaterialization.GetValue(entityQueryModelVisitor.QueryCompilationContext) is ISet<IQuerySource> sources && sources != null)
                        {
                            var source = sources.FirstOrDefault(s => s.ItemType == pe.Type && s.ItemName == pe.Name);
                            if (source != null)
                            {
                                var exp = entityQueryModelVisitor.QueryCompilationContext.QuerySourceMapping.GetExpression(source);
                                if (exp != null)
                                {
                                    var meexp = Expression.MakeMemberAccess(exp, me.Member);
                                    var test = Expression.Call(ExtensionsDbFunctionsExtensions.ValueFromOpenJsonMethod.MakeGenericMethod(typeof(string))
                                        , valueFromOpenJsonOperator.ParseInfo.ParsedExpression.Arguments[0]
                                        , meexp
                                        , valueFromOpenJsonOperator.Path);
                                    return test;
                                }
                            }
                        }
                    }
                }
            }

            return entityQueryModelVisitor.Expression;
        }
    }
}
