using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// Wraps a database connection, allowing SQL execution timings to be collected when a <see cref="MiniProfiler"/> session is started.
    /// </summary>
    [System.ComponentModel.DesignerCategory("")]
    public class ProfiledDbConnection : DbConnection
#if NET46
, ICloneable
#endif
    {
        private DbConnection _connection;
        /// <summary>
        /// Gets the underlying, real database connection to your database provider.
        /// </summary>
        public DbConnection InnerConnection =>  _connection;

        private IDbProfiler _profiler;
        /// <summary>
        /// Gets the current profiler instance; could be null.
        /// </summary>
        public IDbProfiler Profiler => _profiler;

        /// <summary>
        /// Initialises a new instance of the <see cref="ProfiledDbConnection"/> class. 
        /// Returns a new <see cref="ProfiledDbConnection"/> that wraps <paramref name="connection"/>, 
        /// providing query execution profiling. If profiler is null, no profiling will occur.
        /// </summary>
        /// <param name="connection"><c>Your provider-specific flavour of connection, e.g. SqlConnection, OracleConnection</c></param>
        /// <param name="profiler">The currently started <see cref="MiniProfiler"/> or null.</param>
        public ProfiledDbConnection(DbConnection connection, IDbProfiler profiler)
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

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        public override string ConnectionString
        {
            get { return _connection.ConnectionString; }
            set { _connection.ConnectionString = value; }
        }

        /// <summary>
        /// Gets the time to wait while establishing a connection before terminating the attempt and generating an error.
        /// </summary>
        public override int ConnectionTimeout => _connection.ConnectionTimeout;

        /// <summary>
        /// Gets the name of the current database after a connection is opened, 
        /// or the database name specified in the connection string before the connection is opened.
        /// </summary>
        public override string Database => _connection.Database;

        /// <summary>
        /// Gets the name of the database server to which to connect.
        /// </summary>
        public override string DataSource => _connection.DataSource;

        /// <summary>
        /// Gets a string that represents the version of the server to which the object is connected.
        /// </summary>
        public override string ServerVersion => _connection.ServerVersion;

        /// <summary>
        /// Gets the current state of the connection.
        /// </summary>
        public override ConnectionState State => _connection.State;

        /// <summary>
        /// Changes the current database for an open connection.
        /// </summary>
        /// <param name="databaseName">The new database name.</param>
        public override void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

        /// <summary>
        /// Closes the connection to the database.
        /// This is the preferred method of closing any open connection.
        /// </summary>
        public override void Close() => _connection.Close();

        /// <summary>
        /// Opens a database connection with the settings specified by the <see cref="ConnectionString"/>.
        /// </summary>
        public override void Open() => _connection.Open();

        /// <summary>
        /// Starts a database transaction.
        /// </summary>
        /// <param name="isolationLevel">Specifies the isolation level for the transaction.</param>
        /// <returns>An object representing the new transaction.</returns>
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new ProfiledDbTransaction(_connection.BeginTransaction(isolationLevel), this);
        }

        /// <summary>
        /// Creates and returns a <see cref="DbCommand"/> object associated with the current connection.
        /// </summary>
        /// <returns>A <see cref="ProfiledDbCommand"/> wrapping the created <see cref="DbCommand"/>.</returns>
        protected override DbCommand CreateDbCommand() => new ProfiledDbCommand(_connection.CreateCommand(), this, _profiler);

        /// <summary>
        /// Dispose the underlying connection.
        /// </summary>
        /// <param name="disposing">false if pre-empted from a <c>finalizer</c></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _connection != null)
            {
                _connection.StateChange -= StateChangeHandler;
                _connection.Dispose();
            }
            _connection = null;
            _profiler = null;
            base.Dispose(disposing);
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

// TODO: Retuning in .Net Standard 2.0
#if NET46
        /// <summary>
        /// Gets a value indicating whether events can be raised.
        /// </summary>
        protected override bool CanRaiseEvents => true;

        /// <summary>
        /// Enlist the transaction.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        public override void EnlistTransaction(System.Transactions.Transaction transaction) =>_connection.EnlistTransaction(transaction);

        /// <summary>
        /// Gets the database schema.
        /// </summary>
        /// <returns>The <see cref="DataTable"/>.</returns>
        public override DataTable GetSchema() => _connection.GetSchema();

        /// <summary>
        /// Gets the collection schema.
        /// </summary>
        /// <param name="collectionName">The collection name.</param>
        /// <returns>The <see cref="DataTable"/>.</returns>
        public override DataTable GetSchema(string collectionName) => _connection.GetSchema(collectionName);

        /// <summary>
        /// Gets the filtered collection schema.
        /// </summary>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="restrictionValues">The restriction values.</param>
        /// <returns>The <see cref="DataTable"/>.</returns>
        public override DataTable GetSchema(string collectionName, string[] restrictionValues) => _connection.GetSchema(collectionName, restrictionValues);

        /// <summary>
        /// Creates a clone of this <see cref="ProfiledDbConnection"/>.
        /// </summary>
        /// <returns>The <see cref="ProfiledDbConnection"/>.</returns>
        public ProfiledDbConnection Clone()
        {
            var tail = _connection as ICloneable;
            if (tail == null) throw new NotSupportedException("Underlying " + _connection.GetType().Name + " is not cloneable");
            return new ProfiledDbConnection((DbConnection)tail.Clone(), _profiler);
        }

        /// <summary>
        /// Creates a clone of this <see cref="ProfiledDbConnection"/>.
        /// </summary>
        /// <returns>The <see cref="object"/>.</returns>
        object ICloneable.Clone() => Clone();
#endif
    }
}
