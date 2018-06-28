using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using EFCore.Extensions.Query.ResultOperators.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace EFCore.Extensions.Query.Internal
{
    public class ExtensionsRelationalResultOperatorHandler : RelationalResultOperatorHandler
    {
        private sealed class HandlerContext
        {
            private readonly IResultOperatorHandler _resultOperatorHandler;
            private readonly ISqlTranslatingExpressionVisitorFactory _sqlTranslatingExpressionVisitorFactory;

            public HandlerContext(
                IResultOperatorHandler resultOperatorHandler,
                IModel model,
                ISqlTranslatingExpressionVisitorFactory sqlTranslatingExpressionVisitorFactory,
                ISelectExpressionFactory selectExpressionFactory,
                RelationalQueryModelVisitor queryModelVisitor,
                ResultOperatorBase resultOperator,
                QueryModel queryModel,
                SelectExpression selectExpression)
            {
                _resultOperatorHandler = resultOperatorHandler;
                _sqlTranslatingExpressionVisitorFactory = sqlTranslatingExpressionVisitorFactory;

                Model = model;
                SelectExpressionFactory = selectExpressionFactory;
                QueryModelVisitor = queryModelVisitor;
                ResultOperator = resultOperator;
                QueryModel = queryModel;
                SelectExpression = selectExpression;
            }

            public IModel Model { get; }
            public ISelectExpressionFactory SelectExpressionFactory { get; }
            public ResultOperatorBase ResultOperator { get; }
            public SelectExpression SelectExpression { get; }
            public QueryModel QueryModel { get; }
            public RelationalQueryModelVisitor QueryModelVisitor { get; }
            public Expression EvalOnServer => QueryModelVisitor.Expression;

            public Expression EvalOnClient(bool requiresClientResultOperator = true)
            {
                QueryModelVisitor.RequiresClientResultOperator = requiresClientResultOperator;

                return _resultOperatorHandler
                    .HandleResultOperator(QueryModelVisitor, ResultOperator, QueryModel);
            }

            public SqlTranslatingExpressionVisitor CreateSqlTranslatingVisitor()
                => _sqlTranslatingExpressionVisitorFactory.Create(QueryModelVisitor, SelectExpression);
        }

        private static readonly Dictionary<Type, Func<HandlerContext, Expression>>
            _resultHandlers = new Dictionary<Type, Func<HandlerContext, Expression>>
            {
                { typeof(ValueFromOpenJsonOperator), HandleValueFromOpenJson }
            };

        private readonly IModel _model;
        private readonly ISqlTranslatingExpressionVisitorFactory _sqlTranslatingExpressionVisitorFactory;
        private readonly ISelectExpressionFactory _selectExpressionFactory;
        private readonly IResultOperatorHandler _resultOperatorHandler;

        public ExtensionsRelationalResultOperatorHandler(IModel model
            , ISqlTranslatingExpressionVisitorFactory sqlTranslatingExpressionVisitorFactory
            , ISelectExpressionFactory selectExpressionFactory
            , IResultOperatorHandler resultOperatorHandler)
            : base(model, sqlTranslatingExpressionVisitorFactory, selectExpressionFactory, resultOperatorHandler)
        {
            _model = model;
            _sqlTranslatingExpressionVisitorFactory = sqlTranslatingExpressionVisitorFactory;
            _selectExpressionFactory = selectExpressionFactory;
            _resultOperatorHandler = resultOperatorHandler;
        }

        public override Expression HandleResultOperator(EntityQueryModelVisitor entityQueryModelVisitor, ResultOperatorBase resultOperator, QueryModel queryModel)
        {
            if (_resultHandlers.TryGetValue(resultOperator.GetType(), out var resultHandler))
            {
                var relationalQueryModelVisitor = (RelationalQueryModelVisitor)entityQueryModelVisitor;

                var selectExpression
                    = relationalQueryModelVisitor
                        .TryGetQuery(queryModel.MainFromClause);

                var handlerContext
                    = new HandlerContext(
                        _resultOperatorHandler,
                        _model,
                        _sqlTranslatingExpressionVisitorFactory,
                        _selectExpressionFactory,
                        relationalQueryModelVisitor,
                        resultOperator,
                        queryModel,
                        selectExpression);

                if (relationalQueryModelVisitor.RequiresClientEval
                    || relationalQueryModelVisitor.RequiresClientSelectMany
                    || relationalQueryModelVisitor.RequiresClientJoin
                    || relationalQueryModelVisitor.RequiresClientFilter
                    || relationalQueryModelVisitor.RequiresClientOrderBy
                    || relationalQueryModelVisitor.RequiresClientResultOperator
                    || relationalQueryModelVisitor.RequiresStreamingGroupResultOperator
                    || selectExpression == null)
                {
                    return handlerContext.EvalOnClient();
                }
                return resultHandler(handlerContext);
            }

            return base.HandleResultOperator(entityQueryModelVisitor, resultOperator, queryModel);
        }

        private static Expression HandleValueFromOpenJson(HandlerContext handlerContext)
        {
            var resultOperator = (ValueFromOpenJsonOperator)handlerContext.ResultOperator;
            return (Expression)_transformClientExpressionMethodInfo
                .MakeGenericMethod(typeof(IEnumerable<JsonResult<string>>))
                .Invoke(null, new object[] { handlerContext, false });
        }

        private static readonly MethodInfo _transformClientExpressionMethodInfo
            = typeof(ExtensionsRelationalResultOperatorHandler).GetTypeInfo()
                .GetDeclaredMethod(nameof(TransformClientExpression));

        private static Expression TransformClientExpression<TResult>(
            HandlerContext handlerContext, bool throwOnNullResult = false)
            => new ResultTransformingExpressionVisitor<TResult>(
                    handlerContext.QueryModelVisitor.QueryCompilationContext,
                    throwOnNullResult)
                .Visit(handlerContext.QueryModelVisitor.Expression);
    }
}
