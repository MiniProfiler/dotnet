using System.Data.Common;
using System.Data.Entity.Infrastructure;
using StackExchange.Profiling.Data;

namespace StackExchange.Profiling.EntityFramework6
{
    /// <summary>
    /// Wrapper for a service for obtaining the correct System.Data.Common.DbProviderFactory from
    //  a given System.Data.Common.DbConnection.
    /// </summary>
    public class EFProfiledDbProviderFactoryResolver : IDbProviderFactoryResolver
    {
        private readonly IDbProviderFactoryResolver _inner;

        /// <summary>
        /// Initialises a new instance of the <see cref="EFProfiledDbProviderFactoryResolver"/> class.
        /// </summary>
        /// <param name="inner">The IDbProviderFactoryResolver to wrap.</param>
        public EFProfiledDbProviderFactoryResolver(IDbProviderFactoryResolver inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Returns the System.Data.Common.DbProviderFactory for the given connection,
        /// unwrapping the ProfiledDbConnection as necessary
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>The provider factory for the connection.</returns>
        public DbProviderFactory ResolveProviderFactory(DbConnection connection)
        {
            return _inner.ResolveProviderFactory(connection is ProfiledDbConnection profiled ? profiled.InnerConnection : connection);
        }
    }
}