using System;
using System.Data.Common;
using MvcMiniProfiler;

namespace MvcMiniProfiler.Data
{
    /// <summary>
    /// Wrapper for a db provider factory to enable profiling
    /// </summary>
    public class EFProfiledDbProviderFactory : DbProviderFactory, IServiceProvider
    {

        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        public static EFProfiledDbProviderFactory Instance = new EFProfiledDbProviderFactory();

        private IDbProfiler profiler;
        private DbProviderFactory tail;


        /// <summary>
        /// Used for db provider apis internally 
        /// </summary>
        private EFProfiledDbProviderFactory ()
	    {

	    }

        /// <summary>
        /// Allow to re-init the provider factory.
        /// </summary>
        /// <param name="profiler"></param>
        /// <param name="tail"></param>
        public void InitProfiledDbProviderFactory(IDbProfiler profiler, DbProviderFactory tail)
        {
            this.profiler = profiler;
            this.tail = tail;
        }

        /// <summary>
        /// proxy
        /// </summary>
        /// <param name="profiler"></param>
        /// <param name="tail"></param>
        public EFProfiledDbProviderFactory(IDbProfiler profiler, DbProviderFactory tail)
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
            return new EFProfiledDbConnection(tail.CreateConnection(), profiler);
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

            if (serviceType == typeof(DbProviderServices))
            {
                svc = new ProfiledDbProviderServices((DbProviderServices)svc, profiler);
            }
            return svc;
        }
    }
}
