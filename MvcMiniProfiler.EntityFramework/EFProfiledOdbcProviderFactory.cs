using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.Odbc;

namespace MvcMiniProfiler.Data
{
    /// <summary>
    /// Specific implementation of EFProfiledDbProviderFactory&lt;OdbcFactory&gt; to enable profiling
    /// </summary>
    public class EFProfiledOdbcProviderFactory
        : EFProfiledDbProviderFactory<OdbcFactory>
    {
        private EFProfiledOdbcProviderFactory()
        { }

        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        public static new EFProfiledOdbcProviderFactory Instance = new EFProfiledOdbcProviderFactory();

    }
}
