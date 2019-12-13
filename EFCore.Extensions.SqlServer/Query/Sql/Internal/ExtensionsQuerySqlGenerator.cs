using EFCore.Extensions.SqlServer.Query.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EFCore.Extensions.SqlServer.Query.Sql.Internal
{
    public static class SharedTypeExtensions
    {
        public static Type UnwrapNullableType(this Type type) => Nullable.GetUnderlyingType(type) ?? type;

        public static bool IsInteger(this Type type)
        {
            type = type.UnwrapNullableType();

            return type == typeof(int)
                || type == typeof(long)
                || type == typeof(short)
                || type == typeof(byte)
                || type == typeof(uint)
                || type == typeof(ulong)
                || type == typeof(ushort)
                || type == typeof(sbyte)
                || type == typeof(char);
        }
    }

    public class ExtensionsQuerySqlGenerator : SqlServerQuerySqlGenerator
    {
        private readonly FieldInfo _typeMappingInfo = typeof(DefaultQuerySqlGenerator)
            .GetField(nameof(_typeMapping), BindingFlags.NonPublic | BindingFlags.Instance);
        private RelationalTypeMapping _typeMapping
        {
            get => (RelationalTypeMapping)_typeMappingInfo.GetValue(this);
            set => _typeMappingInfo.SetValue(this, value);
        }

        private readonly FieldInfo _parameterNameGeneratorInfo = typeof(DefaultQuerySqlGenerator)
            .GetField(nameof(_parameterNameGenerator), BindingFlags.NonPublic | BindingFlags.Instance);
        private ParameterNameGenerator _parameterNameGenerator
        {
            get => (ParameterNameGenerator)_parameterNameGeneratorInfo.GetValue(this);
            set => _parameterNameGeneratorInfo.SetValue(this, value);
        }

        private readonly List<SelectExpression> _selectExpressions = new List<SelectExpression>();
        //private readonly List<AliasExpression> _aliasExpressions = new List<AliasExpression>();
        //private readonly List<ParameterExpression> _parameterExpressions = new List<ParameterExpression>();
        private readonly List<TableExpression> _tableExpressions = new List<TableExpression>();
        //private readonly List<SubQueryExpression> _subQueryExpressions = new List<SubQueryExpression>();
        //private readonly List<QuerySourceReferenceExpression> _querySourceReferenceExpressions = new List<QuerySourceReferenceExpression>();
        //private readonly List<Expression> _expressions = new List<Expression>();

        public ExtensionsQuerySqlGenerator(QuerySqlGeneratorDependencies dependencies
            , SelectExpression selectExpression
            , bool rowNumberPagingEnabled)
            : base(dependencies, selectExpression, rowNumberPagingEnabled)
        {
        }

        public override Expression VisitSelect(SelectExpression selectExpression)
        {
            //_expressions.Add(selectExpression);
            _selectExpressions.Add(selectExpression);
            return base.VisitSelect(selectExpression);
        }

        //public override Expression VisitAlias(AliasExpression aliasExpression)
        //{
        //    _expressions.Add(aliasExpression);
        //    _aliasExpressions.Add(aliasExpression);
        //    return base.VisitAlias(aliasExpression);
        //}

        //protected override Expression VisitParameter(ParameterExpression parameterExpression)
        //{
        //    _expressions.Add(parameterExpression);
        //    _parameterExpressions.Add(parameterExpression);
        //    return base.VisitParameter(parameterExpression);
        //}

        public override Expression VisitTable(TableExpression tableExpression)
        {
            //_expressions.Add(tableExpression);
            _tableExpressions.Add(tableExpression);
            return base.VisitTable(tableExpression);
        }

        //protected override Expression VisitSubQuery(SubQueryExpression expression)
        //{
        //    _expressions.Add(expression);
        //    _subQueryExpressions.Add(expression);
        //    return base.VisitSubQuery(expression);
        //}

        //protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression)
        //{
        //    _querySourceReferenceExpressions.Add(expression);
        //    _expressions.Add(expression);
        //    return base.VisitQuerySourceReference(expression);
        //}

        public virtual Expression VisitValueFromOpenJson(ValueFromOpenJsonExpression expression)
        {
            //_expressions.Add(expression);
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            Sql.AppendLine("(");

            using (Sql.Indent())
            {
                GenerateValueFromOpenJson(expression, expression.Json, expression.Path, ParameterValues);
            }

            Sql.Append(") AS ")
                .Append(SqlGenerator.DelimitIdentifier(expression.Alias));

            return expression;
        }

        protected virtual void GenerateValueFromOpenJson(ValueFromOpenJsonExpression valueFromOpenJsonExpression,
            Expression json,
            Expression path,
            IReadOnlyDictionary<string, object> parameters)
        {
            //if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentNullException(nameof(sql));
            if (json == null)
                throw new ArgumentNullException(nameof(json));
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            string pathValue = null;
            if (path != null)
            {
                if (path.NodeType == ExpressionType.Constant && path is ConstantExpression ce)
                {
                    var value = ce.Value;
                    pathValue = GenerateSqlLiteral(value);
                }
            }

            string[] substitutions = null;

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (json.NodeType)
            {
                case ExpressionType.MemberAccess:
                {
                    var arg = (MemberExpression)json;

                    if (valueFromOpenJsonExpression.PropertyMapping.TryGetValue(json, out var prop))
                    {
                        var columnName = prop.Relational().ColumnName;
                        var querySource = valueFromOpenJsonExpression.QuerySource;
                        var select = SelectExpression;
                        var table = select.GetTableForQuerySource(querySource);

                        ColumnExpression column = null;
                        if (table != null)
                        {
                            while (table is SelectExpression selectTable)
                            {
                                var tmp = selectTable.GetTableForQuerySource(querySource);
                                if (tmp != null)
                                    table = tmp;
                            }

                            if (prop.DeclaringEntityType.ClrType != table.QuerySource.ItemType)
                            {
                                // we have to explore in predicate subquery
                                if (select.Predicate is BinaryExpression bpe)
                                {
                                    var left = bpe.Left;
                                    var right = bpe.Right;

                                    if (right is ExistsExpression rexists)
                                    {
                                        var subQuery = rexists.Subquery;
                                        var subTable = subQuery.GetTableForQuerySource(querySource);
                                        if (subTable != null && prop.DeclaringEntityType.ClrType == subTable.QuerySource.ItemType)
                                            table = subTable;
                                    }

                                }

                                if (select.Predicate is ExistsExpression rrexists)
                                {
                                    var subQuery = rrexists.Subquery;
                                    var subTable = subQuery.GetTableForQuerySource(querySource);
                                    if (subTable != null && prop.DeclaringEntityType.ClrType == subTable.QuerySource.ItemType)
                                        table = subTable;
                                }
                            }
                        }
                        else
                        {
                            foreach (var t in _tableExpressions.Reverse<TableExpression>())
                            {
                                if (t.QuerySource.ItemType == prop.DeclaringEntityType.ClrType)
                                {
                                    table = t;

                                    foreach (var s in _selectExpressions.Reverse<SelectExpression>())
                                    {
                                        if (s == select)
                                            continue;
                                        if (s.Tables.Contains(valueFromOpenJsonExpression))
                                            continue;
                                        if (s.Tables.Contains(t))
                                        {
                                            if (s.QuerySource is JoinClause joinClause && joinClause.ItemType != prop.DeclaringEntityType.ClrType)
                                                break;
                                            if (!string.IsNullOrWhiteSpace(s.Alias))
                                            {
                                                column = new ColumnExpression(columnName, prop, s);
                                                break;
                                            }
                                        }
                                    }

                                    break;
                                }
                            }


                            if (table == null)
                            {
                                foreach (var t in select.Tables/*.Reverse()*/)
                                {
                                    if (t is JoinExpressionBase join && join.TableExpression is SelectExpression joinSelect)
                                    {
                                        table = joinSelect.GetTableForQuerySource(querySource);
                                        if (table != null)
                                        {
                                            while (table is SelectExpression selectTable)
                                            {
                                                var tmp = selectTable.GetTableForQuerySource(querySource);
                                                if (tmp != null)
                                                    table = tmp;
                                            }
                                            if (prop.DeclaringEntityType.ClrType == table.QuerySource.ItemType)
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            if (t.QuerySource.ItemType != prop.DeclaringEntityType.ClrType)
                                                continue;
                                            var bind = select.BindProperty(prop, t.QuerySource);
                                            if (bind == null)
                                                continue;
                                            if (bind is ColumnExpression c)
                                            {
                                                column = c;
                                                break;
                                            }
                                            else
                                                throw new NotImplementedException();
                                        }
                                        catch
                                        {
                                            continue;
                                        }
                                    }
                                }
                            }

                            if (table == null && column == null)
                                throw new NotImplementedException();
                        }

                        if (prop.DeclaringEntityType.ClrType != table.QuerySource.ItemType)
                        {
                            foreach (var t in select.Tables/*.Reverse()*/)
                            {
                                if (t is JoinExpressionBase join && join.TableExpression is SelectExpression joinSelect)
                                {
                                    table = joinSelect.GetTableForQuerySource(querySource);
                                    if (table != null)
                                    {
                                        while (table is SelectExpression selectTable)
                                        {
                                            var tmp = selectTable.GetTableForQuerySource(querySource);
                                            if (tmp != null)
                                                table = tmp;
                                        }
                                        if (prop.DeclaringEntityType.ClrType == table.QuerySource.ItemType)
                                            break;
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        if (t.QuerySource.ItemType != prop.DeclaringEntityType.ClrType)
                                            continue;
                                        var bind = select.BindProperty(prop, t.QuerySource);
                                        if (bind == null)
                                            continue;
                                        if (bind is ColumnExpression c)
                                        {
                                            column = c;
                                            break;
                                        }
                                        else
                                            throw new NotImplementedException();
                                    }
                                    catch
                                    {
                                        continue;
                                    }
                                }
                            }
                        }

                        column = column ?? new ColumnExpression(columnName, prop, table);

                        var fsql = "SELECT [Key], [Value], [Type] FROM OPENJSON(";
                        Sql.Append(fsql);
                        VisitColumn(column);
                        Sql.Append($",{pathValue ?? "N'$'"})");
                        return;
                    }

                    var projection = SelectExpression.Projection
                        .OfType<ColumnExpression>()
                        .FirstOrDefault(cp => cp.Type == arg.Type
                            //&& cp.Table.Type == arg.Expression.Type
                            && (cp.Property.PropertyInfo == arg.Member || cp.Property.FieldInfo == arg.Member));
                    if (projection != null)
                        substitutions = new[] { $"[{projection.Table.Alias}].{SqlGenerator.DelimitIdentifier(projection.Name)}" };
                    break;
                }
                case ExpressionType.Parameter:
                {
                    var parameterExpression = (ParameterExpression)json;

                    if (parameters.TryGetValue(parameterExpression.Name, out var parameterValue))
                    {
                        var argumentValues = (object[])parameterValue;

                        substitutions = new string[argumentValues.Length];

                        Sql.AddCompositeParameter(
                            parameterExpression.Name,
                            builder =>
                            {
                                for (var i = 0; i < argumentValues.Length; i++)
                                {
                                    var parameterName = _parameterNameGenerator.GenerateNext();

                                    substitutions[i] = SqlGenerator.GenerateParameterName(parameterName);

                                    builder.AddParameter(
                                    parameterName,
                                    substitutions[i]);
                                }
                            });
                    }

                    break;
                }
                case ExpressionType.Constant:
                {
                    var constantExpression = (ConstantExpression)json;
                    var argumentValues = (object[])constantExpression.Value;

                    substitutions = new string[argumentValues.Length];

                    for (var i = 0; i < argumentValues.Length; i++)
                    {
                        var value = argumentValues[i];

                        if (value is DbParameter dbParameter)
                        {
                            if (string.IsNullOrEmpty(dbParameter.ParameterName))
                            {
                                dbParameter.ParameterName = SqlGenerator.GenerateParameterName(_parameterNameGenerator.GenerateNext());
                            }

                            substitutions[i] = dbParameter.ParameterName;

                            Sql.AddRawParameter(
                                dbParameter.ParameterName,
                                dbParameter);
                        }
                        else
                        {
                            substitutions[i] = GetTypeMapping(value).GenerateSqlLiteral(value);
                        }
                    }

                    break;
                }
                case ExpressionType.NewArrayInit:
                {
                    var newArrayExpression = (NewArrayExpression)json;

                    substitutions = new string[newArrayExpression.Expressions.Count];

                    for (var i = 0; i < newArrayExpression.Expressions.Count; i++)
                    {
                        var expression = newArrayExpression.Expressions[i].RemoveConvert();

                        // ReSharper disable once SwitchStatementMissingSomeCases
                        switch (expression.NodeType)
                        {
                            case ExpressionType.Constant:
                            {
                                var value = ((ConstantExpression)expression).Value;
                                substitutions[i]
                                    = GetTypeMapping(value).GenerateSqlLiteral(value);

                                break;
                            }
                            case ExpressionType.Parameter:
                            {
                                var parameter = (ParameterExpression)expression;

                                if (ParameterValues.ContainsKey(parameter.Name))
                                {
                                    substitutions[i] = SqlGenerator.GenerateParameterName(parameter.Name);

                                    Sql.AddParameter(
                                        parameter.Name,
                                        substitutions[i]);
                                }

                                break;
                            }
                        }
                    }

                    break;
                }
            }



            var sql = "SELECT [Key], [Value], [Type] FROM OPENJSON({0}, {1})";

            if (substitutions != null)
            {
                // ReSharper disable once CoVariantArrayConversion
                // InvariantCulture not needed since substitutions are all strings
                //sql = string.Format(sql, substitutions);
                sql = pathValue != null
                    ? string.Format(sql, substitutions[0], pathValue)
                    : string.Format(sql, substitutions[0], "N'$'");
            }

            Sql.AppendLines(sql);
        }

        private string GenerateSqlLiteral(object value)
        {
            var mapping = _typeMapping;
            var mappingClrType = mapping?.ClrType.UnwrapNullableType();

            if (mappingClrType != null
                && (value == null
                    || mappingClrType.IsInstanceOfType(value)
                    || value.GetType().IsInteger()
                    && (mappingClrType.IsInteger()
                        || mappingClrType.IsEnum)))
            {
                if (value?.GetType().IsInteger() == true
                    && mappingClrType.IsEnum)
                {
                    value = Enum.ToObject(mappingClrType, value);
                }
            }
            else
            {
                mapping = Dependencies.TypeMappingSource.GetMappingForValue(value);
            }

            //LogValueConversionWarning(mapping);

            return mapping.GenerateSqlLiteral(value);
        }

        private RelationalTypeMapping GetTypeMapping(object value)
        {
            return _typeMapping != null
                   && (value == null
                       || _typeMapping.ClrType.IsInstanceOfType(value))
                ? _typeMapping
                : Dependencies.TypeMappingSource.GetMappingForValue(value);
        }
    }
}
