using EFCore.Extensions.Infrastructure;
using EFCore.Extensions.Internal;
using EFCore.Extensions.Query;
using EFCore.Extensions.Query.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.EntityFrameworkCore
{
    public static class DbContextOptionsBuilderExtensions
    {
        private static readonly ServiceReplacementCollection _replacements = ServiceReplacement.Collection(s => s
            .Add<INodeTypeProviderFactory, ExtensionsMethodInfoBasedNodeTypeRegistryFactory>()
            //.Add<IResultOperatorHandler, ExtensionsResultOperatorHandler>()
            //.Add<IQueryAnnotationExtractor, ExtensionsQueryAnnotationExtractor>()
            .Add<IResultOperatorHandler, ExtensionsResultOperatorHandler>()
            .Add<IQueryAnnotationExtractor, ExtensionsQueryAnnotationExtractor>()
            .Add<IRelationalResultOperatorHandler, ExtensionsRelationalResultOperatorHandler>()
            .Add<IModelCustomizer, ExtensionsRelationalModelCustomizer>()
        );

        public static IServiceCollection AddEntityFrameworkExtensions(this IServiceCollection services)
        {
            _replacements.Apply(services);
            return services;
        }

        private static void UseExtensions(this DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsInternalServiceProviderConfigured())
                _replacements.Apply(optionsBuilder);
        }

        public static DbContextOptionsBuilder UseExtensions(this DbContextOptionsBuilder optionsBuilder, Action<ExtensionsDbContextOptionsBuilder> configure)
        {
            optionsBuilder.UseExtensions();
            (configure ?? throw new ArgumentNullException(nameof(configure)))(new ExtensionsDbContextOptionsBuilder(optionsBuilder));
            return optionsBuilder;
        }

        public static DbContextOptionsBuilder<TContext> UseExtensions<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, Action<ExtensionsDbContextOptionsBuilder<TContext>> configure)
            where TContext : DbContext
        {
            optionsBuilder.UseExtensions();
            (configure ?? throw new ArgumentNullException(nameof(configure)))(new ExtensionsDbContextOptionsBuilder<TContext>(optionsBuilder));
            return optionsBuilder;
        }

        public static bool IsInternalServiceProviderConfigured(this DbContextOptionsBuilder optionsBuilder)
        {
            return optionsBuilder.Options.FindExtension<CoreOptionsExtension>()?.InternalServiceProvider != null;
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
