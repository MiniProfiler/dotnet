using System.Data.Common;
using System.Data.Entity.Infrastructure;
using StackExchange.Profiling.Data;

namespace StackExchange.Profiling.EntityFramework6
{
    /// <summary>
    /// Wrapper for an <see cref="IDbConnectionFactory"/>
    /// </summary>
    public class EFProfiledDbConnectionFactory : IDbConnectionFactory
    {
        private readonly IDbConnectionFactory _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFProfiledDbConnectionFactory"/> class.
        /// </summary>
        /// <param name="inner">The <see cref="IDbConnectionFactory"/> to wrap.</param>
        public EFProfiledDbConnectionFactory(IDbConnectionFactory inner) => _inner = inner;

        /// <summary>
        /// Creates a connection based on the given database name or connection string.
        /// </summary>
        /// <param name="nameOrConnectionString">The database name or connection string.</param>
        /// <returns>An initialized <see cref="DbConnection"/>.</returns>
        public DbConnection CreateConnection(string nameOrConnectionString)
        {
            var connection = _inner.CreateConnection(nameOrConnectionString);
            if (connection is ProfiledDbConnection)
            {
                return connection;
            }

            var profiler = MiniProfiler.Current;
            return profiler != null
                ? new ProfiledDbConnection(connection, profiler)
                : connection;
        }
    }
}