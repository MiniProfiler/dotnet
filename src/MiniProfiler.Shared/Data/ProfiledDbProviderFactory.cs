using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// Wrapper for a database provider factory to enable profiling
    /// </summary>
    public class ProfiledDbProviderFactory : DbProviderFactory
    {
        private DbProviderFactory _tail;

        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "This does not appear to be used anywhere, we need to refactor it.")]
        public static ProfiledDbProviderFactory Instance = new ProfiledDbProviderFactory();

        /// <summary>
        /// Initialises a new instance of the <see cref="ProfiledDbProviderFactory"/> class.
        /// A proxy provider factory
        /// </summary>
        /// <param name="tail">The provider factory to wrap.</param>
        public ProfiledDbProviderFactory(DbProviderFactory tail) 
        {
            _tail = tail;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="ProfiledDbProviderFactory"/> class from being created.
        /// Used for database provider APIS internally
        /// </summary>
        private ProfiledDbProviderFactory() { }

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbCommand"/> class.</summary>
        /// <returns>A new instance of <see cref="DbCommand"/>.</returns>
        public override DbCommand CreateCommand()
        {
            var profiler = MiniProfiler.Current;

            var command = _tail.CreateCommand();

            return profiler != null
                ? new ProfiledDbCommand(command, null, MiniProfiler.Current)
                : command;
        }

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbConnection"/> class.</summary>
        /// <returns>A new instance of <see cref="DbConnection"/>.</returns>
        public override DbConnection CreateConnection()
        {
            var profiler = MiniProfiler.Current;

            var connection = _tail.CreateConnection();

            return profiler != null
                ? new ProfiledDbConnection(connection, MiniProfiler.Current)
                : connection;
        }

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbConnectionStringBuilder"/> class.</summary>
        /// <returns>A new instance of <see cref="DbConnectionStringBuilder"/>.</returns>
        public override DbConnectionStringBuilder CreateConnectionStringBuilder() => _tail.CreateConnectionStringBuilder();

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbParameter"/> class.</summary>
        /// <returns>A new instance of <see cref="DbParameter"/>.</returns>
        public override DbParameter CreateParameter() => _tail.CreateParameter();

        /// <summary>
        /// Allow to re-initialise the provider factory.
        /// </summary>
        /// <param name="tail">The tail.</param>
        public void InitProfiledDbProviderFactory(DbProviderFactory tail) => _tail = tail;

// TODO: These are added back in netstandard1.7
#if NET45
        /// <summary>
        /// Specifies whether the specific <see cref="DbProviderFactory"/> supports the <see cref="DbDataSourceEnumerator"/> class.
        /// </summary>
        public override bool CanCreateDataSourceEnumerator => _tail.CanCreateDataSourceEnumerator;

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbCommandBuilder"/> class.</summary>
        /// <returns>A new instance of <see cref="DbCommandBuilder"/>.</returns>
        public override DbCommandBuilder CreateCommandBuilder() => _tail.CreateCommandBuilder();

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbDataAdapter"/> class.</summary>
        /// <returns>A new instance of <see cref="DbDataAdapter"/>.</returns>
        public override DbDataAdapter CreateDataAdapter()
        {
            var profiler = MiniProfiler.Current;

            var dataAdapter = _tail.CreateDataAdapter();

            return profiler != null
                ? new ProfiledDbDataAdapter(dataAdapter, MiniProfiler.Current)
                : dataAdapter;
        }

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbDataSourceEnumerator"/> class.</summary>
        /// <returns>A new instance of <see cref="DbDataSourceEnumerator"/>.</returns>
        public override DbDataSourceEnumerator CreateDataSourceEnumerator() => _tail.CreateDataSourceEnumerator();
        
        /// <summary>Returns a new instance of the provider's class that implements the provider's version of the <see cref="CodeAccessPermission"/> class.</summary>
        /// <param name="state">One of the <see cref="PermissionState"/> values.</param>
        /// <returns>A <see cref="CodeAccessPermission"/> object for the specified <see cref="PermissionState"/>.</returns>
        public override CodeAccessPermission CreatePermission(System.Security.Permissions.PermissionState state) => _tail.CreatePermission(state);
#endif
    }
}