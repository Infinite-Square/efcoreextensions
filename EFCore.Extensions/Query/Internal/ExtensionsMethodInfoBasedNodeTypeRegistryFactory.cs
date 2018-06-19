using EFCore.Extensions.Query.ResultOperators.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Remotion.Linq.Parsing.Structure;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace EFCore.Extensions.Query.Internal
{
    public class ExtensionsMethodInfoBasedNodeTypeRegistryFactory : DefaultMethodInfoBasedNodeTypeRegistryFactory
    {
        public override INodeTypeProvider Create()
        {
            return new NodeTypeProviderProxy(base.Create());
        }

        public override void RegisterMethods(IEnumerable<MethodInfo> methods, Type nodeType)
        {
            base.RegisterMethods(methods, nodeType);
        }

        private class NodeTypeProviderProxy : INodeTypeProvider
        {
            private readonly INodeTypeProvider _inner;

            public NodeTypeProviderProxy(INodeTypeProvider inner)
            {
                _inner = inner;
            }

            public Type GetNodeType(MethodInfo method)
            {
                if (method.Name == nameof(ExtensionsDbFunctionsExtensions.ValueFromOpenJson))
                {
                    return typeof(ValueFromOpenJsonExpressionNode);
                }

                return _inner.GetNodeType(method);
            }

            public bool IsRegistered(MethodInfo method)
            {
                return _inner.IsRegistered(method);
            }
        }
    }
}
