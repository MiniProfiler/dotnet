using System.Data.Common;

namespace MvcMiniProfiler.Data
{
    class ProfiledDbProviderServices : DbProviderServices
    {
        private DbProviderServices wrapped;
        private IDbProfiler profiler;
        public ProfiledDbProviderServices(DbProviderServices tail, IDbProfiler profiler)
        {
            this.wrapped = tail;
            this.profiler = profiler;
        }

        protected override DbProviderManifest GetDbProviderManifest(string manifestToken)
        {
            return wrapped.GetProviderManifest(manifestToken);
        }
        protected override string GetDbProviderManifestToken(DbConnection connection)
        {
            var wrappedConnection = connection;

            var profiled = connection as ProfiledDbConnection;
            if (profiled != null)
            {
                wrappedConnection = profiled.WrappedConnection;
            }

            return wrapped.GetProviderManifestToken(wrappedConnection);
        }
        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, System.Data.Common.CommandTrees.DbCommandTree commandTree)
        {
            var cmdDef = wrapped.CreateCommandDefinition(providerManifest, commandTree);
            var cmd = cmdDef.CreateCommand();
            return CreateCommandDefinition(new ProfiledDbCommand(cmd, cmd.Connection, profiler));
        }

        private static DbConnection GetRealConnection(DbConnection cnn)
        {
            var profiled = cnn as ProfiledDbConnection;
            if (profiled != null)
            {
                cnn = profiled.WrappedConnection;
            }
            return cnn;
        }

        protected override void DbCreateDatabase(DbConnection connection, int? commandTimeout, System.Data.Metadata.Edm.StoreItemCollection storeItemCollection)
        {
            wrapped.CreateDatabase(GetRealConnection(connection), commandTimeout, storeItemCollection);
        }

        protected override void DbDeleteDatabase(DbConnection connection, int? commandTimeout, System.Data.Metadata.Edm.StoreItemCollection storeItemCollection)
        {
            wrapped.DeleteDatabase(GetRealConnection(connection), commandTimeout, storeItemCollection);
        }

        protected override string DbCreateDatabaseScript(string providerManifestToken, System.Data.Metadata.Edm.StoreItemCollection storeItemCollection)
        {
            return wrapped.CreateDatabaseScript(providerManifestToken, storeItemCollection);
        }

        protected override bool DbDatabaseExists(DbConnection connection, int? commandTimeout, System.Data.Metadata.Edm.StoreItemCollection storeItemCollection)
        {
            return wrapped.DatabaseExists(GetRealConnection(connection), commandTimeout, storeItemCollection);
        }

        /// <summary>
        /// Get DB command definition
        /// </summary>
        /// <param name="prototype"></param>
        /// <returns></returns>
        public override DbCommandDefinition CreateCommandDefinition(DbCommand prototype)
        {
            return wrapped.CreateCommandDefinition(prototype);
        }
    }
}
