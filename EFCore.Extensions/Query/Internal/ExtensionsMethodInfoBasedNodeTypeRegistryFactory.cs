using EFCore.Extensions.Query.ResultOperators.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace EFCore.Extensions.Query.Internal
{
    public class ExtensionsMethodInfoBasedNodeTypeRegistryFactory : DefaultMethodInfoBasedNodeTypeRegistryFactory
    {
        public ExtensionsMethodInfoBasedNodeTypeRegistryFactory()
        {
            RegisterMethods(CountDistinctExpressionNode.GetSupportedMethods(), typeof(CountDistinctExpressionNode));
            RegisterMethods(ValueFromOpenJsonExpressionNode.GetSupportedMethods(), typeof(ValueFromOpenJsonExpressionNode));
        }
    }
}
