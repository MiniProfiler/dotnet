using System;

namespace MvcMiniProfiler.Data
{
    /// <summary>
    /// Utility class to provide translations between DbProviderFactory and EFProfiledDbProviderFactory types.
    /// This is to ensure that <see cref="EFProfiledDbConnection.DbProviderFactory"/> is consistent with provider
    /// factories injected as part of the <see cref="MiniProfilerEF.Initialize"/> method.
    /// Also allows for EF41 Update 1 workaround and future caching of factories for performance.
    /// </summary>
    internal class EFProviderUtilities
    {
        public static Func<Type, Type> ResolveFactoryType { get; set; }

        static EFProviderUtilities()
        {
            ResolveFactoryType = GetProfiledProviderFactoryType;
        }

        public static void UseEF41Hack()
        {
            ResolveFactoryType = GetEF41ProfiledProviderFactoryType;
        }

        public static Type ResolveFactoryTypeOrOriginal(Type factoryType)
        {
            return ResolveFactoryType(factoryType) ?? factoryType;
        }

        private static Type GetProfiledProviderFactoryType(Type factoryType)
        {
            return typeof(Data.EFProfiledDbProviderFactory<>).MakeGenericType(factoryType);
        }

        private static Type GetEF41ProfiledProviderFactoryType(Type factoryType)
        {
            if (factoryType == typeof(System.Data.SqlClient.SqlClientFactory))
                return typeof(Data.EFProfiledSqlClientDbProviderFactory);
            else if (factoryType == typeof(System.Data.OleDb.OleDbFactory))
                return typeof(Data.EFProfiledOleDbProviderFactory);
            else if (factoryType == typeof(System.Data.Odbc.OdbcFactory))
                return typeof(Data.EFProfiledOdbcProviderFactory);

            return null;
        }
    }
}
