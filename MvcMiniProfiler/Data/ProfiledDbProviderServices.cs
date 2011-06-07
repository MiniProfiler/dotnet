using System.Data.Common;

#if ENTITY_FRAMEWORK
namespace StackExchange.MvcMiniProfiler.Data
{
    class ProfiledDbProviderServices : DbProviderServices
    {
        private DbProviderServices tail;
        private MiniProfiler profiler;
        public ProfiledDbProviderServices(DbProviderServices tail, MiniProfiler profiler)
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
            return tail.GetProviderManifestToken(connection);
        }
        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, System.Data.Common.CommandTrees.DbCommandTree commandTree)
        {
            var cmdDef = tail.CreateCommandDefinition(providerManifest, commandTree);
            var cmd = cmdDef.CreateCommand();
            return CreateCommandDefinition(new ProfiledDbCommand(cmd, cmd.Connection, profiler));
        }
    }
}
#endif