using System;
using System.Data;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// A general implementation of IDbConnection that uses an <see cref="IDbProfiler"/>
    /// to collect profiling information.
    /// </summary>
    public class SimpleProfiledConnection : IDbConnection
    {
        private IDbProfiler _profiler;
        private IDbConnection _connection;

        public string ConnectionString
        {
            get { return _connection.ConnectionString; }
            set { _connection.ConnectionString = value; }
        }

        public int ConnectionTimeout
        {
            get { return _connection.ConnectionTimeout; }
        }

        public string Database
        {
            get { return _connection.Database; }
        }

        public ConnectionState State
        {
            get { return _connection.State; }
        }

        /// <summary>
        /// The internally wrapped <see cref="IDbConnection"/>
        /// </summary>
        public IDbConnection WrappedConnection
        {
            get { return _connection; }
        }

        /// <summary>
        /// Creates a simple profiled connection instance.
        /// </summary>
        /// <param name="connection">The database connection to wrap</param>
        /// <param name="profiler">The profiler to use</param>
        public SimpleProfiledConnection(IDbConnection connection, IDbProfiler profiler)
        {
            _connection = connection;
            if (profiler != null)
            {
                _profiler = profiler;
            }
        }

        public IDbTransaction BeginTransaction()
        {
            return new SimpleProfiledTransaction(_connection.BeginTransaction(), this);
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return new SimpleProfiledTransaction(_connection.BeginTransaction(il), this);
        }

        public void ChangeDatabase(string databaseName)
        {
            _connection.ChangeDatabase(databaseName);
        }

        public IDbCommand CreateCommand()
        {
            return new SimpleProfiledCommand(_connection.CreateCommand(), this, _profiler);
        }

        public void Close()
        {
            _connection.Close();
        }

        public void Open()
        {
            _connection.Open();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && _connection != null && _connection.State != ConnectionState.Closed)
            {
                _connection.Dispose();
            }
            _connection = null;
            _profiler = null;
        }
    }
}
