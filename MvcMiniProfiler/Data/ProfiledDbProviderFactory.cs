using System;
using System.Data.Common;
using MvcMiniProfiler;

namespace MvcMiniProfiler.Data
{
    /// <summary>
    /// Wrapper for a db provider factory to enable profiling
    /// </summary>
    public class ProfiledDbProviderFactory : DbProviderFactory, IServiceProvider
    {
        private readonly IDbProfiler profiler;
        private readonly DbProviderFactory tail;
        /// <summary>
        /// proxy
        /// </summary>
        /// <param name="profiler"></param>
        /// <param name="tail"></param>
        public ProfiledDbProviderFactory(IDbProfiler profiler, DbProviderFactory tail)
        {
            this.profiler = profiler;
            this.tail = tail;
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override bool CanCreateDataSourceEnumerator
        {
            get
            {
                return tail.CanCreateDataSourceEnumerator;
            }
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbDataSourceEnumerator CreateDataSourceEnumerator()
        {
            return tail.CreateDataSourceEnumerator();
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbCommand CreateCommand()
        {
            return new ProfiledDbCommand(tail.CreateCommand(), null, profiler);
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbConnection CreateConnection()
        {
            return ProfiledDbConnection.Get(tail.CreateConnection(), profiler);
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbParameter CreateParameter()
        {
            return tail.CreateParameter();
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            return tail.CreateConnectionStringBuilder();
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbCommandBuilder CreateCommandBuilder()
        {
            return tail.CreateCommandBuilder();
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbDataAdapter CreateDataAdapter()
        {
            return tail.CreateDataAdapter();
        }
        /// <summary>
        /// proxy
        /// </summary>
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
