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
        /// <summary>
        /// The transaction.
        /// </summary>
        private readonly IDbTransaction _transaction;

        /// <summary>
        /// The connection.
        /// </summary>
        private readonly SimpleProfiledConnection _connection;

        /// <summary>
        /// Initialises a new instance of the <see cref="SimpleProfiledTransaction"/> class. 
        /// Creates a new wrapped <see cref="IDbTransaction"/>
        /// </summary>
        /// <param name="transaction">
        /// The transaction to wrap
        /// </param>
        /// <param name="connection">
        /// The wrapped connection this transaction is attached to
        /// </param>
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
        public IDbTransaction WrappedTransaction
        {
            get { return _transaction; }
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        public IDbConnection Connection
        {
            get { return _connection; }
        }

        /// <summary>
        /// Gets the isolation level.
        /// </summary>
        public IsolationLevel IsolationLevel
        {
            get { return _transaction.IsolationLevel; }
        }

        /// <summary>
        /// commit the transaction.
        /// </summary>
        public void Commit()
        {
            _transaction.Commit();
        }

        /// <summary>
        /// rollback the transaction
        /// </summary>
        public void Rollback()
        {
            _transaction.Rollback();
        }

        /// <summary>
        /// dispose the command / connection and profiler.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// dispose the command / connection and profiler.
        /// </summary>
        /// <param name="disposing">false if the dispose is called from a <c>finalizer</c></param>
        private void Dispose(bool disposing)
        {
            if (disposing && _transaction != null)
            {
                _transaction.Dispose();
            }
        }
    }
}
