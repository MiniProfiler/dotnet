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
        private readonly IDbTransaction _transaction;
        private readonly SimpleProfiledConnection _connection;

        /// <summary>
        /// Creates a new wrapped <see cref="IDbTransaction"/>
        /// </summary>
        public SimpleProfiledTransaction(IDbTransaction transaction, SimpleProfiledConnection connection)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (connection == null) throw new ArgumentNullException("connection");
            _transaction = transaction;
            _connection = connection;
        }

        /// <summary>
        /// Gets the internal wrapped transaction
        /// </summary>
        public IDbTransaction WrappedTransaction => _transaction;

        /// <summary>
        /// Gets the connection.
        /// </summary>
        public IDbConnection Connection => _connection;

        /// <summary>
        /// Gets the isolation level.
        /// </summary>
        public IsolationLevel IsolationLevel => _transaction.IsolationLevel;

        /// <summary>
        /// commit the transaction.
        /// </summary>
        public void Commit() => _transaction.Commit();

        /// <summary>
        /// rollback the transaction
        /// </summary>
        public void Rollback() => _transaction.Rollback();

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
            if (disposing) _transaction?.Dispose();
        }
    }
}
