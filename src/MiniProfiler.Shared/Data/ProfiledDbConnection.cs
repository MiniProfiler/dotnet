using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// Wraps a database connection, allowing SQL execution timings to be collected when a <see cref="MiniProfiler"/> session is started.
    /// </summary>
    [System.ComponentModel.DesignerCategory("")]
    public class ProfiledDbConnection : DbConnection
    {
        private DbConnection _connection;
        private IDbProfiler? _profiler;

        /// <summary>
        /// Gets the current profiler instance; could be null.
        /// </summary>
        public IDbProfiler? Profiler => _profiler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfiledDbConnection"/> class.
        /// Returns a new <see cref="ProfiledDbConnection"/> that wraps <paramref name="connection"/>,
        /// providing query execution profiling. If profiler is null, no profiling will occur.
        /// </summary>
        /// <param name="connection"><c>Your provider-specific flavour of connection, e.g. SqlConnection, OracleConnection</c></param>
        /// <param name="profiler">The currently started <see cref="MiniProfiler"/> or null.</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="connection"/> is <c>null</c>.</exception>
        public ProfiledDbConnection(DbConnection connection, IDbProfiler? profiler)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _connection.StateChange += StateChangeHandler;

            if (profiler != null)
            {
                _profiler = profiler;
            }
        }

        /// <summary>
        /// Gets the connection that this ProfiledDbConnection wraps.
        /// </summary>
        public DbConnection WrappedConnection => _connection;

        /// <inheritdoc cref="DbConnection.ConnectionString"/>
        [AllowNull]
        public override string ConnectionString
        {
            get => _connection.ConnectionString;
            set => _connection.ConnectionString = value;
        }

        /// <inheritdoc cref="DbConnection.ConnectionTimeout"/>
        public override int ConnectionTimeout => _connection.ConnectionTimeout;

        /// <inheritdoc cref="DbConnection.Database"/>
        public override string Database => _connection.Database;

        /// <inheritdoc cref="DbConnection.DataSource"/>
        public override string DataSource => _connection.DataSource;

        /// <inheritdoc cref="DbConnection.ServerVersion"/>
        public override string ServerVersion => _connection.ServerVersion;

        /// <inheritdoc cref="DbConnection.State"/>
        public override ConnectionState State => _connection.State;

        /// <inheritdoc cref="DbConnection.ChangeDatabase(string)"/>
        public override void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

        /// <inheritdoc cref="DbConnection.Close()"/>
        public override void Close()
        {
            var miniProfiler = _profiler as MiniProfiler;
            if (miniProfiler?.IsActive != true || miniProfiler.Options?.TrackConnectionOpenClose == false)
            {
                _connection.Close();
                return;
            }

            using (miniProfiler.CustomTiming("sql", "Connection Close()", nameof(Close)))
            {
                _connection.Close();
            }
        }

        /// <inheritdoc cref="DbConnection.Open()"/>
        public override void Open()
        {
            var miniProfiler = _profiler as MiniProfiler;
            if (miniProfiler?.IsActive != true || miniProfiler.Options?.TrackConnectionOpenClose == false)
            {
                _connection.Open();
                return;
            }

            using (miniProfiler.CustomTiming("sql", "Connection Open()", nameof(Open)))
            {
                _connection.Open();
            }
        }

        /// <inheritdoc cref="DbConnection.OpenAsync(CancellationToken)"/>
        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            var miniProfiler = _profiler as MiniProfiler;
            if (miniProfiler?.IsActive != true || miniProfiler.Options?.TrackConnectionOpenClose == false)
            {
                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            using (miniProfiler.CustomTiming("sql", "Connection OpenAsync()", nameof(OpenAsync)))
            {
                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="DbConnection.BeginDbTransaction(IsolationLevel)"/>
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new ProfiledDbTransaction(_connection.BeginTransaction(isolationLevel), this);
        }

        /// <summary>
        /// Creates and returns a <see cref="DbCommand"/> object associated with the current connection.
        /// </summary>
        /// <returns>A <see cref="ProfiledDbCommand"/> wrapping the created <see cref="DbCommand"/>.</returns>
        protected virtual DbCommand CreateDbCommand(DbCommand original, IDbProfiler? profiler)
                => new ProfiledDbCommand(original, this, profiler);

        /// <inheritdoc cref="DbConnection.CreateDbCommand()"/>
        protected override DbCommand CreateDbCommand() => CreateDbCommand(_connection.CreateCommand(), _profiler);

        /// <summary>
        /// Dispose the underlying connection.
        /// </summary>
        /// <param name="disposing">false if preempted from a <c>finalizer</c></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _connection != null)
            {
                _connection.StateChange -= StateChangeHandler;
                _connection.Dispose();
            }
            base.Dispose(disposing);
            _connection = null!;
            _profiler = null;
        }

        /// <summary>
        /// The state change handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="stateChangeEventArguments">The state change event arguments.</param>
        private void StateChangeHandler(object sender, StateChangeEventArgs stateChangeEventArguments)
        {
            OnStateChange(stateChangeEventArguments);
        }

        /// <summary>
        /// Gets a value indicating whether events can be raised.
        /// </summary>
        protected override bool CanRaiseEvents => true;

        /// <inheritdoc cref="DbConnection.EnlistTransaction(System.Transactions.Transaction)"/>
        public override void EnlistTransaction(System.Transactions.Transaction? transaction) => _connection.EnlistTransaction(transaction);

        /// <inheritdoc cref="DbConnection.GetSchema()"/>
        public override DataTable GetSchema() => _connection.GetSchema();

        /// <inheritdoc cref="DbConnection.GetSchema(string)"/>
        public override DataTable GetSchema(string collectionName) => _connection.GetSchema(collectionName);

        /// <inheritdoc cref="DbConnection.GetSchema(string, string[])"/>
        public override DataTable GetSchema(string collectionName, string?[] restrictionValues) => _connection.GetSchema(collectionName, restrictionValues);
    }
}
