using System;
using System.Data.Common;
using MvcMiniProfiler;
using System.Reflection;

namespace MvcMiniProfiler.Data
{
    /// <summary>
    /// Wrapper for a db provider factory to enable profiling
    /// </summary>
    public class EFProfiledDbProviderFactory<T> : DbProviderFactory, IServiceProvider where T : DbProviderFactory
    {
        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        public static EFProfiledDbProviderFactory<T> Instance = new EFProfiledDbProviderFactory<T>();

        private T tail;

        /// <summary>
        /// Used for db provider apis internally 
        /// </summary>
        protected EFProfiledDbProviderFactory ()
	    {
            FieldInfo field = typeof(T).GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            this.tail = (T)field.GetValue(null);
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
            return new ProfiledDbCommand(tail.CreateCommand(), null, MiniProfiler.Current);
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbConnection CreateConnection()
        {
            return new EFProfiledDbConnection(tail.CreateConnection(), MiniProfiler.Current);
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
                svc = new ProfiledDbProviderServices((DbProviderServices)svc, MiniProfiler.Current);
            }
            return svc;
        }
    }
}
