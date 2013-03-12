namespace StackExchange.Profiling.Data
{
    using System.Data.Common;
    using System.Diagnostics;

    /// <summary>
    /// The profiled database provider services.
    /// </summary>
    public class ProfiledDbProviderServices : DbProviderServices
    {
        /// <summary>
        /// The wrapped provider.
        /// </summary>
        private readonly DbProviderServices _wrapped;

        /// <summary>
        /// The profiler.
        /// </summary>
        private readonly IDbProfiler _profiler;

        /// <summary>
        /// Initialises a new instance of the <see cref="ProfiledDbProviderServices"/> class.
        /// </summary>
        /// <param name="tail">The tail.</param>
        /// <param name="profiler">The profiler.</param>
        public ProfiledDbProviderServices(DbProviderServices tail, IDbProfiler profiler)
        {
            _wrapped = tail;
            _profiler = profiler;
        }

        /// <summary>
        /// Get DB command definition
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <returns>the command definition.</returns>
        public override DbCommandDefinition CreateCommandDefinition(DbCommand prototype)
        {
            return _wrapped.CreateCommandDefinition(prototype);
        }

        /// <summary>
        /// The get database provider manifest.
        /// </summary>
        /// <param name="manifestToken">The manifest token.</param>
        /// <returns>the provider manifest.</returns>
        protected override DbProviderManifest GetDbProviderManifest(string manifestToken)
        {
            return _wrapped.GetProviderManifest(manifestToken);
        }

        /// <summary>
        /// get the database provider manifest token.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>a string containing the token.</returns>
        protected override string GetDbProviderManifestToken(DbConnection connection)
        {
            var wrappedConnection = connection;

            var profiled = connection as ProfiledDbConnection;
            if (profiled != null)
            {
                wrappedConnection = profiled.WrappedConnection;
            }

            return _wrapped.GetProviderManifestToken(wrappedConnection);
        }

        /// <summary>
        /// create the database command definition.
        /// </summary>
        /// <param name="providerManifest">The provider manifest.</param>
        /// <param name="commandTree">The command tree.</param>
        /// <returns>the command definition.</returns>
        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, System.Data.Common.CommandTrees.DbCommandTree commandTree)
        {
            var cmdDef = _wrapped.CreateCommandDefinition(providerManifest, commandTree);
            var cmd = cmdDef.CreateCommand();
            Debug.Assert(cmd != null, "cmd != null");
            return CreateCommandDefinition(new ProfiledDbCommand(cmd, cmd.Connection, _profiler));
        }

        /// <summary>
        /// create the database.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="commandTimeout">The command timeout.</param>
        /// <param name="storeItemCollection">The store item collection.</param>
        protected override void DbCreateDatabase(DbConnection connection, int? commandTimeout, System.Data.Metadata.Edm.StoreItemCollection storeItemCollection)
        {
            _wrapped.CreateDatabase(GetRealConnection(connection), commandTimeout, storeItemCollection);
        }

        /// <summary>
        /// delete the database.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="commandTimeout">The command timeout.</param>
        /// <param name="storeItemCollection">The store item collection.</param>
        protected override void DbDeleteDatabase(DbConnection connection, int? commandTimeout, System.Data.Metadata.Edm.StoreItemCollection storeItemCollection)
        {
            _wrapped.DeleteDatabase(GetRealConnection(connection), commandTimeout, storeItemCollection);
        }

        /// <summary>
        /// create the database script.
        /// </summary>
        /// <param name="providerManifestToken">The provider manifest token.</param>
        /// <param name="storeItemCollection">The store item collection.</param>
        /// <returns>a string containing the database script.</returns>
        protected override string DbCreateDatabaseScript(string providerManifestToken, System.Data.Metadata.Edm.StoreItemCollection storeItemCollection)
        {
            return _wrapped.CreateDatabaseScript(providerManifestToken, storeItemCollection);
        }

        /// <summary>
        /// test if the database exists.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="commandTimeout">The command timeout.</param>
        /// <param name="storeItemCollection">The store item collection.</param>
        /// <returns>true if the database exists.</returns>
        protected override bool DbDatabaseExists(DbConnection connection, int? commandTimeout, System.Data.Metadata.Edm.StoreItemCollection storeItemCollection)
        {
            return _wrapped.DatabaseExists(GetRealConnection(connection), commandTimeout, storeItemCollection);
        }

        /// <summary>
        /// get the real connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>the database connection</returns>
        private static DbConnection GetRealConnection(DbConnection connection)
        {
            var profiled = connection as ProfiledDbConnection;
            if (profiled != null)
            {
                connection = profiled.WrappedConnection;
            }

            return connection;
        }
    }
}
