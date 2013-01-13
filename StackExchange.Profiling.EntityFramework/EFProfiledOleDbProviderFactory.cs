namespace StackExchange.Profiling.Data
{
    using System.Data.OleDb;

    /// <summary>
    /// Specific implementation of <c>EFProfiledDbProviderFactory&lt;OleDbFactory&gt;</c> to enable profiling
    /// </summary>
    public class EFProfiledOleDbProviderFactory
        : EFProfiledDbProviderFactory<OleDbFactory>
    {
        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        public static new EFProfiledOleDbProviderFactory Instance = new EFProfiledOleDbProviderFactory();

        /// <summary>
        /// Prevents a default instance of the <see cref="EFProfiledOleDbProviderFactory"/> class from being created.
        /// </summary>
        private EFProfiledOleDbProviderFactory()
        {
        }
    }
}
