using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Spatial;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// Wrapper for a database provider factory to enable profiling
    /// </summary>
    public class EFProfiledDbProviderServices : DbProviderServices
    {
        private readonly DbProviderServices _tail;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFProfiledDbProviderServices"/> class. 
        /// Used for DB provider APIS internally 
        /// </summary>
        /// <param name="providerServices">The <see cref="DbProviderServices"/> to wrap.</param>
        /// <exception cref="Exception">Throws when providerServices is <c>null</c>.</exception>
        public EFProfiledDbProviderServices(DbProviderServices providerServices)
        {
            _tail = providerServices ??
                throw new ArgumentException("providerServices cannot be null. Please check that your web.config defines a <DbProviderFactories> section underneath <system.data>.", nameof(providerServices));
        }

        /// <summary>
        /// Get DB command definition
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <returns>the command definition.</returns>
        public override DbCommandDefinition CreateCommandDefinition(DbCommand prototype) =>
            _tail.CreateCommandDefinition(prototype);

        /// <summary>
        /// The get database provider manifest.
        /// </summary>
        /// <param name="manifestToken">The manifest token.</param>
        /// <returns>the provider manifest.</returns>
        protected override DbProviderManifest GetDbProviderManifest(string manifestToken) =>
            _tail.GetProviderManifest(manifestToken);

        /// <summary>
        /// Get the database provider manifest token.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>a string containing the token.</returns>
        protected override string GetDbProviderManifestToken(DbConnection connection) =>
            _tail.GetProviderManifestToken(connection is ProfiledDbConnection profiled ? profiled.WrappedConnection : connection);

        /// <summary>
        /// Create the database command definition.
        /// </summary>
        /// <param name="providerManifest">The provider manifest.</param>
        /// <param name="commandTree">The command tree.</param>
        /// <returns>the command definition.</returns>
        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, DbCommandTree commandTree)
        {
            var cmdDef = _tail.CreateCommandDefinition(providerManifest, commandTree);
            var cmd = cmdDef.CreateCommand();
            var profiler = cmd.Connection is ProfiledDbConnection profiledConn ? profiledConn.Profiler : MiniProfiler.Current;
            return CreateCommandDefinition(new ProfiledDbCommand(cmd, cmd.Connection, profiler));
        }

        /// <summary>
        /// Create the database.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="commandTimeout">The command timeout.</param>
        /// <param name="storeItemCollection">The store item collection.</param>
        protected override void DbCreateDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection) =>
            _tail.CreateDatabase(GetRealConnection(connection), commandTimeout, storeItemCollection);

        /// <summary>
        /// Delete the database.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="commandTimeout">The command timeout.</param>
        /// <param name="storeItemCollection">The store item collection.</param>
        protected override void DbDeleteDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection) =>
            _tail.DeleteDatabase(GetRealConnection(connection), commandTimeout, storeItemCollection);

        /// <summary>
        /// Create the database script.
        /// </summary>
        /// <param name="providerManifestToken">The provider manifest token.</param>
        /// <param name="storeItemCollection">The store item collection.</param>
        /// <returns>a string containing the database script.</returns>
        protected override string DbCreateDatabaseScript(string providerManifestToken, StoreItemCollection storeItemCollection) =>
            _tail.CreateDatabaseScript(providerManifestToken, storeItemCollection);

        /// <summary>
        /// test if the database exists.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="commandTimeout">The command timeout.</param>
        /// <param name="storeItemCollection">The store item collection.</param>
        /// <returns>true if the database exists.</returns>
        protected override bool DbDatabaseExists(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection) =>
            _tail.DatabaseExists(GetRealConnection(connection), commandTimeout, storeItemCollection);

        /// <summary>
        /// Gets the real connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>the database connection</returns>
        private static DbConnection GetRealConnection(DbConnection connection) =>
            connection is ProfiledDbConnection profiled ? profiled.WrappedConnection : connection;

        /// <summary>
        /// Called to resolve additional default provider services when a derived type is registered 
        /// as an EF provider either using an entry in the application's config file or through code-based 
        /// registration in <c>DbConfiguration</c>. The implementation of this method in this class uses the resolvers 
        /// added with the AddDependencyResolver method to resolve dependencies.
        /// </summary>
        /// <param name="type">The type of the service to be resolved.</param>
        /// <param name="key">An optional key providing additional information for resolving the service.</param>
        /// <returns>An instance of the given type, or null if the service could not be resolved.</returns>
        public override object GetService(Type type, object key) =>
            _tail.GetService(type, key);

        /// <summary>
        /// Called to resolve additional default provider services when a derived type is registered 
        /// as an EF provider either using an entry in the application's config file or through code-based 
        /// registration in <c>DbConfiguration</c>. The implementation of this method in this class uses the resolvers 
        /// added with the AddDependencyResolver method to resolve dependencies.
        /// </summary>
        /// <param name="type">The type of the service to be resolved.</param>
        /// <param name="key">An optional key providing additional information for resolving the service.</param>
        /// <returns>All registered services that satisfy the given type and key, or an empty enumeration if there are none.</returns>
        public override IEnumerable<object> GetServices(Type type, object key) =>
            _tail.GetServices(type, key);

        /// <summary>
        /// Gets the spatial data reader for the DbProviderServices.
        /// </summary>
        /// <param name="fromReader">The reader where the spatial data came from.</param>
        /// <param name="manifestToken">The token information associated with the provider manifest.</param>
        /// <returns>The spatial data reader.</returns>
        protected override DbSpatialDataReader GetDbSpatialDataReader(DbDataReader fromReader, string manifestToken) =>
            _tail.GetSpatialDataReader(fromReader is ProfiledDbDataReader profiled ? profiled.WrappedReader : fromReader, manifestToken);

        /// <summary>
        /// Gets the spatial services for the <c>DbProviderServices</c>.
        /// </summary>
        /// <param name="manifestToken">The token information associated with the provider manifest.</param>
        /// <returns>The spatial services.</returns>
        [Obsolete("Return DbSpatialServices from the GetService method. See http://go.microsoft.com/fwlink/?LinkId=260882 for more information.")]
        protected override DbSpatialServices DbGetSpatialServices(string manifestToken) =>
#pragma warning disable 618, 672
            _tail.GetSpatialServices(manifestToken);
#pragma warning restore 618, 672

        /// <summary>
        /// Sets the parameter value and appropriate facets for the given <c>TypeUsage</c>.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="parameterType">The type of parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        protected override void SetDbParameterValue(DbParameter parameter, TypeUsage parameterType, object value) =>
            _tail.SetParameterValue(parameter, parameterType, value);

        /// <summary>
        /// Clones the connection.
        /// </summary>
        /// <param name="connection">The original connection.</param>
        /// <returns></returns>
        public override DbConnection CloneDbConnection(DbConnection connection) =>
            connection is ProfiledDbConnection profiled
                ? new ProfiledDbConnection(base.CloneDbConnection(profiled.WrappedConnection), profiled.Profiler)
                : base.CloneDbConnection(connection);

        /// <summary>
        /// Clones the connection.
        /// </summary>
        /// <param name="connection">The original connection.</param>
        /// <param name="factory">The factory to use.</param>
        /// <returns>Cloned connection</returns>
        public override DbConnection CloneDbConnection(DbConnection connection, DbProviderFactory factory) =>
            connection is ProfiledDbConnection profiled
                ? new ProfiledDbConnection(base.CloneDbConnection(profiled.WrappedConnection, factory), profiled.Profiler)
                : base.CloneDbConnection(connection, factory);
    }
}
