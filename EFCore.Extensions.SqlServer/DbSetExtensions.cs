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
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore
{
    public static class DbSetExtensions
    {
        public static async Task BulkInsertAsync<T>(this DbSet<T> self, IEnumerable<T> entities)
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

            var data = new DataTable();
            var properties = entityType.GetProperties()
                .Select(property =>
                {
                    var pi = property.PropertyInfo;
                    var pr = property.Relational();
                    if (property.Name != "Id")
                        data.Columns.Add(pr.ColumnName, pi.PropertyType);
                    var getter = property.GetGetter();
                    return (property, getter, columnName: pr.ColumnName);
                }).ToList();

            foreach (var entity in entities)
            {
                var row = data.NewRow();
                foreach (var (property, getter, columnName) in properties)
                    if (property.Name != "Id")
                        row[columnName] = getter.GetClrValue(entity);
            }

            var connectionState = connection.State;

            using (var bulk = new SqlBulkCopy(connection is SqlConnection sqlConnection
                ? sqlConnection
                : throw new NotImplementedException()))
            {
                bulk.DestinationTableName = schema == null ? $"[{tableName}]" : $"[{schema}].[{tableName}]";

                if (connectionState == ConnectionState.Closed)
                    await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    await bulk.WriteToServerAsync(data);
                    transaction.Commit();
                }

                if (connectionState == ConnectionState.Closed)
                    connection.Close();
            }
        }
    }
}
