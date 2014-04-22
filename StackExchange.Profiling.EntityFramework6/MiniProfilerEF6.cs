using System.Data.Entity.Infrastructure.Interception;

namespace StackExchange.Profiling.EntityFramework6
{
    /// <summary>
    /// Provides helper methods to help with initializing the MiniProfiler for Entity Framework 6.
    /// </summary>
    public static class MiniProfilerEF6
    {
        public static void Initialize()
        {
            DbInterception.Add(new MiniProfilerDbCommandInterceptor());
            ExcludeEntityFrameworkAssemblies();
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
