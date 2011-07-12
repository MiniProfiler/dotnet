using System;
using System.Data.Common;
using MvcMiniProfiler;

namespace MvcMiniProfiler.Data
{
    internal class ProfiledDbProviderFactory : DbProviderFactory, IServiceProvider
    {
        private readonly IDbProfiler profiler;
        private readonly DbProviderFactory tail;
        public ProfiledDbProviderFactory(IDbProfiler profiler, DbProviderFactory tail)
        {
            this.profiler = profiler;
            this.tail = tail;
        }
        public override bool CanCreateDataSourceEnumerator
        {
            get
            {
                return tail.CanCreateDataSourceEnumerator;
            }
        }
        public override DbDataSourceEnumerator CreateDataSourceEnumerator()
        {
            return tail.CreateDataSourceEnumerator();
        }
        public override DbCommand CreateCommand()
        {
            return new ProfiledDbCommand(tail.CreateCommand(), null, profiler);
        }
        public override DbConnection CreateConnection()
        {
            return ProfiledDbConnection.Get(tail.CreateConnection(), profiler);
        }
        public override DbParameter CreateParameter()
        {
            return tail.CreateParameter();
        }
        public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            return tail.CreateConnectionStringBuilder();
        }
        public override DbCommandBuilder CreateCommandBuilder()
        {
            return tail.CreateCommandBuilder();
        }
        public override DbDataAdapter CreateDataAdapter()
        {
            return tail.CreateDataAdapter();
        }
        public override System.Security.CodeAccessPermission CreatePermission(System.Security.Permissions.PermissionState state)
        {
            return tail.CreatePermission(state);
        }

        /// <summary>
        /// Extension mechanism for additional services;  
        /// </summary>
        /// <returns>requested service provider or null.</returns>
        object IServiceProvider.GetService(Type serviceType)
        {
            IServiceProvider tailProvider = tail as IServiceProvider;
            if (tailProvider == null) return null;
            var svc = tailProvider.GetService(serviceType);
            if (svc == null) return null;

#if ENTITY_FRAMEWORK
            if (serviceType == typeof(DbProviderServices))
            {
                svc = new ProfiledDbProviderServices((DbProviderServices)svc, profiler);
            }
#endif
            return svc;
        }
    }
}
