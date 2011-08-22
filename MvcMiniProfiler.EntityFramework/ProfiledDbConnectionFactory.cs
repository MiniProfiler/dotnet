using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity.Infrastructure;

namespace MvcMiniProfiler.Data
{
    /// <summary>
    /// Connection factory used for EF Code First DbContext API
    /// </summary>
    public class ProfiledDbConnectionFactory : IDbConnectionFactory
    {
        IDbConnectionFactory _wrapped;

        /// <summary>
        /// Create a profiled connection factory
        /// </summary>
        /// <param name="wrapped">The underlying connection that needs to be profiled</param>
        public ProfiledDbConnectionFactory(IDbConnectionFactory wrapped)
        {
            _wrapped = wrapped;
        }

        /// <summary>
        /// Create a wrapped connection for profiling purposes 
        /// </summary>
        /// <param name="nameOrConnectionString"></param>
        /// <returns></returns>
        public System.Data.Common.DbConnection CreateConnection(string nameOrConnectionString)
        {
            return new EFProfiledDbConnection(_wrapped.CreateConnection(nameOrConnectionString), MiniProfiler.Current);
        }
    }
}
