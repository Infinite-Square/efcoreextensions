using EFCore.Extensions.Query.ResultOperators.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EFCore.Extensions.Query
{
    using JetBrains.Annotations;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Microsoft.EntityFrameworkCore.Query.Expressions;
    using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
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

        private static FieldInfo QuerySourceMappingLookup = typeof(QuerySourceMapping)
            .GetField("_lookup", BindingFlags.NonPublic | BindingFlags.Instance);

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
                    var mapping = entityQueryModelVisitor.QueryCompilationContext.QuerySourceMapping;
                    if (mapping.ContainsMapping(queryModel.MainFromClause))
                    {
                        if (QuerySourceMappingLookup.GetValue(mapping) is IDictionary<IQuerySource, Expression> lookup)
                        {
                            var source = lookup.Keys.FirstOrDefault(s => s.ItemType == pe.Type && s.ItemName == pe.Name);
                            source = source ?? lookup.Keys.SingleOrDefault(s => s.ItemType == pe.Type);
                            if (source != null)
                            {

                                var exp = mapping.GetExpression(source);
                                if (exp != null)
                                {
                                    if (exp.Type == typeof(ValueBuffer))
                                    {
                                        var entityType = entityQueryModelVisitor.QueryCompilationContext.Model.FindEntityType(pe.Type);
                                        if (entityType != null)
                                        {
                                            var prop = entityType.FindProperty(me.Member as PropertyInfo);
                                            if (prop != null)
                                            {
                                                var json = entityQueryModelVisitor.BindMemberToValueBuffer(me, exp);
                                                return Expression.Call(valueFromOpenJsonOperator.ParseInfo.ParsedExpression.Method
                                                    , valueFromOpenJsonOperator.ParseInfo.ParsedExpression.Arguments[0]
                                                    , json
                                                    , valueFromOpenJsonOperator.Path);
                                            }
                                        }
                                    }

                                    var meexp = Expression.MakeMemberAccess(exp, me.Member);
                                    return Expression.Call(valueFromOpenJsonOperator.ParseInfo.ParsedExpression.Method
                                        , valueFromOpenJsonOperator.ParseInfo.ParsedExpression.Arguments[0]
                                        , meexp
                                        , valueFromOpenJsonOperator.Path);
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
