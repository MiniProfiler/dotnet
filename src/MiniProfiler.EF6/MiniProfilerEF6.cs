using System.Collections.Concurrent;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.SqlClient;
using System.Reflection;

using StackExchange.Profiling.Data;

namespace StackExchange.Profiling.EntityFramework6
{
    /// <summary>
    /// Provides helper methods to help with initializing the MiniProfiler for Entity Framework 6.
    /// </summary>
    public static class MiniProfilerEF6
    {
        /// <summary>
        /// A cache so we don't have to do reflection every time someone asks for the MiniProfiler implementation for a DB Provider.
        /// </summary>
        private static readonly ConcurrentDictionary<DbProviderServices,DbProviderServices> ProviderCache = new ConcurrentDictionary<DbProviderServices, DbProviderServices>();

        /// <summary>
        /// Registers the WrapProviderService method with the Entity Framework 6 DbConfiguration as a replacement service for DbProviderServices.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                DbConfiguration.Loaded += (_, a) => a.ReplaceService<DbProviderServices>(
                    (services, o) => WrapProviderService(services));

                ExcludeEntityFrameworkAssemblies();
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

        /// <summary>
        /// Wraps the provided DbProviderServices class in a MiniProfiler profiled DbService and returns the wrapped service.
        /// </summary>
        /// <param name="services">The DbProviderServices service to wrap.</param>
        /// <returns>A wrapped version of the DbProviderService service.</returns>
        private static DbProviderServices WrapProviderService(DbProviderServices services)
        {
            // First let's check our cache.
            if (ProviderCache.TryGetValue(services, out var result))
            {
                return result;
            }

            // Then let's see if our type is already wrapped.
            var serviceType = services.GetType();
            while (serviceType != null)
            {
                if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(EFProfiledDbProviderServices<>))
                {
                    // Add it to the cache and return it.
                    return ProviderCache[services] = services;
                }
                serviceType = serviceType.BaseType;
            }

            // Finally let's wrap it.
            var createdType = typeof(EFProfiledDbProviderServices<>).MakeGenericType(new[] { services.GetType() });
            var instanceProperty = createdType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            return ProviderCache[services] = instanceProperty.GetValue(null) as DbProviderServices;
        }

        private static void ExcludeEntityFrameworkAssemblies()
        {
            MiniProfiler.Settings.ExcludeAssembly("EntityFramework");
            MiniProfiler.Settings.ExcludeAssembly("EntityFramework.SqlServer");
            MiniProfiler.Settings.ExcludeAssembly("EntityFramework.SqlServerCompact");
            MiniProfiler.Settings.ExcludeAssembly(typeof(MiniProfilerEF6).Assembly.GetName().Name);
        }
    }
}
