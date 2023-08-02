using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
#if !NETSTANDARD2_0
using System.Security;
using System.Security.Permissions;
#endif

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// Wrapper for a database provider factory to enable profiling.
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
        /// Every provider factory must have an Instance public field.
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
        /// Used for database provider APIs internally.
        /// </summary>
#pragma warning disable CS8618
        private ProfiledDbProviderFactory() { }
#pragma warning restore CS8618

        /// <inheritdoc cref="DbProviderFactory.CreateCommand()"/>
        public override DbCommand? CreateCommand()
        {
            var command = _factory.CreateCommand();
            var profiler = MiniProfiler.Current;

            return command is not null && (profiler is not null || _alwaysWrap)
                ? new ProfiledDbCommand(command, null, profiler)
                : command;
        }

        /// <inheritdoc cref="DbProviderFactory.CreateConnection()"/>
        public override DbConnection? CreateConnection()
        {
            var connection = _factory.CreateConnection();
            var profiler = MiniProfiler.Current;

            return connection is not null && (profiler is not null || _alwaysWrap)
                ? new ProfiledDbConnection(connection, profiler)
                : connection;
        }

        /// <inheritdoc cref="DbProviderFactory.CreateConnectionStringBuilder()"/>
        public override DbConnectionStringBuilder? CreateConnectionStringBuilder() => _factory.CreateConnectionStringBuilder();

        /// <inheritdoc cref="DbProviderFactory.CreateParameter()"/>
        public override DbParameter? CreateParameter() => _factory.CreateParameter();

        /// <summary>
        /// Allow to re-initialize the provider factory.
        /// </summary>
        /// <param name="tail">The tail.</param>
        public void InitProfiledDbProviderFactory(DbProviderFactory tail) => _factory = tail;

        /// <inheritdoc cref="DbProviderFactory.CanCreateDataSourceEnumerator"/>
        public override bool CanCreateDataSourceEnumerator => _factory.CanCreateDataSourceEnumerator;

        /// <inheritdoc cref="DbProviderFactory.CreateCommandBuilder()"/>
        public override DbCommandBuilder? CreateCommandBuilder() => _factory.CreateCommandBuilder();

        /// <inheritdoc cref="DbProviderFactory.CreateDataAdapter()"/>
        public override DbDataAdapter? CreateDataAdapter()
        {
            var dataAdapter = _factory.CreateDataAdapter();
            var profiler = MiniProfiler.Current;

            return dataAdapter is not null && (profiler is not null || _alwaysWrap)
                ? new ProfiledDbDataAdapter(dataAdapter, profiler)
                : dataAdapter;
        }

        /// <inheritdoc cref="DbProviderFactory.CreateDataSourceEnumerator()"/>
        public override DbDataSourceEnumerator? CreateDataSourceEnumerator() => _factory.CreateDataSourceEnumerator();

#if NET46_OR_GREATER
        /// <inheritdoc cref="DbProviderFactory.CreatePermission(PermissionState)"/>
        public override CodeAccessPermission CreatePermission(PermissionState state) => _factory.CreatePermission(state);
#endif
    }
}
