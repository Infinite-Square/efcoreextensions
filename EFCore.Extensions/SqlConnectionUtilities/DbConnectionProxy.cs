using EFCore.Extensions.SqlCommandCatching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EFCore.Extensions.SqlConnectionUtilities
{
    public interface ICommandExecutor
    {
        int ExecuteNonQuery(DbCommand command);
        object ExecuteScalar(DbCommand command);
        DbDataReader ExecuteDbDataReader(DbCommand command, CommandBehavior behavior);
        Task<int> ExecuteNonQueryAsync(DbCommand command, CancellationToken cancellationToken);
        Task<object> ExecuteScalarAsync(DbCommand command, CancellationToken cancellationToken);
        Task<DbDataReader> ExecuteDbDataReaderAsync(DbCommand command, CommandBehavior behavior, CancellationToken cancellationToken);
    }

    public abstract class DbConnectionProxy : DbConnection
    {
        private ConnectionState _state;

        public DbConnectionProxy(string connectionString
            , ConnectionState state = ConnectionState.Closed)
        {
            ConnectionString = connectionString;
            _state = state;
        }

        public abstract DbConnection UnderlyingConnection { get; }
        protected abstract ICommandExecutor CommandExecutor { get; }

        public override string ConnectionString { get; set; }

        public override string Database => UnderlyingConnection?.Database ?? null;

        public override string DataSource => UnderlyingConnection?.DataSource ?? null;

        public override string ServerVersion => UnderlyingConnection?.ServerVersion ?? null;

        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName)
        {
            UnderlyingConnection?.ChangeDatabase(databaseName);
        }

        public override void Close()
        {
            UnderlyingConnection?.Close();
            _state = ConnectionState.Closed;
        }

        public override void Open()
        {
            UnderlyingConnection?.Open();
            _state = ConnectionState.Open;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return UnderlyingConnection?.BeginTransaction(isolationLevel)
                ?? new FakeDbTransactionProxy(this, isolationLevel);
        }

        protected override DbCommand CreateDbCommand()
        {
            return UnderlyingConnection?.CreateCommand()
                ?? new FakeDbCommand(this, CommandExecutor);
        }

        private class FakeDbTransactionProxy : DbTransaction
        {
            public FakeDbTransactionProxy(DbConnectionProxy connection, IsolationLevel isolationLevel = IsolationLevel.Unspecified)
            {
                DbConnection = connection;
                IsolationLevel = isolationLevel;
            }

            public override IsolationLevel IsolationLevel { get; }

            protected override DbConnection DbConnection { get; }

            public override void Commit()
            {
            }

            public override void Rollback()
            {
            }
        }

        private class FakeDbCommand : DbCommand
        {
            private readonly DbConnectionProxy _connection;
            private readonly ICommandExecutor _commandExecutor;

            private readonly FakeDbParameterCollection _parameters = new FakeDbParameterCollection();

            public FakeDbCommand(DbConnectionProxy connection, ICommandExecutor commandExecutor)
            {
                _connection = connection;
                _commandExecutor = commandExecutor;
            }

            public override string CommandText { get; set; }
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection DbConnection { get; set; }

            protected override DbParameterCollection DbParameterCollection => _parameters;

            protected override DbTransaction DbTransaction { get; set; }

            public override void Cancel()
            {
                throw new NotImplementedException();
            }

            public override int ExecuteNonQuery()
            {
                return _commandExecutor.ExecuteNonQuery(this);
            }

            public override object ExecuteScalar()
            {
                return _commandExecutor.ExecuteScalar(this);
            }

            public override void Prepare()
            {
            }

            protected override DbParameter CreateDbParameter()
            {
                var p = new FakeDbParameter();
                _parameters.Add(p);
                return p;
            }

            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            {
                return _commandExecutor.ExecuteDbDataReader(this, behavior);
            }

            public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
            {
                return _commandExecutor.ExecuteNonQueryAsync(this, cancellationToken);
            }

            public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
            {
                return _commandExecutor.ExecuteScalarAsync(this, cancellationToken);
            }

            protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
            {
                return _commandExecutor.ExecuteDbDataReaderAsync(this, behavior, cancellationToken);
            }
        }

        private class FakeDbParameter : DbParameter
        {
            public override DbType DbType { get; set; }
            public override ParameterDirection Direction { get; set; }
            public override bool IsNullable { get; set; }
            public override string ParameterName { get; set; }
            public override int Size { get; set; }
            public override string SourceColumn { get; set; }
            public override bool SourceColumnNullMapping { get; set; }
            public override object Value { get; set; }

            public override void ResetDbType()
            {
                throw new NotImplementedException();
            }
        }

        private class FakeDbParameterCollection : DbParameterCollection
        {
            private readonly List<DbParameter> _inner = new List<DbParameter>();
            private readonly object _synRoot = new object();

            public override int Count => _inner.Count;

            public override object SyncRoot => _synRoot;

            public override int Add(object value)
            {
                _inner.Add((DbParameter)value);
                return _inner.Count - 1;
            }

            public override void AddRange(Array values)
            {
                _inner.AddRange(values.OfType<DbParameter>());
            }

            public override void Clear()
            {
                _inner.Clear();
            }

            public override bool Contains(object value)
            {
                return _inner.Any(p => p.Value == value);
            }

            public override bool Contains(string value)
            {
                return _inner.Any(p => p.ParameterName == value);
            }

            public override void CopyTo(Array array, int index)
            {
                throw new NotImplementedException();
            }

            public override IEnumerator GetEnumerator()
            {
                return _inner.GetEnumerator();
            }

            public override int IndexOf(object value)
            {
                throw new NotImplementedException();
            }

            public override int IndexOf(string parameterName)
            {
                throw new NotImplementedException();
            }

            public override void Insert(int index, object value)
            {
                _inner.Insert(index, (DbParameter)value);
                throw new NotImplementedException();
            }

            public override void Remove(object value)
            {
                _inner.Remove((DbParameter)value);
            }

            public override void RemoveAt(int index)
            {
                _inner.RemoveAt(index);
            }

            public override void RemoveAt(string parameterName)
            {
                throw new NotImplementedException();
            }

            protected override DbParameter GetParameter(int index)
            {
                throw new NotImplementedException();
            }

            protected override DbParameter GetParameter(string parameterName)
            {
                throw new NotImplementedException();
            }

            protected override void SetParameter(int index, DbParameter value)
            {
                throw new NotImplementedException();
            }

            protected override void SetParameter(string parameterName, DbParameter value)
            {
                throw new NotImplementedException();
            }
        }
    }

    //public class DbConnectionProxyOld : DbConnection
    //{
    //    private readonly ISqlCommandCatcher _sqlCommandCatcher;
    //    private readonly Func<DbConnection> _factory;

    //    private ConnectionState _state;

    //    private DbConnection _trueConnection;
    //    private DbConnection _underlying => _sqlCommandCatcher.Catch
    //        ? null
    //        : (_trueConnection ?? (_trueConnection = _factory()));

    //    public DbConnectionProxyOld(ISqlCommandCatcher sqlCommandCatcher
    //        , string connectionString
    //        , Func<DbConnection> factory
    //        , ConnectionState state = ConnectionState.Closed)
    //    {
    //        ConnectionString = connectionString;
    //        _state = state;
    //        _sqlCommandCatcher = sqlCommandCatcher;
    //        _factory = factory;
    //    }

    //    public override string ConnectionString { get; set; }

    //    public override string Database => _underlying?.Database ?? null;

    //    public override string DataSource => _underlying?.DataSource ?? null;

    //    public override string ServerVersion => _underlying?.ServerVersion ?? null;

    //    public override ConnectionState State => _state;

    //    public override void ChangeDatabase(string databaseName)
    //    {
    //        _underlying?.ChangeDatabase(databaseName);
    //    }

    //    public override void Close()
    //    {
    //        _underlying?.Close();
    //        _state = ConnectionState.Closed;
    //    }

    //    public override void Open()
    //    {
    //        _underlying?.Open();
    //        _state = ConnectionState.Open;
    //    }

    //    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    //    {
    //        return _underlying?.BeginTransaction(isolationLevel)
    //            ?? new FakeDbTransactionProxy(this, isolationLevel);
    //    }

    //    protected override DbCommand CreateDbCommand()
    //    {
    //        return _underlying?.CreateCommand() 
    //            ?? new FakeDbCommand(this, new FakeCommandExecutor(_sqlCommandCatcher));
    //    }

    //    private class FakeDbTransactionProxy : DbTransaction
    //    {
    //        public FakeDbTransactionProxy(DbConnectionProxy connection, IsolationLevel isolationLevel = IsolationLevel.Unspecified)
    //        {
    //            DbConnection = connection;
    //            IsolationLevel = isolationLevel;
    //        }

    //        public override IsolationLevel IsolationLevel { get; }

    //        protected override DbConnection DbConnection { get; }

    //        public override void Commit()
    //        {
    //        }

    //        public override void Rollback()
    //        {
    //        }
    //    }

    //    private class FakeDbCommand : DbCommand
    //    {
    //        private readonly DbConnectionProxy _connection;
    //        private readonly ICommandExecutor _commandExecutor;

    //        private readonly FakeDbParameterCollection _parameters = new FakeDbParameterCollection();

    //        public FakeDbCommand(DbConnectionProxy connection, ICommandExecutor commandExecutor)
    //        {
    //            _connection = connection;
    //            _commandExecutor = commandExecutor;
    //        }

    //        public override string CommandText { get; set; }
    //        public override int CommandTimeout { get; set; }
    //        public override CommandType CommandType { get; set; }
    //        public override bool DesignTimeVisible { get; set; }
    //        public override UpdateRowSource UpdatedRowSource { get; set; }
    //        protected override DbConnection DbConnection { get; set; }

    //        protected override DbParameterCollection DbParameterCollection => _parameters;

    //        protected override DbTransaction DbTransaction { get; set; }

    //        public override void Cancel()
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public override int ExecuteNonQuery()
    //        {
    //            return _commandExecutor.ExecuteNonQuery(this);
    //        }

    //        public override object ExecuteScalar()
    //        {
    //            return _commandExecutor.ExecuteScalar(this);
    //        }

    //        public override void Prepare()
    //        {
    //        }

    //        protected override DbParameter CreateDbParameter()
    //        {
    //            var p = new FakeDbParameter();
    //            _parameters.Add(p);
    //            return p;
    //        }

    //        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    //        {
    //            return _commandExecutor.ExecuteDbDataReader(this, behavior);
    //        }

    //        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    //        {
    //            return _commandExecutor.ExecuteNonQueryAsync(this, cancellationToken);
    //        }

    //        public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
    //        {
    //            return _commandExecutor.ExecuteScalarAsync(this, cancellationToken);
    //        }

    //        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    //        {
    //            return _commandExecutor.ExecuteDbDataReaderAsync(this, behavior, cancellationToken);
    //        }
    //    }

    //    private class FakeDbParameter : DbParameter
    //    {
    //        public override DbType DbType { get; set; }
    //        public override ParameterDirection Direction { get; set; }
    //        public override bool IsNullable { get; set; }
    //        public override string ParameterName { get; set; }
    //        public override int Size { get; set; }
    //        public override string SourceColumn { get; set; }
    //        public override bool SourceColumnNullMapping { get; set; }
    //        public override object Value { get; set; }

    //        public override void ResetDbType()
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    private class FakeDbParameterCollection : DbParameterCollection
    //    {
    //        private readonly List<DbParameter> _inner = new List<DbParameter>();
    //        private readonly object _synRoot = new object();

    //        public override int Count => _inner.Count;

    //        public override object SyncRoot => _synRoot;

    //        public override int Add(object value)
    //        {
    //            _inner.Add((DbParameter)value);
    //            return _inner.Count - 1;
    //        }

    //        public override void AddRange(Array values)
    //        {
    //            _inner.AddRange(values.OfType<DbParameter>());
    //        }

    //        public override void Clear()
    //        {
    //            _inner.Clear();
    //        }

    //        public override bool Contains(object value)
    //        {
    //            return _inner.Any(p => p.Value == value);
    //        }

    //        public override bool Contains(string value)
    //        {
    //            return _inner.Any(p => p.ParameterName == value);
    //        }

    //        public override void CopyTo(Array array, int index)
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public override IEnumerator GetEnumerator()
    //        {
    //            return _inner.GetEnumerator();
    //        }

    //        public override int IndexOf(object value)
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public override int IndexOf(string parameterName)
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public override void Insert(int index, object value)
    //        {
    //            _inner.Insert(index, (DbParameter)value);
    //            throw new NotImplementedException();
    //        }

    //        public override void Remove(object value)
    //        {
    //            _inner.Remove((DbParameter)value);
    //        }

    //        public override void RemoveAt(int index)
    //        {
    //            _inner.RemoveAt(index);
    //        }

    //        public override void RemoveAt(string parameterName)
    //        {
    //            throw new NotImplementedException();
    //        }

    //        protected override DbParameter GetParameter(int index)
    //        {
    //            throw new NotImplementedException();
    //        }

    //        protected override DbParameter GetParameter(string parameterName)
    //        {
    //            throw new NotImplementedException();
    //        }

    //        protected override void SetParameter(int index, DbParameter value)
    //        {
    //            throw new NotImplementedException();
    //        }

    //        protected override void SetParameter(string parameterName, DbParameter value)
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }
    //}
}
