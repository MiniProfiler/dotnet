namespace StackExchange.Profiling.Data
{
    using System;

    /// <summary>
    /// Utility class to provide translations between <c>DbProviderFactory</c> and <c>EFProfiledDbProviderFactory</c> types.
    /// This is to ensure that <see cref="EFProfiledDbConnection.DbProviderFactory"/> is consistent with provider
    /// factories injected as part of the <see cref="MiniProfilerEF.Initialize"/> method.
    /// Also allows for EF41 Update 1 workaround and future caching of factories for performance.
    /// </summary>
    internal class EFProviderUtilities
    {
        /// <summary>
        /// Initialises static members of the <see cref="EFProviderUtilities"/> class.
        /// </summary>
        static EFProviderUtilities()
        {
            ResolveFactoryType = GetProfiledProviderFactoryType;
        }

        /// <summary>
        /// Gets or sets the resolve factory type.
        /// </summary>
        public static Func<Type, Type> ResolveFactoryType { get; set; }

        /// <summary>
        /// The use EF41 hack.
        /// </summary>
        public static void UseEF41Hack()
        {
            ResolveFactoryType = GetEF41ProfiledProviderFactoryType;
        }

        /// <summary>
        /// The resolve factory type or original.
        /// </summary>
        /// <param name="factoryType">
        /// The factory type.
        /// </param>
        /// <returns>
        /// The <see cref="Type"/>.
        /// </returns>
        public static Type ResolveFactoryTypeOrOriginal(Type factoryType)
        {
            return ResolveFactoryType(factoryType) ?? factoryType;
        }

        /// <summary>
        /// The get profiled provider factory type.
        /// </summary>
        /// <param name="factoryType">
        /// The factory type.
        /// </param>
        /// <returns>
        /// The <see cref="Type"/>.
        /// </returns>
        private static Type GetProfiledProviderFactoryType(Type factoryType)
        {
            return typeof(EFProfiledDbProviderFactory<>).MakeGenericType(factoryType);
        }

        /// <summary>
        /// The get EF41 profiled provider factory type.
        /// </summary>
        /// <param name="factoryType">The factory type.</param>
        /// <returns>The <see cref="Type"/>.</returns>
        private static Type GetEF41ProfiledProviderFactoryType(Type factoryType)
        {
            if (factoryType == typeof(System.Data.SqlClient.SqlClientFactory))
                return typeof(EFProfiledSqlClientDbProviderFactory);
            if (factoryType == typeof(System.Data.OleDb.OleDbFactory))
                return typeof(EFProfiledOleDbProviderFactory);
            if (factoryType == typeof(System.Data.Odbc.OdbcFactory))
                return typeof(EFProfiledOdbcProviderFactory);

            return null;
        }
    }
}
