using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.OleDb;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// Specific implementation of EFProfiledDbProviderFactory&lt;OleDbFactory&gt; to enable profiling
    /// </summary>
    public class EFProfiledOleDbProviderFactory
        : EFProfiledDbProviderFactory<OleDbFactory>
    {
        private EFProfiledOleDbProviderFactory()
        { }

        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        public static new EFProfiledOleDbProviderFactory Instance = new EFProfiledOleDbProviderFactory();

    }
}
