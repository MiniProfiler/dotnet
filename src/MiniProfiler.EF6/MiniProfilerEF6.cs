using System.Collections.Concurrent;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using StackExchange.Profiling.Data;

namespace StackExchange.Profiling.EntityFramework6
{
    /// <summary>
    /// Provides helper methods to help with initializing the MiniProfiler for Entity Framework 6.
    /// </summary>
    public static class MiniProfilerEF6
    {
        /// <summary>
        /// Registers the WrapProviderService method with the Entity Framework 6 DbConfiguration as a replacement service for DbProviderServices.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                DbConfiguration.Loaded += (_, a) =>
                {
                    a.ReplaceService<DbProviderServices>(WrapDbProviderServices);
                    a.ReplaceService<DbProviderFactory>(WrapDbProviderFactory);
                    a.ReplaceService<IDbProviderFactoryResolver>(WrapIDbProviderFactoryResolver);
                    a.ReplaceService<IDbConnectionFactory>(WrapIDbConnectionFactory);
                    a.AddDependencyResolver(new EFProfiledInvariantNameResolver(), false);
                };

                MiniProfiler.Settings.ExcludeAssembly("EntityFramework");
                MiniProfiler.Settings.ExcludeAssembly("EntityFramework.SqlServer");
                MiniProfiler.Settings.ExcludeAssembly("EntityFramework.SqlServerCompact");
                MiniProfiler.Settings.ExcludeAssembly(typeof(MiniProfilerEF6).Assembly.GetName().Name);
            }
            catch (SqlException ex)
            {
                // Try to prevent tripping this harmless Exception when initializing the DB
                // Issue in EF6 upgraded from EF5 on first db call in debug mode: http://entityframework.codeplex.com/workitem/594
                if (!ex.Message.Contains("Invalid column name 'ContextKey'"))
                {
                    throw;
                }
            }
        }

        private static readonly ConcurrentDictionary<object, DbProviderServices> _wrappedDbProviderServicesCache = new ConcurrentDictionary<object, DbProviderServices>();

        private static DbProviderServices WrapDbProviderServices(DbProviderServices inner, object key)
        {
            return _wrappedDbProviderServicesCache.GetOrAdd(key, _ => new EFProfiledDbProviderServices(inner));
        }

        private static readonly ConcurrentDictionary<object, DbProviderFactory> _wrappedDbProviderFactoryCache = new ConcurrentDictionary<object, DbProviderFactory>();

        private static DbProviderFactory WrapDbProviderFactory(DbProviderFactory inner, object key)
        {
            return _wrappedDbProviderFactoryCache.GetOrAdd(key, _ => new ProfiledDbProviderFactory(inner));
        }

        private static readonly ConcurrentDictionary<object, IDbProviderFactoryResolver> _wrappedIDbProviderFactoryResolverCache = new ConcurrentDictionary<object, IDbProviderFactoryResolver>();

        private static IDbProviderFactoryResolver WrapIDbProviderFactoryResolver(IDbProviderFactoryResolver inner, object key)
        {
            return _wrappedIDbProviderFactoryResolverCache.GetOrAdd(key, _ => new EFProfiledDbProviderFactoryResolver(inner));
        }

        private static readonly ConcurrentDictionary<object, IDbConnectionFactory> _wrappedIDbConnectionFactoryCache = new ConcurrentDictionary<object, IDbConnectionFactory>();

        private static IDbConnectionFactory WrapIDbConnectionFactory(IDbConnectionFactory inner, object key)
        {
            return _wrappedIDbConnectionFactoryCache.GetOrAdd(key, _ => new EFProfiledDbConnectionFactory(inner));
        }
    }
}
