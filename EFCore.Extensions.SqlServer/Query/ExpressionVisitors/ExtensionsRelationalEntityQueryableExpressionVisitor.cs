using EFCore.Extensions.Query.ResultOperators.Internal;
using EFCore.Extensions.SqlServer.Query.Expressions;
using EFCore.Extensions.SqlServer.Query.ResultOperators.Internal;
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
        private readonly ISqlTranslatingExpressionVisitorFactory _sqlTranslatingExpressionVisitorFactory;
        private readonly IQueryModelGenerator _queryModelGenerator;

        private new RelationalQueryModelVisitor QueryModelVisitor => (RelationalQueryModelVisitor)base.QueryModelVisitor;

        public SqlServerExtensionsRelationalEntityQueryableExpressionVisitor(RelationalEntityQueryableExpressionVisitorDependencies dependencies
            , RelationalQueryModelVisitor queryModelVisitor
            , IQuerySource querySource
            , ISqlTranslatingExpressionVisitorFactory sqlTranslatingExpressionVisitorFactory
            , IQueryModelGenerator queryModelGenerator)
            : base(dependencies, queryModelVisitor, querySource)
        {
            _model = (dependencies ?? throw new ArgumentNullException(nameof(dependencies))).Model;
            _selectExpressionFactory = dependencies.SelectExpressionFactory;
            _materializerFactory = dependencies.MaterializerFactory;
            _shaperCommandContextFactory = dependencies.ShaperCommandContextFactory;
            _querySource = querySource;
            _sqlTranslatingExpressionVisitorFactory = sqlTranslatingExpressionVisitorFactory;
            _queryModelGenerator = queryModelGenerator;
        }

        protected override Expression VisitEntityQueryable(Type elementType)
        {
            if (elementType == null)
                throw new ArgumentNullException(nameof(elementType));

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

                    ////QueryModelVisitor.VisitResultOperator(valueFromOpenJsonAnnotation, valueFromOpenJsonAnnotation.QueryModel, 0);
                    ////var subQueryInjector = new CollectionNavigationSubqueryInjector(QueryModelVisitor, true);
                    ////var json = subQueryInjector.Visit(me);
                    //var ff = new NavigationRewritingExpressionVisitor(QueryModelVisitor, true);
                    //var json = ff.Visit(me);

                    //ff.Rewrite(valueFromOpenJsonAnnotation.QueryModel, null);
                    //ff.InjectSubqueryToCollectionsInProjection(valueFromOpenJsonAnnotation.QueryModel);
                    //ff.Rewrite(valueFromOpenJsonAnnotation.QueryModel, null);
                    //var _modelExpressionApplyingExpressionVisitor = new ModelExpressionApplyingExpressionVisitor(relationalQueryCompilationContext, _queryModelGenerator, QueryModelVisitor);
                    //_modelExpressionApplyingExpressionVisitor.ApplyModelExpressions(valueFromOpenJsonAnnotation.QueryModel);
                    //ff.Rewrite(valueFromOpenJsonAnnotation.QueryModel, null);

                    //var ddjson = _sqlTranslatingExpressionVisitorFactory.Create(QueryModelVisitor, selectExpression).Visit(json);

                    var expr = new ValueFromOpenJsonExpression(_querySource,
                            valueFromOpenJsonAnnotation.Json,//json,
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

                    valueFromOpenJsonAnnotation.Context.SelectExpression = selectExpression;
                    valueFromOpenJsonAnnotation.Context.SqlTranslatingExpressionVisitorFactory = _sqlTranslatingExpressionVisitorFactory;

                    QueryModelVisitor.VisitResultOperator(valueFromOpenJsonAnnotation, valueFromOpenJsonAnnotation.QueryModel, 0);

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

                    DiscriminateProjectionQuery(entityType, selectExpression, _querySource);

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

            var forSystemTimeAsOfAnnotation
                = relationalQueryCompilationContext
                .QueryAnnotations
                .OfType<ForSystemTimeAsOfResultOperator>()
                .LastOrDefault(a => a.QuerySource == _querySource);

            if (forSystemTimeAsOfAnnotation != null)
            {
                var entityType = relationalQueryCompilationContext.FindEntityType(_querySource)
                             ?? _model.FindEntityType(elementType);

                var selectExpression = _selectExpressionFactory.Create(relationalQueryCompilationContext);

                QueryModelVisitor.AddQuery(_querySource, selectExpression);

                var tableName = entityType.Relational().TableName;// + " FOR SYSTEM_TIME";

                var tableAlias
                    = relationalQueryCompilationContext.CreateUniqueTableAlias(
                        _querySource.HasGeneratedItemName()
                            ? tableName[0].ToString().ToLowerInvariant()
                            : (_querySource as GroupJoinClause)?.JoinClause.ItemName
                              ?? _querySource.ItemName);

                Func<IQuerySqlGenerator> querySqlGeneratorFunc = selectExpression.CreateDefaultQuerySqlGenerator;

                selectExpression.AddTable(
                    new ForSystemTimeAsOfTableExpression(
                        tableName,
                        entityType.Relational().Schema,
                        tableAlias,
                        _querySource,
                        forSystemTimeAsOfAnnotation.DateTime));

                var shaper = CreateShaper(elementType, entityType, selectExpression);

                DiscriminateProjectionQuery(entityType, selectExpression, _querySource);

                return Expression.Call(
                    QueryModelVisitor.QueryCompilationContext.QueryMethodProvider
                        .ShapedQueryMethod
                        .MakeGenericMethod(shaper.Type),
                    EntityQueryModelVisitor.QueryContextParameter,
                    Expression.Constant(_shaperCommandContextFactory.Create(querySqlGeneratorFunc)),
                    Expression.Constant(shaper));
            }

            return base.VisitEntityQueryable(elementType);
        }

        private static Expression GenerateDiscriminatorExpression(
            IEntityType entityType, SelectExpression selectExpression, IQuerySource querySource)
        {
            var concreteEntityTypes
                = entityType.GetConcreteTypesInHierarchy().ToList();

            if (concreteEntityTypes.Count == 1
                && concreteEntityTypes[0].RootType() == concreteEntityTypes[0])
            {
                return null;
            }

            var discriminatorColumn
                = selectExpression.BindProperty(
                    concreteEntityTypes[0].Relational().DiscriminatorProperty,
                    querySource);

            var firstDiscriminatorValue
                = Expression.Constant(
                    concreteEntityTypes[0].Relational().DiscriminatorValue,
                    discriminatorColumn.Type);

            var discriminatorPredicate
                = Expression.Equal(discriminatorColumn, firstDiscriminatorValue);

            if (concreteEntityTypes.Count > 1)
            {
                discriminatorPredicate
                    = concreteEntityTypes
                        .Skip(1)
                        .Select(
                            concreteEntityType
                                => Expression.Constant(
                                    concreteEntityType.Relational().DiscriminatorValue,
                                    discriminatorColumn.Type))
                        .Aggregate(
                            discriminatorPredicate, (current, discriminatorValue) =>
                                Expression.OrElse(
                                    Expression.Equal(discriminatorColumn, discriminatorValue),
                                    current));
            }

            return discriminatorPredicate;
        }

        private void DiscriminateProjectionQuery(
            IEntityType entityType, SelectExpression selectExpression, IQuerySource querySource)
        {
            Expression discriminatorPredicate;

            if (entityType.IsQueryType)
            {
                discriminatorPredicate = GenerateDiscriminatorExpression(entityType, selectExpression, querySource);
            }
            else
            {
                var sharedTypes = new HashSet<IEntityType>(
                    _model.GetEntityTypes()
                        .Where(e => !e.IsQueryType)
                        .Where(
                            et => et.Relational().TableName == entityType.Relational().TableName
                                  && et.Relational().Schema == entityType.Relational().Schema));

                var currentPath = new Stack<IEntityType>();
                currentPath.Push(entityType);

                var allPaths = new List<List<IEntityType>>();
                FindPaths(entityType.RootType(), sharedTypes, currentPath, allPaths);

                discriminatorPredicate = allPaths
                    .Select(
                        p => p.Select(
                                et => GenerateDiscriminatorExpression(et, selectExpression, querySource))
                            .Aggregate(
                                (Expression)null,
                                (result, current) => result != null
                                    ? current != null
                                        ? Expression.AndAlso(result, current)
                                        : result
                                    : current))
                    .Aggregate(
                        (Expression)null,
                        (result, current) => result != null
                            ? current != null
                                ? Expression.OrElse(result, current)
                                : result
                            : current);
            }

            if (discriminatorPredicate != null)
            {
                selectExpression.Predicate = new DiscriminatorPredicateExpression(discriminatorPredicate, querySource);
            }
        }

        private static void FindPaths(
            IEntityType entityType, ICollection<IEntityType> sharedTypes,
            Stack<IEntityType> currentPath, ICollection<List<IEntityType>> result)
        {
            var identifyingFks = entityType.FindForeignKeys(entityType.FindPrimaryKey().Properties)
                .Where(
                    fk => fk.PrincipalKey.IsPrimaryKey()
                          && fk.PrincipalEntityType != entityType
                          && sharedTypes.Contains(fk.PrincipalEntityType))
                .ToList();

            if (identifyingFks.Count == 0)
            {
                result.Add(new List<IEntityType>(currentPath));
                return;
            }

            foreach (var fk in identifyingFks)
            {
                currentPath.Push(fk.PrincipalEntityType);
                FindPaths(fk.PrincipalEntityType.RootType(), sharedTypes, currentPath, result);
                currentPath.Pop();
            }
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
