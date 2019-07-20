using System.Collections.Concurrent;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using StackExchange.Profiling.Data;

namespace StackExchange.Profiling.EntityFrameworkClassic7
{
    /// <summary>
    /// Provides helper methods to help with initializing the MiniProfiler for Entity Framework 6.
    /// </summary>
    public static class MiniProfilerEFC7
    {
        private class Lookup<T> : ConcurrentDictionary<object, T> { /* just for brevity */ }
        private static readonly Lookup<DbProviderServices> _DbProviderServicesCache = new Lookup<DbProviderServices>();
        private static readonly Lookup<DbProviderFactory> _DbProviderFactoryCache = new Lookup<DbProviderFactory>();
        private static readonly Lookup<IDbProviderFactoryResolver> _IDbProviderFactoryResolverCache = new Lookup<IDbProviderFactoryResolver>();
        private static readonly Lookup<IDbConnectionFactory> _IDbConnectionFactoryCache = new Lookup<IDbConnectionFactory>();
        private static readonly object _nullKeyPlaceholder = new object();

        /// <summary>
        /// Registers the WrapProviderService method with the Entity Framework 6 DbConfiguration as a replacement service for DbProviderServices.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                DbConfiguration.Loaded += (_, a) =>
                {
                    a.ReplaceService((DbProviderServices inner, object key) => _DbProviderServicesCache.GetOrAdd(key ?? _nullKeyPlaceholder, __ => new EFProfiledDbProviderServices(inner)));
                    a.ReplaceService((DbProviderFactory inner, object key) => _DbProviderFactoryCache.GetOrAdd(key ?? _nullKeyPlaceholder, __ => new ProfiledDbProviderFactory(inner)));
                    a.ReplaceService((IDbProviderFactoryResolver inner, object key) => _IDbProviderFactoryResolverCache.GetOrAdd(key ?? _nullKeyPlaceholder, __ => new EFProfiledDbProviderFactoryResolver(inner)));
                    a.ReplaceService((IDbConnectionFactory inner, object key) => _IDbConnectionFactoryCache.GetOrAdd(key ?? _nullKeyPlaceholder, __ => new EFProfiledDbConnectionFactory(inner)));
                    a.AddDependencyResolver(new EFProfiledInvariantNameResolver(), false);
                };
            }
            catch (SqlException ex) when (ex.Message.Contains("Invalid column name 'ContextKey'"))
            {
                // Try to prevent tripping this harmless Exception when initializing the DB
                // Issue in EF6 upgraded from EF5 on first db call in debug mode: http://entityframework.codeplex.com/workitem/594
            }
        }
    }
}
