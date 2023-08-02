using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// A general implementation of <c>IDbConnection</c> that uses an <see cref="IDbProfiler"/>
    /// to collect profiling information.
    /// </summary>
    public class SimpleProfiledConnection : IDbConnection
    {
        private IDbProfiler? _profiler;
        private IDbConnection _connection;

        /// <inheritdoc cref="IDbConnection.ConnectionString"/>
        [AllowNull]
        public string ConnectionString
        {
            get => _connection.ConnectionString;
            set => _connection.ConnectionString = value;
        }

        /// <inheritdoc cref="IDbConnection.ConnectionTimeout"/>
        public int ConnectionTimeout => _connection.ConnectionTimeout;

        /// <inheritdoc cref="IDbConnection.Database"/>
        public string Database => _connection.Database;

        /// <inheritdoc cref="IDbConnection.State"/>
        public ConnectionState State => _connection.State;

        /// <summary>
        /// Gets the internally wrapped <see cref="IDbConnection"/>.
        /// </summary>
        public IDbConnection WrappedConnection => _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleProfiledConnection"/> class.
        /// Creates a simple profiled connection instance.
        /// </summary>
        /// <param name="connection">The database connection to wrap.</param>
        /// <param name="profiler">The profiler to use.</param>
        public SimpleProfiledConnection(IDbConnection connection, IDbProfiler profiler)
        {
            _connection = connection;
            if (profiler != null)
            {
                _profiler = profiler;
            }
        }

        /// <inheritdoc cref="IDbConnection.BeginTransaction()"/>
        public IDbTransaction BeginTransaction() =>
            new SimpleProfiledTransaction(_connection.BeginTransaction(), this);

        /// <inheritdoc cref="IDbConnection.BeginTransaction(IsolationLevel)"/>
        public IDbTransaction BeginTransaction(IsolationLevel il) =>
            new SimpleProfiledTransaction(_connection.BeginTransaction(il), this);

        /// <inheritdoc cref="IDbConnection.ChangeDatabase(string)"/>
        public void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

        /// <inheritdoc cref="IDbConnection.CreateCommand()"/>
        public IDbCommand CreateCommand() =>
            new SimpleProfiledCommand(_connection.CreateCommand(), this, _profiler);

        /// <inheritdoc cref="IDbConnection.Close()"/>
        public void Close() => _connection.Close();

        /// <inheritdoc cref="IDbConnection.Open()"/>
        public void Open() => _connection.Open();

        /// <summary>
        /// Releases all resources used by the connection.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the connection and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">false if the dispose is called from a <c>finalizer</c></param>
        private void Dispose(bool disposing)
        {
            if (disposing && _connection != null && _connection.State != ConnectionState.Closed)
            {
                _connection.Dispose();
            }
            _connection = null!;
            _profiler = null;
        }
    }
}
