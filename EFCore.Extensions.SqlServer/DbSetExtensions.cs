using EFCore.Extensions.SqlConnectionUtilities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore
{
    public static class DbSetExtensions
    {
        public static async Task BulkInsertAsync<T>(this DbSet<T> self, IEnumerable<T> entities, CancellationToken cancellationToken)
            where T : class
        {
            var stateManager = ((IInfrastructure<IServiceProvider>)self).Instance.GetRequiredService<IStateManager>();
            var context = stateManager.Context;
            var connection = context.Database.GetDbConnection();
            if (connection is DbConnectionProxy proxy)
                connection = proxy.UnderlyingConnection;

            var entityType = context.Model.FindEntityType(typeof(T));
            var relational = entityType.Relational();
            var schema = relational.Schema ?? "dbo";
            var tableName = relational.TableName;

            var pk = entityType.FindPrimaryKey();
            var data = new DataTable();

            var entityProperties = entityType.GetProperties();

            if (pk.Properties.Count == 1)
                entityProperties = entityProperties
                .Where(p => !pk.Properties.Contains(p));

            var properties =
                entityProperties
                .Select(property =>
                {
                    var pi = property.PropertyInfo;
                    var pr = property.Relational();
                    var nullable = Nullable.GetUnderlyingType(pi.PropertyType);
                    var piType = nullable ?? pi.PropertyType;
                    var column = data.Columns.Add(pr.ColumnName, piType);
                    column.AllowDBNull = false;
                    if (nullable != null || property.PropertyInfo.PropertyType == typeof(string))
                    {
                        column.AllowDBNull = true;
                    }
                    var getter = new EntityClrPropertyGetter<T>(stateManager, property);
                    return (columnName: pr.ColumnName, getter);
                }).ToList();

            foreach (var entity in entities)
            {
                var row = data.NewRow();
                foreach (var (columnName, getter) in properties)
                    row[columnName] = getter.GetClrValue(entity) ?? DBNull.Value;
                data.Rows.Add(row);
            }

            var connectionState = connection.State;

            using (var bulk = new SqlBulkCopy(connection is SqlConnection sqlConnection
                ? sqlConnection
                : throw new NotImplementedException()))
            {
                foreach (var column in data.Columns)
                {
                    bulk.ColumnMappings.Add(((DataColumn)column).ColumnName, ((DataColumn)column).ColumnName);
                }

                bulk.DestinationTableName = schema == null ? $"[{tableName}]" : $"[{schema}].[{tableName}]";

                if (connectionState == ConnectionState.Closed)
                    await connection.OpenAsync(cancellationToken);

                await bulk.WriteToServerAsync(data);
                bulk.Close();
            }
        }

        private class EntityClrPropertyGetter<T> : IClrPropertyGetter
            where T : class
        {
            private readonly Func<IProperty, IEntityType, ValueGenerator> _factory;
            private readonly IStateManager _stateManager;
            private readonly IProperty _property;
            private readonly IEntityType _entityType;

            private readonly IClrPropertyGetter _internalGetter;

            public EntityClrPropertyGetter(IStateManager stateManager, IProperty property)
            {
                _stateManager = stateManager;
                _property = property;
                _entityType = property.DeclaringEntityType;
                _internalGetter = property.GetGetter();

                if (property.RequiresValueGenerator())
                {
                    _factory = property.GetValueGeneratorFactory();
                }
            }

            public object GetClrValue(object instance)
            {
                if (!(instance is T entity))
                    throw new ArgumentException($"Not an instance of {typeof(T)}", nameof(instance));

                if (_factory != null)
                {
                    var entry = new EntityEntry(_stateManager.GetOrCreateEntry(entity));
                    return _factory(_property, _entityType).Next(entry);
                }
                else
                {
                    return _internalGetter.GetClrValue(instance);
                }
            }

            public bool HasDefaultValue(object instance)
            {
                return _internalGetter.HasDefaultValue(instance);
            }
        }
    }
}
