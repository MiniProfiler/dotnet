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

        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "This does not appear to be used anywhere, we need to refactor it.")]
        public static ProfiledDbProviderFactory Instance = new ProfiledDbProviderFactory();

        /// <summary>
        /// The profiler.
        /// </summary>
        private IDbProfiler _profiler;

        /// <summary>
        /// The tail.
        /// </summary>
        private DbProviderFactory _tail;

        /// <summary>
        /// Prevents a default instance of the <see cref="ProfiledDbProviderFactory"/> class from being created. 
        /// Used for database provider APIS internally 
        /// </summary>
        private ProfiledDbProviderFactory()
        {
        }

        /// <summary>
        /// Allow to re-initialise the provider factory.
        /// </summary>
        /// <param name="profiler">The profiler.</param>
        /// <param name="tail">The tail.</param>
        public void InitProfiledDbProviderFactory(IDbProfiler profiler, DbProviderFactory tail)
        {
            _profiler = profiler;
            _tail = tail;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="ProfiledDbProviderFactory"/> class. 
        /// proxy provider factory
        /// </summary>
        /// <param name="profiler">The profiler.</param>
        /// <param name="tail">The tail.</param>
        public ProfiledDbProviderFactory(IDbProfiler profiler, DbProviderFactory tail)
        {
            _profiler = profiler;
            _tail = tail;
        }

        /// <summary>
        /// Gets a value indicating whether a data source enumerator can be created.
        /// </summary>
        public override bool CanCreateDataSourceEnumerator
        {
            get
            {
                return _tail.CanCreateDataSourceEnumerator;
            }
        }

        /// <summary>
        /// create the data source enumerator.
        /// </summary>
        /// <returns>The <see cref="DbDataSourceEnumerator"/>.</returns>
        public override DbDataSourceEnumerator CreateDataSourceEnumerator()
        {
            return _tail.CreateDataSourceEnumerator();
        }

        /// <summary>
        /// create the command.
        /// </summary>
        /// <returns>The <see cref="DbCommand"/>.</returns>
        public override DbCommand CreateCommand()
        {
            return new ProfiledDbCommand(_tail.CreateCommand(), null, _profiler);
        }

        /// <summary>
        /// create the connection.
        /// </summary>
        /// <returns>The <see cref="DbConnection"/>.</returns>
        public override DbConnection CreateConnection()
        {
            return new ProfiledDbConnection(_tail.CreateConnection(), _profiler);
        }

        /// <summary>
        /// create the parameter.
        /// </summary>
        /// <returns>The <see cref="DbParameter"/>.</returns>
        public override DbParameter CreateParameter()
        {
            return _tail.CreateParameter();
        }

        /// <summary>
        /// create the connection string builder.
        /// </summary>
        /// <returns>
        /// The <see cref="DbConnectionStringBuilder"/>.
        /// </returns>
        public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            return _tail.CreateConnectionStringBuilder();
        }

        /// <summary>
        /// create the command builder.
        /// </summary>
        /// <returns>
        /// The <see cref="DbCommandBuilder"/>.
        /// </returns>
        public override DbCommandBuilder CreateCommandBuilder()
        {
            return _tail.CreateCommandBuilder();
        }

        /// <summary>
        /// create the data adapter.
        /// </summary>
        /// <returns>
        /// The <see cref="DbDataAdapter"/>.
        /// </returns>
        public override DbDataAdapter CreateDataAdapter()
        {
            return new ProfiledDbDataAdapter(_tail.CreateDataAdapter(), _profiler);
        }

        /// <summary>
        /// create the permission.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>The <see cref="CodeAccessPermission"/>.</returns>
        public override CodeAccessPermission CreatePermission(System.Security.Permissions.PermissionState state)
        {
            return _tail.CreatePermission(state);
        }

    }
}
