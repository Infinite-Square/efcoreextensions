using System.Data;
using System.Data.Common;

namespace EFCore.Extensions.SqlCommandCatching
{
    public class DbCommandInfo
    {
        public DbCommand Command { get; set; }
        public DbCommandExecution Execution { get; set; }
        public CommandBehavior? Behavior { get; set; }
    }

    public enum DbCommandExecution
    {
        ExecuteDbDataReader,
        ExecuteDbDataReaderAsync,
        ExecuteNonQuery,
        ExecuteNonQueryAsync,
        ExecuteScalar,
        ExecuteScalarAsync
    }
}
