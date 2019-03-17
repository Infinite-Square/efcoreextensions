using EFCore.Extensions.SqlConnectionUtilities;
using System;
using System.Data.Common;

namespace EFCore.Extensions.SqlCommandCatching
{
    public class CommandCatchingDbConnectionProxy : DbConnectionProxy
    {
        private readonly ISqlCommandCatchingState _catchingState;
        private readonly CatchingCommandExecutor _executor;
        private readonly Func<DbConnection> _factory;


        private DbConnection _effectiveConnection;
        public override DbConnection UnderlyingConnection => _catchingState.Enabled
            ? null
            : (_effectiveConnection ?? (_effectiveConnection = _factory()));

        protected override ICommandExecutor CommandExecutor => _executor;

        public CommandCatchingDbConnectionProxy(string connectionString
            , ISqlCommandCatchingState catchingState
            , ISqlCommandCatchingStore catchingStore
            , Func<DbConnection> factory)
            : base(connectionString)
        {
            _catchingState = catchingState;
            _executor = new CatchingCommandExecutor(catchingStore);
            _factory = factory;
        }
    }
}
