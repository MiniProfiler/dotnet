namespace StackExchange.Profiling.Data
{
    using System.Data.SqlClient;

    /// <summary>
    /// Specific implementation of <c>EFProfiledDbProviderFactory&lt;SqlClientFactory&gt;</c> to enable profiling
    /// </summary>
    public class EFProfiledSqlClientDbProviderFactory
        : EFProfiledDbProviderFactory<SqlClientFactory>
    {
        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        public static new EFProfiledSqlClientDbProviderFactory Instance = new EFProfiledSqlClientDbProviderFactory();

        /// <summary>
        /// Prevents a default instance of the <see cref="EFProfiledSqlClientDbProviderFactory"/> class from being created.
        /// </summary>
        private EFProfiledSqlClientDbProviderFactory()
        {
        }
    }
}
