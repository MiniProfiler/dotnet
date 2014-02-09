using System;

namespace StackExchange.Profiling.Data
{
    using System.Data.Common;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Diagnostics;
    using System.Reflection;
    using StackExchange.Profiling;

    /// <summary>
    /// Wrapper for a database provider factory to enable profiling
    /// </summary>
    /// <typeparam name="T">the factory type.</typeparam>
    public class EFProfiledDbProviderServices<T> : DbProviderServices where T : DbProviderServices
    {
        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        public static EFProfiledDbProviderServices<T> Instance = new EFProfiledDbProviderServices<T>();

        /// <summary>
        /// The tail.
        /// </summary>
        private readonly T _tail;

        /// <summary>
        /// Initialises a new instance of the <see cref="EFProfiledDbProviderServices{T}"/> class. 
        /// Used for DB provider APIS internally 
        /// </summary>
        protected EFProfiledDbProviderServices()
        {
            PropertyInfo property = typeof(T).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (property != null)
                _tail = (T)property.GetValue(null, null);

            if (_tail == null)
            {
                FieldInfo field = typeof(T).GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                if(field != null)
                    _tail = (T)field.GetValue(null);
            }
            if (_tail == null)
            {
                throw new Exception(string.Format("Unable to define EFProfiledDbProviderServices class of type '{0}'. Please check that your web.config defines a <DbProviderFactories> section underneath <system.data>.", typeof(T).Name));
            }
        }

        /// <summary>
        /// Get DB command definition
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <returns>the command definition.</returns>
        public override DbCommandDefinition CreateCommandDefinition(DbCommand prototype)
        {
            return _tail.CreateCommandDefinition(prototype);
        }

        /// <summary>
        /// The get database provider manifest.
        /// </summary>
        /// <param name="manifestToken">The manifest token.</param>
        /// <returns>the provider manifest.</returns>
        protected override DbProviderManifest GetDbProviderManifest(string manifestToken)
        {
            return _tail.GetProviderManifest(manifestToken);
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

            return _tail.GetProviderManifestToken(wrappedConnection);
        }

        /// <summary>
        /// create the database command definition.
        /// </summary>
        /// <param name="providerManifest">The provider manifest.</param>
        /// <param name="commandTree">The command tree.</param>
        /// <returns>the command definition.</returns>
        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, DbCommandTree commandTree)
        {
            var cmdDef = _tail.CreateCommandDefinition(providerManifest, commandTree);
            var cmd = cmdDef.CreateCommand();
            Debug.Assert(cmd != null, "cmd != null");
            return CreateCommandDefinition(new ProfiledDbCommand(cmd, cmd.Connection, MiniProfiler.Current));
        }

        /// <summary>
        /// create the database.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="commandTimeout">The command timeout.</param>
        /// <param name="storeItemCollection">The store item collection.</param>
        protected override void DbCreateDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            _tail.CreateDatabase(GetRealConnection(connection), commandTimeout, storeItemCollection);
        }

        /// <summary>
        /// delete the database.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="commandTimeout">The command timeout.</param>
        /// <param name="storeItemCollection">The store item collection.</param>
        protected override void DbDeleteDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            _tail.DeleteDatabase(GetRealConnection(connection), commandTimeout, storeItemCollection);
        }

        /// <summary>
        /// create the database script.
        /// </summary>
        /// <param name="providerManifestToken">The provider manifest token.</param>
        /// <param name="storeItemCollection">The store item collection.</param>
        /// <returns>a string containing the database script.</returns>
        protected override string DbCreateDatabaseScript(string providerManifestToken, StoreItemCollection storeItemCollection)
        {
            return _tail.CreateDatabaseScript(providerManifestToken, storeItemCollection);
        }

        /// <summary>
        /// test if the database exists.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="commandTimeout">The command timeout.</param>
        /// <param name="storeItemCollection">The store item collection.</param>
        /// <returns>true if the database exists.</returns>
        protected override bool DbDatabaseExists(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            return _tail.DatabaseExists(GetRealConnection(connection), commandTimeout, storeItemCollection);
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
