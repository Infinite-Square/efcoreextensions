using EFCore.Extensions.SqlServer.Query.ResultOperators.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;

namespace EFCore.Extensions.SqlServer.Query.Internal
{
    public class ExtensionsSqlServerQueryCompilationContextFactory : SqlServerQueryCompilationContextFactory
    {
        public ExtensionsSqlServerQueryCompilationContextFactory(QueryCompilationContextDependencies dependencies
            , RelationalQueryCompilationContextDependencies relationalDependencies)
            : base(dependencies, relationalDependencies)
        {
            relationalDependencies
                .NodeTypeProviderFactory
                .RegisterMethods(ForSystemTimeAsOfExpressionNode.SupportedMethods, typeof(ForSystemTimeAsOfExpressionNode));
        }
    }
}
