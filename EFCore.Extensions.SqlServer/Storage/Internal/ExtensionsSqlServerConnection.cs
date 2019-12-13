using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using EFCore.Extensions.SqlCommandCatching;
using EFCore.Extensions.SqlConnectionUtilities;
using EFCore.Extensions.Storage;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.Extensions.SqlServer.Storage.Internal
{
    public class ExtensionsSqlServerConnection : SqlServerConnection
        , IExtensionsRelationalConnection
    {
        private readonly RelationalConnectionDependencies _dependencies;
        private readonly ISqlCommandCatchingStore _catchingStore;
        private readonly ISqlCommandCatchingState _catchingState;

        public ExtensionsSqlServerConnection(RelationalConnectionDependencies dependencies
            , ISqlCommandCatchingStore catchingStore
            , ISqlCommandCatchingState catchingState) 
            : base(dependencies)
        {
            _dependencies = dependencies;
            _catchingStore = catchingStore;
            _catchingState = catchingState;
        }

        public IRelationalConnection PrepareTransaction()
        {
            return DbConnection is DbConnectionProxy dcp && dcp.UnderlyingConnection != null
                ? new TransactionConnection(_dependencies, dcp.UnderlyingConnection)
                //? new SqlServerConnection(new RelationalConnectionDependencies(_dependencies.ContextOptions
                //    , _dependencies.TransactionLogger
                //    , _dependencies.ConnectionLogger
                //    , _dependencies.ConnectionStringResolver
                //    , _dependencies.RelationalTransactionFactory))
                : null;
        }

        protected override DbConnection CreateDbConnection()
        {
            return new CommandCatchingDbConnectionProxy(ConnectionString, _catchingState , _catchingStore, () => base.CreateDbConnection());
        }

        private class TransactionConnection : SqlServerConnection
        {
            public TransactionConnection(RelationalConnectionDependencies dependencies
                , DbConnection dbConnection)
                : base(dependencies)
            {
                DbConnection = dbConnection;
            }

            public override DbConnection DbConnection { get; }
        }

        //public override DbConnection DbConnection => base.DbConnection;
    }
}
