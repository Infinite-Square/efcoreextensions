using EFCore.Extensions.SqlConnectionUtilities;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
            var properties =
                entityProperties
                .Where(p => !pk.Properties.Contains(p))
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
                    var getter = property.GetGetter();
                    return (property, getter, columnName: pr.ColumnName);
                }).ToList();

            foreach (var entity in entities)
            {
                var row = data.NewRow();
                foreach (var (property, getter, columnName) in properties)
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
    }
}
