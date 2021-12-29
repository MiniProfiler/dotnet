using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Security.Permissions;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// Wrapper for a database provider factory to enable profiling
    /// </summary>
    public class ProfiledDbProviderFactory : DbProviderFactory
    {
        private DbProviderFactory _factory;
        private readonly bool _alwaysWrap;

        /// <summary>
        /// The <see cref="DbProviderFactory"/> that this profiled version wraps.
        /// </summary>
        public DbProviderFactory WrappedDbProviderFactory => _factory;

        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "This does not appear to be used anywhere, we need to refactor it.")]
        public readonly static ProfiledDbProviderFactory Instance = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfiledDbProviderFactory"/> class.
        /// A proxy provider factory
        /// </summary>
        /// <param name="factory">The provider factory to wrap.</param>
        /// <param name="alwaysWrap">Whether ti always wrap results in Profiled equivalents, even if there's no active profiler</param>
        /// <remarks>
        /// This exists for places where extremely consistent behavior is desired. Primarily assigning profiled
        /// elements to others, where such an assignment would be invalid without the wrapping.
        /// Example: when MiniProfiler.Current is null:
        ///     alwaysWrap == false: CreateCommand returns (type), e.g. SqlCommand
        ///     alwaysWrap == true: CreateCommand  return ProfiledDbCommand
        /// </remarks>
        public ProfiledDbProviderFactory(DbProviderFactory factory, bool alwaysWrap = false)
        {
            _factory = factory;
            _alwaysWrap = alwaysWrap;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="ProfiledDbProviderFactory"/> class from being created.
        /// Used for database provider APIS internally
        /// </summary>
        private ProfiledDbProviderFactory()
        {
        }

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbCommand"/> class.</summary>
        /// <returns>A new instance of <see cref="DbCommand"/>.</returns>
        public override DbCommand CreateCommand()
        {
            var command = _factory.CreateCommand();
            var profiler = MiniProfiler.Current;

            return profiler != null || _alwaysWrap
                ? new ProfiledDbCommand(command, null, profiler)
                : command;
        }

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbConnection"/> class.</summary>
        /// <returns>A new instance of <see cref="DbConnection"/>.</returns>
        public override DbConnection CreateConnection()
        {
            var connection = _factory.CreateConnection();
            var profiler = MiniProfiler.Current;

            return profiler != null || _alwaysWrap
                ? new ProfiledDbConnection(connection, profiler)
                : connection;
        }

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbConnectionStringBuilder"/> class.</summary>
        /// <returns>A new instance of <see cref="DbConnectionStringBuilder"/>.</returns>
        public override DbConnectionStringBuilder CreateConnectionStringBuilder() => _factory.CreateConnectionStringBuilder();

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbParameter"/> class.</summary>
        /// <returns>A new instance of <see cref="DbParameter"/>.</returns>
        public override DbParameter CreateParameter() => _factory.CreateParameter();

        /// <summary>
        /// Allow to re-initialize the provider factory.
        /// </summary>
        /// <param name="tail">The tail.</param>
        public void InitProfiledDbProviderFactory(DbProviderFactory tail) => _factory = tail;

        /// <summary>
        /// Specifies whether the specific <see cref="DbProviderFactory"/> supports the <see cref="DbDataSourceEnumerator"/> class.
        /// </summary>
        public override bool CanCreateDataSourceEnumerator => _factory.CanCreateDataSourceEnumerator;

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbCommandBuilder"/> class.</summary>
        /// <returns>A new instance of <see cref="DbCommandBuilder"/>.</returns>
        public override DbCommandBuilder CreateCommandBuilder() => _factory.CreateCommandBuilder();

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbDataAdapter"/> class.</summary>
        /// <returns>A new instance of <see cref="DbDataAdapter"/>.</returns>
        public override DbDataAdapter CreateDataAdapter()
        {
            var profiler = MiniProfiler.Current;

            var dataAdapter = _factory.CreateDataAdapter();

            return profiler != null || _alwaysWrap
                ? new ProfiledDbDataAdapter(dataAdapter, profiler)
                : dataAdapter;
        }

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbDataSourceEnumerator"/> class.</summary>
        /// <returns>A new instance of <see cref="DbDataSourceEnumerator"/>.</returns>
        public override DbDataSourceEnumerator CreateDataSourceEnumerator() => _factory.CreateDataSourceEnumerator();

#if !NETSTANDARD2_0
        /// <summary>Returns a new instance of the provider's class that implements the provider's version of the <see cref="CodeAccessPermission"/> class.</summary>
        /// <param name="state">One of the <see cref="PermissionState"/> values.</param>
        /// <returns>A <see cref="CodeAccessPermission"/> object for the specified <see cref="PermissionState"/>.</returns>
        public override CodeAccessPermission CreatePermission(PermissionState state) => _factory.CreatePermission(state);
#endif
    }
}
