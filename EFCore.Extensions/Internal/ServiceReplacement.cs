using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EFCore.Extensions.Internal
{
    public abstract class ServiceReplacement
    {
        public abstract void ReplaceService(DbContextOptionsBuilder optionsBuilder);
        public abstract void ReplaceService(IServiceCollection services);

        //public static ServiceReplacement Create<TService, TImplementation>()
        //    where TImplementation : TService
        //{
        //    return new Impl<TService, TImplementation>();
        //}

        public static ServiceReplacementCollection Collection(Action<ServiceReplacementCollectionBuilder> build)
        {
            var replacements = new List<ServiceReplacement>();
            var builder = new ServiceReplacementCollectionBuilder(replacements);
            build(builder);
            return new ServiceReplacementCollection(replacements);
        }

        private class Impl<TService, TImplementation> : ServiceReplacement
            where TImplementation : TService
        {
            public override void ReplaceService(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.ReplaceService<TService, TImplementation>();
            }

            public override void ReplaceService(IServiceCollection services)
            {
                var existing = services.FirstOrDefault(d => d.ServiceType == typeof(TService));
                if (existing == null) throw new NotImplementedException();
                services.Replace(ServiceDescriptor.Describe(typeof(TService), typeof(TImplementation), existing.Lifetime));
            }
        }

        public class ServiceReplacementCollectionBuilder
        {
            private readonly List<ServiceReplacement> _replacements;

            public ServiceReplacementCollectionBuilder(List<ServiceReplacement> replacements)
            {
                _replacements = replacements;
            }

            public ServiceReplacementCollectionBuilder Add<TService, TImplementation>()
                where TImplementation : TService
            {
                _replacements.Add(new Impl<TService, TImplementation>());
                return this;
            }
        }
    }

    public class ServiceReplacementCollection
    {
        private readonly IEnumerable<ServiceReplacement> _replacements;

        public ServiceReplacementCollection(IEnumerable<ServiceReplacement> replacements)
        {
            _replacements = replacements;
        }

        public void Apply(IServiceCollection services)
        {
            foreach (var replacement in _replacements)
                replacement.ReplaceService(services);
        }

        public void Apply(DbContextOptionsBuilder optionsBuilder)
        {
            foreach (var replacement in _replacements)
                replacement.ReplaceService(optionsBuilder);
        }
    }
}
