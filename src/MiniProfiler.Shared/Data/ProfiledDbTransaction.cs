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
        /// <exception cref="ArgumentNullException">Throws when the <paramref name="transaction"/> or <paramref name="connection"/> is <c>null</c>.</exception>
        public ProfiledDbTransaction(DbTransaction transaction, ProfiledDbConnection connection)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
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
        /// Commits the database transaction.
        /// </summary>
        public override void Commit() => _transaction.Commit();

        /// <summary>
        /// Rolls back a transaction from a pending state.
        /// </summary>
        public override void Rollback() => _transaction.Rollback();

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="DbTransaction"/>.
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
