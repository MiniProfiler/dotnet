namespace StackExchange.Profiling.Data
{
    using System.Data.Odbc;

    /// <summary>
    /// Specific implementation of <c>EFProfiledDbProviderFactory&lt;OdbcFactory&gt;</c> to enable profiling
    /// </summary>
    public class EFProfiledOdbcProviderFactory
        : EFProfiledDbProviderFactory<OdbcFactory>
    {
        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        public static EFProfiledOdbcProviderFactory instance = new EFProfiledOdbcProviderFactory();

        /// <summary>
        /// Prevents a default instance of the <see cref="EFProfiledOdbcProviderFactory"/> class from being created.
        /// </summary>
        private EFProfiledOdbcProviderFactory()
        {
        }
    }
}
