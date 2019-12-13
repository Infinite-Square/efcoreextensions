using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.Extensions.Storage
{
    public interface IExtensionsRelationalConnection : IRelationalConnection
    {
        IRelationalConnection PrepareTransaction();
    }
}
