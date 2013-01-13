#pragma warning disable 1591 // xml doc comments warnings

namespace StackExchange.Profiling.Data
{
    using System;
    using System.Data;
    using System.Data.Common;

    /// <summary>
    /// The profiled database transaction.
    /// </summary>
    public class ProfiledDbTransaction : DbTransaction
    {
        /// <summary>
        /// The connection.
        /// </summary>
        private ProfiledDbConnection _connection;

        /// <summary>
        /// The transaction.
        /// </summary>
        private DbTransaction _transaction;

        /// <summary>
        /// Initialises a new instance of the <see cref="ProfiledDbTransaction"/> class.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="connection">The connection.</param>
        public ProfiledDbTransaction(DbTransaction transaction, ProfiledDbConnection connection)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (connection == null) throw new ArgumentNullException("connection");
            this._transaction = transaction;
            this._connection = connection;
        }

        /// <summary>
        /// Gets the database connection.
        /// </summary>
        protected override DbConnection DbConnection
        {
            get { return this._connection; }
        }

        /// <summary>
        /// Gets the wrapped transaction.
        /// </summary>
        public DbTransaction WrappedTransaction
        {
            get { return this._transaction; }
        }

        /// <summary>
        /// Gets the isolation level.
        /// </summary>
        public override IsolationLevel IsolationLevel
        {
            get { return this._transaction.IsolationLevel; }
        }

        /// <summary>
        /// commit the transaction.
        /// </summary>
        public override void Commit()
        {
            this._transaction.Commit();
        }

        /// <summary>
        /// rollback the transaction
        /// </summary>
        public override void Rollback()
        {
            this._transaction.Rollback();
        }

        /// <summary>
        /// dispose the transaction and connection.
        /// </summary>
        /// <param name="disposing">false if being called from a <c>finalizer</c></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && this._transaction != null)
            {
                this._transaction.Dispose();
            }
            this._transaction = null;
            this._connection = null;
            base.Dispose(disposing);
        }
    }
}
