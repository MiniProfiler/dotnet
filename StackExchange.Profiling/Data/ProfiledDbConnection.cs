namespace StackExchange.Profiling.Data
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Wraps a database connection, allowing SQL execution timings to be collected when a <see cref="MiniProfiler"/> session is started.
    /// </summary>
    [System.ComponentModel.DesignerCategory("")]
    public class ProfiledDbConnection : DbConnection, ICloneable
    {
        /// <summary>
        /// This will be made private; use <see cref="InnerConnection"/>
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "this must be changed in the future.")]
        // ReSharper disable InconsistentNaming
        protected DbConnection _connection; // TODO: in MiniProfiler 2.0, make private
        // ReSharper restore InconsistentNaming
        
        /// <summary>
        /// Gets the underlying, real database connection to your database provider.
        /// </summary>
        public DbConnection InnerConnection
        {
            get { return this._connection; }
        }

        /// <summary>
        /// This will be made private; use <see cref="Profiler"/>
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed. Suppression is OK here.")]
        // ReSharper disable InconsistentNaming
        protected IDbProfiler _profiler; // TODO: in MiniProfiler 2.0, make private
        // ReSharper restore InconsistentNaming
        
        /// <summary>
        /// Gets the current profiler instance; could be null.
        /// </summary>
        public IDbProfiler Profiler
        {
            get { return this._profiler; }
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="ProfiledDbConnection"/> class. 
        /// Returns a new <see cref="ProfiledDbConnection"/> that wraps <paramref name="connection"/>, 
        /// providing query execution profiling. If profiler is null, no profiling will occur.
        /// </summary>
        /// <param name="connection">
        /// <c>Your provider-specific flavour of connection, e.g. SqlConnection, OracleConnection</c>
        /// </param>
        /// <param name="profiler">
        /// The currently started <see cref="MiniProfiler"/> or null.
        /// </param>
        public ProfiledDbConnection(DbConnection connection, IDbProfiler profiler)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            this._connection = connection;
            this._connection.StateChange += this.StateChangeHandler;

            if (profiler != null)
            {
                this._profiler = profiler;
            }
        }

        /// <summary>
        /// Gets the wrapped connection.
        /// </summary>
        public DbConnection WrappedConnection
        {
            get { return this._connection; }
        }

        /// <summary>
        /// Gets a value indicating whether events can be raised.
        /// </summary>
        protected override bool CanRaiseEvents
        {
            get { return true; }
        }

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        public override string ConnectionString
        {
            get { return this._connection.ConnectionString; }
            set { this._connection.ConnectionString = value; }
        }

        /// <summary>
        /// Gets the connection timeout.
        /// </summary>
        public override int ConnectionTimeout
        {
            get { return this._connection.ConnectionTimeout; }
        }

        /// <summary>
        /// Gets the database.
        /// </summary>
        public override string Database
        {
            get { return this._connection.Database; }
        }

        /// <summary>
        /// Gets the data source.
        /// </summary>
        public override string DataSource
        {
            get { return this._connection.DataSource; }
        }

        /// <summary>
        /// Gets the server version.
        /// </summary>
        public override string ServerVersion
        {
            get { return this._connection.ServerVersion; }
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        public override ConnectionState State
        {
            get { return this._connection.State; }
        }

        /// <summary>
        /// change the database.
        /// </summary>
        /// <param name="databaseName">The new database name.</param>
        public override void ChangeDatabase(string databaseName)
        {
            this._connection.ChangeDatabase(databaseName);
        }

        /// <summary>
        /// close the connection.
        /// </summary>
        public override void Close()
        {
            this._connection.Close();
        }

        /// <summary>
        /// enlist the transaction.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        public override void EnlistTransaction(System.Transactions.Transaction transaction)
        {
            this._connection.EnlistTransaction(transaction);
        }

        /// <summary>
        /// get the schema.
        /// </summary>
        /// <returns>The <see cref="DataTable"/>.</returns>
        public override DataTable GetSchema()
        {
            return this._connection.GetSchema();
        }

        /// <summary>
        /// get the schema.
        /// </summary>
        /// <param name="collectionName">The collection name.</param>
        /// <returns>The <see cref="DataTable"/>.</returns>
        public override DataTable GetSchema(string collectionName)
        {
            return this._connection.GetSchema(collectionName);
        }

        /// <summary>
        /// get the schema.
        /// </summary>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="restrictionValues">The restriction values.</param>
        /// <returns>The <see cref="DataTable"/>.</returns>
        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            return this._connection.GetSchema(collectionName, restrictionValues);
        }

        /// <summary>
        /// open the connection
        /// </summary>
        public override void Open()
        {
            this._connection.Open();
        }

        /// <summary>
        /// begin the database transaction.
        /// </summary>
        /// <param name="isolationLevel">The isolation level.</param>
        /// <returns>The <see cref="DbTransaction"/>.</returns>
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new ProfiledDbTransaction(this._connection.BeginTransaction(isolationLevel), this);
        }

        /// <summary>
        /// create the database command.
        /// </summary>
        /// <returns>The <see cref="DbCommand"/>.</returns>
        protected override DbCommand CreateDbCommand()
        {
            return new ProfiledDbCommand(this._connection.CreateCommand(), this, this._profiler);
        }

        /// <summary>
        /// dispose the underlying connection.
        /// </summary>
        /// <param name="disposing">false if pre-empted from a <c>finalizer</c></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && this._connection != null)
            {
                this._connection.StateChange -= this.StateChangeHandler;
                this._connection.Dispose();
            }
            this._connection = null;
            this._profiler = null;
            base.Dispose(disposing);
        }

        /// <summary>
        /// The state change handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="stateChangeEventArguments">The state change event arguments.</param>
        private void StateChangeHandler(object sender, StateChangeEventArgs stateChangeEventArguments)
        {
            this.OnStateChange(stateChangeEventArguments);
        }

        /// <summary>
        /// create a clone.
        /// </summary>
        /// <returns>The <see cref="ProfiledDbConnection"/>.</returns>
        public ProfiledDbConnection Clone()
        {
            var tail = this._connection as ICloneable;
            if (tail == null) throw new NotSupportedException("Underlying " + this._connection.GetType().Name + " is not cloneable");
            return new ProfiledDbConnection((DbConnection)tail.Clone(), this._profiler);
        }

        /// <summary>
        /// create a clone.
        /// </summary>
        /// <returns>The <see cref="object"/>.</returns>
        object ICloneable.Clone()
        {
            return this.Clone();
        }
    }
}
