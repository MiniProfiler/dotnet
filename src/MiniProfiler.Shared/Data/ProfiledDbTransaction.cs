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
        /// Initializes a new instance of the <see cref="ProfiledDbTransaction"/> class.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="connection">The connection.</param>
        /// <exception cref="ArgumentNullException">Throws when the <paramref name="transaction"/> or <paramref name="connection"/> is <c>null</c>.</exception>
        public ProfiledDbTransaction(DbTransaction transaction, ProfiledDbConnection connection)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <inheritdoc cref="DbTransaction.DbConnection"/>
        protected override DbConnection DbConnection => _connection;

        /// <summary>
        /// Gets the wrapped transaction.
        /// </summary>
        public DbTransaction WrappedTransaction => _transaction;

        /// <inheritdoc cref="DbTransaction.IsolationLevel"/>
        public override IsolationLevel IsolationLevel => _transaction.IsolationLevel;

        /// <inheritdoc cref="DbTransaction.Commit()"/>
        public override void Commit() => _transaction.Commit();

        /// <inheritdoc cref="DbTransaction.Rollback()"/>
        public override void Rollback() => _transaction.Rollback();

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="DbTransaction"/>.
        /// </summary>
        /// <param name="disposing">false if being called from a <c>finalizer</c></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _transaction?.Dispose();
            }
            _transaction = null!;
            _connection = null!;
            base.Dispose(disposing);
        }
    }
}
