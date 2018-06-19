using EFCore.Extensions.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System;

namespace Microsoft.EntityFrameworkCore
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static void UseExtensions(this DbContextOptionsBuilder optionsBuilder, Action<ExtensionsDbContextOptionsBuilder> configure)
        {
            (configure ?? throw new ArgumentNullException(nameof(configure)))(new ExtensionsDbContextOptionsBuilder(optionsBuilder));
            optionsBuilder.ReplaceService<INodeTypeProviderFactory, ExtensionsMethodInfoBasedNodeTypeRegistryFactory>();
            //optionsBuilder.ReplaceService<IResultOperatorHandler, ExtensionsResultOperatorHandler>();
            //optionsBuilder.ReplaceService<IQueryAnnotationExtractor, ExtensionsQueryAnnotationExtractor>();
        }

        public static void UseExtensions<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, Action<ExtensionsDbContextOptionsBuilder<TContext>> configure)
            where TContext : DbContext
        {
            (configure ?? throw new ArgumentNullException(nameof(configure)))(new ExtensionsDbContextOptionsBuilder<TContext>(optionsBuilder));
            optionsBuilder.ReplaceService<INodeTypeProviderFactory, ExtensionsMethodInfoBasedNodeTypeRegistryFactory>();
            //optionsBuilder.ReplaceService<IResultOperatorHandler, ExtensionsResultOperatorHandler>();
            //optionsBuilder.ReplaceService<IQueryAnnotationExtractor, ExtensionsQueryAnnotationExtractor>();
        }
    }

    public class ExtensionsDbContextOptionsBuilder
    {
        public ExtensionsDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
        {
            OptionsBuilder = optionsBuilder;
        }

        public DbContextOptionsBuilder OptionsBuilder { get; }
    }

    public class ExtensionsDbContextOptionsBuilder<TContext>
        where TContext : DbContext
    {
        public ExtensionsDbContextOptionsBuilder(DbContextOptionsBuilder<TContext> optionsBuilder)
        {
            OptionsBuilder = optionsBuilder;
        }

        public DbContextOptionsBuilder<TContext> OptionsBuilder { get; }
    }
}
