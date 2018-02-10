using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.DependencyResolution;
using System.Linq;
using System.Reflection;
using StackExchange.Profiling.Data;

namespace StackExchange.Profiling.EntityFramework6
{
    /// <summary>
    /// Replacement for the DefaultInvariantNameResolver which can correctly resolve an <see cref="IProviderInvariantName"/> given a <see cref="ProfiledDbProviderFactory"/>.
    /// </summary>
    internal class EFProfiledInvariantNameResolver : IDbDependencyResolver
    {
        private readonly ConcurrentDictionary<DbProviderFactory, IProviderInvariantName> _providerInvariantNameCache =
            new ConcurrentDictionary<DbProviderFactory, IProviderInvariantName>();

        public object GetService(Type type, object key)
        {
            if (type != typeof(IProviderInvariantName))
            {
                return null;
            }
            var factory = key is ProfiledDbProviderFactory profiled ? profiled.WrappedDbProviderFactory : key as DbProviderFactory;
            if (factory == null)
            {
                return null;
            }
            return _providerInvariantNameCache.GetOrAdd(factory, GetProviderInvariantNameViaReflection);
        }

        public IEnumerable<object> GetServices(Type type, object key)
        {
            var service = GetService(type, key);
            return service == null ? Enumerable.Empty<object>() : new[] { service };
        }

        private static IProviderInvariantName GetProviderInvariantNameViaReflection(DbProviderFactory factory)
        {
            // Avert your eyes. EF6 implements a handy helper method to get the Invariant Name given a DbProviderFactory instance,
            // but of course it is marked internal. Rather than rewrite all of that code, we'll just call into it via reflection and cache the result.
            try
            {
                var extensionsType = Type.GetType("System.Data.Entity.Utilities.DbProviderFactoryExtensions, EntityFramework");
                var getProviderInvariantNameMethod = extensionsType.GetMethod("GetProviderInvariantName", BindingFlags.Static | BindingFlags.Public);
                var providerInvariantName = (string)getProviderInvariantNameMethod.Invoke(null, new[] { factory });
                return new ProviderInvariantName(providerInvariantName);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
                throw;
            }
        }

        private class ProviderInvariantName : IProviderInvariantName
        {
            public string Name { get; }

            public ProviderInvariantName(string name) => Name = name;
        }
    }
}
