using System.Data.Entity;
using System.Data.Entity.Core.Common;
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
                DbConfiguration.Loaded += (_, a) => a.ReplaceService<DbProviderServices>(
                    (services, o) => new EFProfiledDbProviderServices(services));

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
    }
}
