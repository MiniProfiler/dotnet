using System;
using System.Data;
using System.Data.Common;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// The profiled database transaction.
    /// </summary>
    public class ProfiledDbTransaction : DbTransaction
    {
        private ProfiledDbConnection _connection;
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
            _transaction = transaction;
            _connection = connection;
        }

        /// <summary>
        /// Gets the database connection.
        /// </summary>
        protected override DbConnection DbConnection => _connection;

        /// <summary>
        /// Gets the wrapped transaction.
        /// </summary>
        public DbTransaction WrappedTransaction => _transaction;

        /// <summary>
        /// Gets the isolation level.
        /// </summary>
        public override IsolationLevel IsolationLevel => _transaction.IsolationLevel;

        /// <summary>
        /// Commit the transaction.
        /// </summary>
        public override void Commit() => _transaction.Commit();

        /// <summary>
        /// Rollback the transaction.
        /// </summary>
        public override void Rollback() => _transaction.Rollback();

        /// <summary>
        /// dispose the transaction and connection.
        /// </summary>
        /// <param name="disposing">false if being called from a <c>finalizer</c></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _transaction != null)
            {
                _transaction.Dispose();
            }
            _transaction = null;
            _connection = null;
            base.Dispose(disposing);
        }
    }
}
