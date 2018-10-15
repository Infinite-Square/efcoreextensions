using System.Data.Common;
using EFCore.Extensions.SqlCommandCatching;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.Extensions.SqlServer.Storage.Internal
{
    public class ExtensionsSqlServerConnection : SqlServerConnection
    {
        private readonly ISqlCommandCatchingStore _catchingStore;
        private readonly ISqlCommandCatchingState _catchingState;

        public ExtensionsSqlServerConnection(RelationalConnectionDependencies dependencies
            , ISqlCommandCatchingStore catchingStore
            , ISqlCommandCatchingState catchingState) 
            : base(dependencies)
        {
            _catchingStore = catchingStore;
            _catchingState = catchingState;
        }

        protected override DbConnection CreateDbConnection()
        {
            return new CommandCatchingDbConnectionProxy(ConnectionString, _catchingState , _catchingStore, () => base.CreateDbConnection());
        }
    }
}
