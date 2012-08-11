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
        /// <param name="transaction">The transaction to wrap</param>
        /// <param name="connection">The wrapped connection this transaction is attached to</param>
        public SimpleProfiledTransaction(IDbTransaction transaction, SimpleProfiledConnection connection)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (connection == null) throw new ArgumentNullException("connection");
            _transaction = transaction;
            _connection = connection;
        }

        /// <summary>
        /// The internal wrapped transaction
        /// </summary>
        public IDbTransaction WrappedTransaction
        {
            get { return _transaction; }
        }

        public IDbConnection Connection
        {
            get { return _connection; }
        }

        public IsolationLevel IsolationLevel
        {
            get { return _transaction.IsolationLevel; }
        }

        public void Commit()
        {
            _transaction.Commit();
        }

        public void Rollback()
        {
            _transaction.Rollback();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && _transaction != null)
            {
                _transaction.Dispose();
            }
        }
    }
}
