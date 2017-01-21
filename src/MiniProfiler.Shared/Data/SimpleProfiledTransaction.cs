using System;
using System.Data;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// A general implementation of <see cref="IDbTransaction"/> that is used to
    /// wrap profiling information around calls to it.
    /// </summary>
    public class SimpleProfiledTransaction : IDbTransaction
    {
        private readonly SimpleProfiledConnection _connection;

        /// <summary>
        /// Creates a new wrapped <see cref="IDbTransaction"/>
        /// </summary>
        public SimpleProfiledTransaction(IDbTransaction transaction, SimpleProfiledConnection connection)
        {
            WrappedTransaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Gets the internal wrapped transaction
        /// </summary>
        public IDbTransaction WrappedTransaction { get; }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        public IDbConnection Connection => _connection;

        /// <summary>
        /// Gets the isolation level.
        /// </summary>
        public IsolationLevel IsolationLevel => WrappedTransaction.IsolationLevel;

        /// <summary>
        /// Commits the database transaction.
        /// </summary>
        public void Commit() => WrappedTransaction.Commit();

        /// <summary>
        /// Rolls back a transaction from a pending state.
        /// </summary>
        public void Rollback() => WrappedTransaction.Rollback();

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="IDbTransaction"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="IDbTransaction"/>.
        /// </summary>
        /// <param name="disposing">false if being called from a <c>finalizer</c></param>
        private void Dispose(bool disposing)
        {
            if (disposing) WrappedTransaction?.Dispose();
        }
    }
}
