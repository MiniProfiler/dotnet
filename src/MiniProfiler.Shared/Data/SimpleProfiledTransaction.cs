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
        /// commit the transaction.
        /// </summary>
        public void Commit() => WrappedTransaction.Commit();

        /// <summary>
        /// rollback the transaction
        /// </summary>
        public void Rollback() => WrappedTransaction.Rollback();

        /// <summary>
        /// dispose the command / connection and profiler.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing) WrappedTransaction?.Dispose();
        }
    }
}
