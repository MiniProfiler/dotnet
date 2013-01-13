namespace StackExchange.Profiling.Data
{
    using System;
    using System.Data.Common;
    using System.Reflection;
    using System.Security;

    using StackExchange.Profiling;

    /// <summary>
    /// Wrapper for a database provider factory to enable profiling
    /// </summary>
    /// <typeparam name="T">the factory type.</typeparam>
    public class EFProfiledDbProviderFactory<T> : DbProviderFactory, IServiceProvider where T : DbProviderFactory
    {
        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        public static EFProfiledDbProviderFactory<T> Instance = new EFProfiledDbProviderFactory<T>();

        /// <summary>
        /// The tail.
        /// </summary>
        private readonly T _tail;

        /// <summary>
        /// Initialises a new instance of the <see cref="EFProfiledDbProviderFactory{T}"/> class. 
        /// Used for DB provider APIS internally 
        /// </summary>
        protected EFProfiledDbProviderFactory()
        {
            FieldInfo field = typeof(T).GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            if (field != null)
                this._tail = (T)field.GetValue(null);
        }

        /// <summary>
        /// Gets a value indicating whether can create data source enumerator.
        /// </summary>
        public override bool CanCreateDataSourceEnumerator
        {
            get
            {
                return this._tail.CanCreateDataSourceEnumerator;
            }
        }

        /// <summary>
        /// The create data source enumerator.
        /// </summary>
        /// <returns>the data source enumerator.</returns>
        public override DbDataSourceEnumerator CreateDataSourceEnumerator()
        {
            return this._tail.CreateDataSourceEnumerator();
        }

        /// <summary>
        /// The create command.
        /// </summary>
        /// <returns>the command.</returns>
        public override DbCommand CreateCommand()
        {
            return new ProfiledDbCommand(this._tail.CreateCommand(), null, MiniProfiler.Current);
        }

        /// <summary>
        /// The create connection.
        /// </summary>
        /// <returns>
        /// The <see cref="DbConnection"/>.
        /// </returns>
        public override DbConnection CreateConnection()
        {
            return new EFProfiledDbConnection(this._tail.CreateConnection(), MiniProfiler.Current);
        }

        /// <summary>
        /// The create parameter.
        /// </summary>
        /// <returns>the parameter</returns>
        public override DbParameter CreateParameter()
        {
            return this._tail.CreateParameter();
        }

        /// <summary>
        /// The create connection string builder.
        /// </summary>
        /// <returns>
        /// The <see cref="DbConnectionStringBuilder"/>.
        /// </returns>
        public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            return this._tail.CreateConnectionStringBuilder();
        }

        /// <summary>
        /// The create command builder.
        /// </summary>
        /// <returns>the command builder</returns>
        public override DbCommandBuilder CreateCommandBuilder()
        {
            return this._tail.CreateCommandBuilder();
        }

        /// <summary>
        /// The create data adapter.
        /// </summary>
        /// <returns>the data adapter.</returns>
        public override DbDataAdapter CreateDataAdapter()
        {
            return this._tail.CreateDataAdapter();
        }

        /// <summary>
        /// The create permission.
        /// </summary>
        /// <param name="state">
        /// The state.
        /// </param>
        /// <returns>
        /// The <see cref="CodeAccessPermission"/>.
        /// </returns>
        public override CodeAccessPermission CreatePermission(System.Security.Permissions.PermissionState state)
        {
            return this._tail.CreatePermission(state);
        }

        /// <summary>
        /// Extension mechanism for additional services;  
        /// </summary>
        /// <param name="serviceType">The service Type.</param>
        /// <returns>requested service provider or null.</returns>
        object IServiceProvider.GetService(Type serviceType)
        {
            var tailProvider = this._tail as IServiceProvider;
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
