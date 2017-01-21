using System;
using System.Data;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// A general implementation of <c>IDbConnection</c> that uses an <see cref="IDbProfiler"/>
    /// to collect profiling information.
    /// </summary>
    public class SimpleProfiledConnection : IDbConnection
    {
        private IDbProfiler _profiler;
        private IDbConnection _connection;

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        public string ConnectionString
        {
            get { return _connection.ConnectionString; }
            set { _connection.ConnectionString = value; }
        }

        /// <summary>
        /// Gets the connection timeout.
        /// </summary>
        public int ConnectionTimeout => _connection.ConnectionTimeout;

        /// <summary>
        /// Gets the database.
        /// </summary>
        public string Database => _connection.Database;

        /// <summary>
        /// Gets the state.
        /// </summary>
        public ConnectionState State => _connection.State;

        /// <summary>
        /// Gets the internally wrapped <see cref="IDbConnection"/>
        /// </summary>
        public IDbConnection WrappedConnection => _connection;

        /// <summary>
        /// Initialises a new instance of the <see cref="SimpleProfiledConnection"/> class. 
        /// Creates a simple profiled connection instance.
        /// </summary>
        /// <param name="connection">The database connection to wrap</param>
        /// <param name="profiler">The profiler to use</param>
        public SimpleProfiledConnection(IDbConnection connection, IDbProfiler profiler)
        {
            _connection = connection;
            if (profiler != null)
            {
                _profiler = profiler;
            }
        }

        /// <summary>
        /// Starts a database transaction.
        /// </summary>
        /// <returns>An object representing the new transaction.</returns>
        public IDbTransaction BeginTransaction() =>
            new SimpleProfiledTransaction(_connection.BeginTransaction(), this);

        /// <summary>
        /// Starts a database transaction.
        /// </summary>
        /// <param name="isolationLevel">Specifies the isolation level for the transaction.</param>
        /// <returns>An object representing the new transaction.</returns>
        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel) =>
            new SimpleProfiledTransaction(_connection.BeginTransaction(isolationLevel), this);

        /// <summary>
        /// Changes the current database for an open connection.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        public void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

        /// <summary>
        /// Creates and returns a <see cref="IDbCommand"/> object associated with the current connection.
        /// </summary>
        /// <returns>A <see cref="IDbCommand"/> object.</returns>
        public IDbCommand CreateCommand() =>
            new SimpleProfiledCommand(_connection.CreateCommand(), this, _profiler);

        /// <summary>
        /// Closes the connection to the database. This is the preferred method of closing any open connection.
        /// </summary>
        public void Close() => _connection.Close();

        /// <summary>
        /// Opens a database connection with the settings specified by the <see cref="ConnectionString"/>.
        /// </summary>
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
        /// Releases the unmanaged resources used by the connetion and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">false if the dispose is called from a <c>finalizer</c></param>
        private void Dispose(bool disposing)
        {
            if (disposing && _connection != null && _connection.State != ConnectionState.Closed)
            {
                _connection.Dispose();
            }
            _connection = null;
            _profiler = null;
        }
    }
}
