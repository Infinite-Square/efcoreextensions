using System;
using System.Collections.Generic;

namespace EFCore.Extensions.SqlCommandCatching
{
    public interface ISqlCommandCatchingState
    {
        bool Enabled { get; }
    }

    public interface ISqlCommandCatcher
    {
        ISqlCommandCatchingScope EnableCatching();
    }

    public interface ISqlCommandCatchingStore
    {
        void Append(DbCommandInfo command);
    }

    public interface ISqlCommandCatchingScope : IDisposable
    {
        IEnumerable<DbCommandInfo> Commands { get; }
    }

    public class SqlCommandCatcher : ISqlCommandCatcher, ISqlCommandCatchingStore, ISqlCommandCatchingState
    {
        private readonly List<SqlCommandCatchingScope> _activeScopes = new List<SqlCommandCatchingScope>();

        public bool Enabled => _activeScopes.Count > 0;

        public void Append(DbCommandInfo command)
        {
            _activeScopes.ForEach(s => s.Add(command));
        }

        public ISqlCommandCatchingScope EnableCatching()
        {
            var scope = new SqlCommandCatchingScope(this);
            _activeScopes.Add(scope);
            return scope;
        }

        public void Release(SqlCommandCatchingScope scope)
        {
            _activeScopes.Remove(scope);
        }
    }

    public class SqlCommandCatchingScope : ISqlCommandCatchingScope
    {
        private readonly List<DbCommandInfo> _inner = new List<DbCommandInfo>();
        private readonly SqlCommandCatcher _catcher;

        public SqlCommandCatchingScope(SqlCommandCatcher catcher)
        {
            _catcher = catcher;
        }

        IEnumerable<DbCommandInfo> ISqlCommandCatchingScope.Commands => _inner;

        public void Add(DbCommandInfo command)
        {
            _inner.Add(command);
        }

        void IDisposable.Dispose()
        {
            _catcher.Release(this);
        }
    }
}
