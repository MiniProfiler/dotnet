using System.Data.Common;
using System.Data.Entity.Infrastructure;
using StackExchange.Profiling.Data;

namespace StackExchange.Profiling.EntityFramework6
{
    /// <summary>
    /// Wrapper for a service for obtaining the correct <see cref="DbProviderFactory"/> from
    /// a given <see cref="DbConnection"/>.
    /// </summary>
    public class EFProfiledDbProviderFactoryResolver : IDbProviderFactoryResolver
    {
        private readonly IDbProviderFactoryResolver _inner;

        /// <summary>
        /// Initialises a new instance of the <see cref="EFProfiledDbProviderFactoryResolver"/> class.
        /// </summary>
        /// <param name="inner">The <see cref="IDbProviderFactoryResolver"/> to wrap.</param>
        public EFProfiledDbProviderFactoryResolver(IDbProviderFactoryResolver inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Returns the <see cref="DbProviderFactory"/> for the given connection,
        /// unwrapping the <see cref="ProfiledDbConnection"/> as necessary
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>The provider factory for the connection.</returns>
        public DbProviderFactory ResolveProviderFactory(DbConnection connection)
        {
            return _inner.ResolveProviderFactory(connection is ProfiledDbConnection profiled ? profiled.InnerConnection : connection);
        }
    }
}