using EFCore.Extensions.Query.ResultOperators.Internal;
using EFCore.Extensions.SqlServer.Query.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace EFCore.Extensions.SqlServer.Query.ExpressionVisitors
{
    public class SqlServerExtensionsRelationalEntityQueryableExpressionVisitor : RelationalEntityQueryableExpressionVisitor
    {
        private readonly IModel _model;
        private readonly ISelectExpressionFactory _selectExpressionFactory;
        private readonly IMaterializerFactory _materializerFactory;
        private readonly IShaperCommandContextFactory _shaperCommandContextFactory;
        private readonly IQuerySource _querySource;
        private new RelationalQueryModelVisitor QueryModelVisitor => (RelationalQueryModelVisitor)base.QueryModelVisitor;

        public SqlServerExtensionsRelationalEntityQueryableExpressionVisitor(RelationalEntityQueryableExpressionVisitorDependencies dependencies
            , RelationalQueryModelVisitor queryModelVisitor
            , IQuerySource querySource)
            : base(dependencies, queryModelVisitor, querySource)
        {
            _model = (dependencies ?? throw new ArgumentNullException(nameof(dependencies))).Model;
            _selectExpressionFactory = dependencies.SelectExpressionFactory;
            _materializerFactory = dependencies.MaterializerFactory;
            _shaperCommandContextFactory = dependencies.ShaperCommandContextFactory;
            _querySource = querySource;
        }

        protected override Expression VisitEntityQueryable(Type elementType)
        {
            if (elementType == null) throw new ArgumentNullException(nameof(elementType));

            var relationalQueryCompilationContext = QueryModelVisitor.QueryCompilationContext;


            var valueFromOpenJsonAnnotation
                = relationalQueryCompilationContext
                    .QueryAnnotations
                    .OfType<ValueFromOpenJsonOperator>()
                    .LastOrDefault(a => a.QuerySource == _querySource);

            if (valueFromOpenJsonAnnotation != null)
            {
                if (valueFromOpenJsonAnnotation.Json.NodeType == ExpressionType.MemberAccess && valueFromOpenJsonAnnotation.Json is MemberExpression me)
                {
                    var entityType = relationalQueryCompilationContext.FindEntityType(_querySource)
                             ?? _model.FindEntityType(elementType);
                    var selectExpression = _selectExpressionFactory.Create(relationalQueryCompilationContext);
                    QueryModelVisitor.AddQuery(_querySource, selectExpression);
                    var name = entityType.Relational().TableName;
                    var tableAlias
                        = relationalQueryCompilationContext.CreateUniqueTableAlias(
                            _querySource.HasGeneratedItemName()
                                ? name[0].ToString().ToLowerInvariant()
                                : (_querySource as GroupJoinClause)?.JoinClause.ItemName
                                    ?? _querySource.ItemName);
                    Func<IQuerySqlGenerator> querySqlGeneratorFunc = selectExpression.CreateDefaultQuerySqlGenerator;

                    var memberEntityType = relationalQueryCompilationContext.Model.FindEntityType(me.Expression.Type);
                    var memberProperty = memberEntityType.FindProperty(me.Member as PropertyInfo);

                    var expr = new ValueFromOpenJsonExpression(_querySource,
                            valueFromOpenJsonAnnotation.Json,
                            valueFromOpenJsonAnnotation.Path,
                            tableAlias);
                    expr.PropertyMapping[expr.Json] = memberProperty;

                    //var jsonProperties = MemberAccessBindingExpressionVisitor.GetPropertyPath(expr.Json, relationalQueryCompilationContext, out var qsr);
                    //Console.WriteLine(jsonProperties);

                    //expr = (ValueFromOpenJsonExpression)Visit(expr);
                    //expr.PropertyMapping[expr.Json] = memberProperty;

                    //expr = (ValueFromOpenJsonExpression)QueryModelVisitor.ReplaceClauseReferences(expr);
                    //expr.PropertyMapping[expr.Json] = memberProperty;

                    selectExpression.AddTable(expr);

                    //var trimmedSql = valueFromOpenJsonAnnotation.Sql.TrimStart('\r', '\n', '\t', ' ');
                    var useQueryComposition = true;
                        //= trimmedSql.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase)
                        //  || trimmedSql.StartsWith("SELECT" + Environment.NewLine, StringComparison.OrdinalIgnoreCase)
                        //  || trimmedSql.StartsWith("SELECT\t", StringComparison.OrdinalIgnoreCase);
                    var requiresClientEval = !useQueryComposition;
                    if (!useQueryComposition)
                        if (relationalQueryCompilationContext.IsIncludeQuery)
                            throw new InvalidOperationException(
                                RelationalStrings.StoredProcedureIncludeNotSupported);
                    if (useQueryComposition
                        && valueFromOpenJsonAnnotation.QueryModel.IsIdentityQuery()
                        && !valueFromOpenJsonAnnotation.QueryModel.ResultOperators.Any()
                        && !relationalQueryCompilationContext.IsIncludeQuery)
                        useQueryComposition = false;

                    if (!useQueryComposition)
                    {
                        throw new NotImplementedException();
                        //QueryModelVisitor.RequiresClientEval = requiresClientEval;
                        //querySqlGeneratorFunc = ()
                        //    => selectExpression.CreateFromSqlQuerySqlGenerator(
                        //        fromSqlAnnotation.Sql,
                        //        fromSqlAnnotation.Arguments);
                    }

                    var shaper = CreateShaper(elementType, entityType, selectExpression);

                    return Expression.Call(
                        QueryModelVisitor.QueryCompilationContext.QueryMethodProvider // TODO: Don't use ShapedQuery when projecting
                            .ShapedQueryMethod
                            .MakeGenericMethod(shaper.Type),
                        EntityQueryModelVisitor.QueryContextParameter,
                        Expression.Constant(_shaperCommandContextFactory.Create(querySqlGeneratorFunc)),
                        Expression.Constant(shaper));

                    //var member = valueFromOpenJsonAnnotation.Arguments as MemberExpression;
                    //if (member.Type == typeof(string))
                    //{



                    //    var m = this.VisitMember(member);
                    //}
                }
            }

            return base.VisitEntityQueryable(elementType);
        }

        private static readonly MethodInfo _createEntityShaperMethodInfo
            = typeof(RelationalEntityQueryableExpressionVisitor).GetTypeInfo()
                .GetDeclaredMethod(nameof(CreateEntityShaper));

        //[UsedImplicitly]
        private static IShaper<TEntity> CreateEntityShaper<TEntity>(
            IQuerySource querySource,
            bool trackingQuery,
            IKey key,
            Func<MaterializationContext, object> materializer,
            Expression materializerExpression,
            Dictionary<Type, int[]> typeIndexMap,
            bool useQueryBuffer)
            where TEntity : class
            => !useQueryBuffer
                ? (IShaper<TEntity>)new UnbufferedEntityShaper<TEntity>(
                    querySource,
                    trackingQuery,
                    key,
                    materializer,
                    materializerExpression)
                : new BufferedEntityShaper<TEntity>(
                    querySource,
                    trackingQuery,
                    key,
                    materializer,
                    typeIndexMap);

        private Shaper CreateShaper(Type elementType, IEntityType entityType, SelectExpression selectExpression)
        {
            Shaper shaper;

            if (QueryModelVisitor.QueryCompilationContext
                    .QuerySourceRequiresMaterialization(_querySource)
                || QueryModelVisitor.RequiresClientEval)
            {
                var materializerExpression
                    = _materializerFactory
                        .CreateMaterializer(
                            entityType,
                            selectExpression,
                            (p, se) =>
                                se.AddToProjection(
                                    p,
                                    _querySource),
                            out var typeIndexMap);

                var materializer = materializerExpression.Compile();

                shaper
                    = (Shaper)_createEntityShaperMethodInfo.MakeGenericMethod(elementType)
                        .Invoke(
                            obj: null,
                            parameters: new object[]
                            {
                                _querySource,
                                QueryModelVisitor.QueryCompilationContext.IsTrackingQuery
                                && !entityType.IsQueryType,
                                entityType.FindPrimaryKey(),
                                materializer,
                                materializerExpression,
                                typeIndexMap,
                                QueryModelVisitor.QueryCompilationContext.IsQueryBufferRequired
                                && !entityType.IsQueryType
                            });
            }
            else
            {
                shaper = new ValueBufferShaper(_querySource);
            }

            return shaper;
        }

    }
}
