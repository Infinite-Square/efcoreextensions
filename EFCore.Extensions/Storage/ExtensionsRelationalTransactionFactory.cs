using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.Extensions.Storage
{
    public class ExtensionsRelationalTransactionFactory : RelationalTransactionFactory
    {
        public ExtensionsRelationalTransactionFactory(RelationalTransactionFactoryDependencies dependencies) : base(dependencies)
        {
        }

        public override RelationalTransaction Create(IRelationalConnection connection, DbTransaction transaction, IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger, bool transactionOwned)
        {
            return connection is IExtensionsRelationalConnection erc
                ? base.Create(erc.PrepareTransaction() ?? connection, transaction, logger, transactionOwned)
                : base.Create(connection, transaction, logger, transactionOwned);
        }
    }
}
