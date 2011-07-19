using System.Data.Common;

#if ENTITY_FRAMEWORK
namespace MvcMiniProfiler.Data
{
    class ProfiledDbProviderServices : DbProviderServices
    {
        private DbProviderServices tail;
        private IDbProfiler profiler;
        public ProfiledDbProviderServices(DbProviderServices tail, IDbProfiler profiler)
        {
            this.tail = tail;
            this.profiler = profiler;
        }

        protected override DbProviderManifest GetDbProviderManifest(string manifestToken)
        {
            return tail.GetProviderManifest(manifestToken);
        }
        protected override string GetDbProviderManifestToken(DbConnection connection)
        {
            var wrapped = connection;

            var profiled = connection as ProfiledDbConnection;
            if (profiled != null)
            {
                wrapped = profiled.WrappedConnection;
            }

            return tail.GetProviderManifestToken(wrapped);
        }
        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, System.Data.Common.CommandTrees.DbCommandTree commandTree)
        {
            var cmdDef = tail.CreateCommandDefinition(providerManifest, commandTree);
            var cmd = cmdDef.CreateCommand();
            return CreateCommandDefinition(new ProfiledDbCommand(cmd, cmd.Connection, profiler));
        }

        private static DbConnection GetRealConnection(DbConnection cnn)
        {
            var real = cnn as ProfiledDbConnection;
            if (real != null)
            {
                cnn = real.WrappedConnection;
            }
            return cnn;
        }

        protected override void DbCreateDatabase(DbConnection connection, int? commandTimeout, System.Data.Metadata.Edm.StoreItemCollection storeItemCollection)
        {
            connection = GetRealConnection(connection);
            var method = tail.GetType().GetMethod("DbCreateDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(tail, new object[] { connection, commandTimeout, storeItemCollection });
        }
    }
}
#endif