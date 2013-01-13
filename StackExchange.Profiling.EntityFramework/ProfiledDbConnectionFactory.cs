namespace StackExchange.Profiling.Data
{
    using System.Data.Entity.Infrastructure;

    /// <summary>
    /// Connection factory used for EF Code First <c>DbContext</c> API
    /// </summary>
    public class ProfiledDbConnectionFactory : IDbConnectionFactory
    {
        /// <summary>
        /// The wrapped connection factory.
        /// </summary>
        private readonly IDbConnectionFactory _wrapped;

        /// <summary>
        /// Initialises a new instance of the <see cref="ProfiledDbConnectionFactory"/> class. 
        /// Create a profiled connection factory
        /// </summary>
        /// <param name="wrapped">
        /// The underlying connection that needs to be profiled
        /// </param>
        public ProfiledDbConnectionFactory(IDbConnectionFactory wrapped)
        {
            this._wrapped = wrapped;
        }

        /// <summary>
        /// Create a wrapped connection for profiling purposes 
        /// </summary>
        /// <param name="nameOrConnectionString">the name or connection string.</param>
        /// <returns>the connection</returns>
        public System.Data.Common.DbConnection CreateConnection(string nameOrConnectionString)
        {
            return new EFProfiledDbConnection(this._wrapped.CreateConnection(nameOrConnectionString), MiniProfiler.Current);
        }
    }
}
