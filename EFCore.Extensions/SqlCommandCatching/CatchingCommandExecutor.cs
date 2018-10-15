using EFCore.Extensions.SqlConnectionUtilities;
using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace EFCore.Extensions.SqlCommandCatching
{
    public class CatchingCommandExecutor : ICommandExecutor
    {
        private readonly ISqlCommandCatchingStore _commandStore;

        public CatchingCommandExecutor(ISqlCommandCatchingStore commandStore)
        {
            _commandStore = commandStore;
        }

        private void OnCommandExecute(DbCommand command, DbCommandExecution execution, CommandBehavior? behavior)
        {
            var rawSql = command.CommandText;
            _commandStore.Append(new DbCommandInfo { Command = command, Execution = execution, Behavior = behavior });
        }

        public DbDataReader ExecuteDbDataReader(DbCommand command, CommandBehavior behavior)
        {
            OnCommandExecute(command, DbCommandExecution.ExecuteDbDataReader, behavior);
            return new FakeDbDataReader();
        }

        public Task<DbDataReader> ExecuteDbDataReaderAsync(DbCommand command, CommandBehavior behavior, CancellationToken cancellationToken)
        {
            OnCommandExecute(command, DbCommandExecution.ExecuteDbDataReaderAsync, behavior);
            return Task.FromResult<DbDataReader>(new FakeDbDataReader());
        }

        public int ExecuteNonQuery(DbCommand command)
        {
            OnCommandExecute(command, DbCommandExecution.ExecuteNonQuery, null);
            return 0;
        }

        public Task<int> ExecuteNonQueryAsync(DbCommand command, CancellationToken cancellationToken)
        {
            OnCommandExecute(command, DbCommandExecution.ExecuteNonQueryAsync, null);
            return Task.FromResult(0);
        }

        public object ExecuteScalar(DbCommand command)
        {
            OnCommandExecute(command, DbCommandExecution.ExecuteScalar, null);
            return "";
        }

        public Task<object> ExecuteScalarAsync(DbCommand command, CancellationToken cancellationToken)
        {
            OnCommandExecute(command, DbCommandExecution.ExecuteScalarAsync, null);
            return Task.FromResult<object>("");
        }

        private class FakeDbDataReader : DbDataReader
        {
            public override object this[int ordinal] => throw new NotImplementedException();

            public override object this[string name] => throw new NotImplementedException();

            public override int Depth => throw new NotImplementedException();

            public override int FieldCount => throw new NotImplementedException();

            public override bool HasRows => throw new NotImplementedException();

            public override bool IsClosed => throw new NotImplementedException();

            public override int RecordsAffected => 0;

            public override bool GetBoolean(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override byte GetByte(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
            {
                throw new NotImplementedException();
            }

            public override char GetChar(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
            {
                throw new NotImplementedException();
            }

            public override string GetDataTypeName(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override DateTime GetDateTime(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override decimal GetDecimal(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override double GetDouble(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override IEnumerator GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public override Type GetFieldType(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override float GetFloat(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override Guid GetGuid(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override short GetInt16(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override int GetInt32(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override long GetInt64(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override string GetName(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override int GetOrdinal(string name)
            {
                throw new NotImplementedException();
            }

            public override string GetString(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override object GetValue(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override int GetValues(object[] values)
            {
                throw new NotImplementedException();
            }

            public override bool IsDBNull(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override bool NextResult()
            {
                throw new NotImplementedException();
            }

            public override bool Read()
            {
                return false;
            }
        }
    }
}
