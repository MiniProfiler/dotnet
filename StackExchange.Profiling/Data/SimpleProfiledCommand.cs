using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// A general implementation of <see cref="IDbCommand"/> that uses an <see cref="IDbProfiler"/>
    /// to collect profiling information.
    /// </summary>
    public class SimpleProfiledCommand : IDbCommand, ICloneable
    {
        private IDbCommand _command;
        private IDbConnection _connection;
        private IDbProfiler _profiler;
        private IDbTransaction _transaction;

        /// <summary>
        /// Creates a new wrapped command
        /// </summary>
        /// <param name="command">The wrapped command</param>
        /// <param name="connection">The wrapped connection the command is attached to</param>
        /// <param name="profiler">The profiler to use</param>
        public SimpleProfiledCommand(IDbCommand command, IDbConnection connection, IDbProfiler profiler)
        {
            if (command == null) throw new ArgumentNullException("command");

            _command = command;
            _connection = connection;

            if (profiler != null)
            {
                _profiler = profiler;
            }
        }

        public void Prepare()
        {
            _command.Prepare();
        }

        public void Cancel()
        {
            _command.Cancel();
        }

        public IDbDataParameter CreateParameter()
        {
            return _command.CreateParameter();
        }

        public int ExecuteNonQuery()
        {
            return ProfileWith(ExecuteType.NonQuery, _command.ExecuteNonQuery);
        }

        public IDataReader ExecuteReader()
        {
            return ProfileWith(ExecuteType.Reader,
                               () => new SimpleProfiledDataReader(_command.ExecuteReader(), _profiler));
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            return ProfileWith(ExecuteType.Reader,
                               () => new SimpleProfiledDataReader(_command.ExecuteReader(behavior), _profiler));
        }

        public object ExecuteScalar()
        {
            return ProfileWith(ExecuteType.Scalar, () => _command.ExecuteScalar());
        }

        private TResult ProfileWith<TResult>(ExecuteType type, Func<TResult> func)
        {
            if (_profiler == null || !_profiler.IsActive)
            {
                return func();
            }

            TResult result;
            _profiler.ExecuteStart(this, type);
            try
            {
                result = func();
            }
            catch (Exception e)
            {
                _profiler.OnError(this, type, e);
                throw;
            }
            finally
            {
                _profiler.ExecuteFinish(this, type, null);
            }
            return result;
        }

        public IDbConnection Connection
        {
            get { return _connection; }
            set
            {
                if (MiniProfiler.Current != null)
                {
                    _profiler = MiniProfiler.Current;
                }

                _connection = value;

                var wrapped = value as SimpleProfiledConnection;
                _command.Connection = wrapped != null ? wrapped.WrappedConnection : value;
            }
        }

        public IDbTransaction Transaction
        {
            get { return _transaction; }
            set
            {
                _transaction = value;
                var wrapped = value as SimpleProfiledTransaction;
                _command.Transaction = wrapped != null ? wrapped.WrappedTransaction : value;
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Handled elsewhere.")]
        public string CommandText
        {
            get { return _command.CommandText; }
            set { _command.CommandText = value; }
        }

        public int CommandTimeout
        {
            get { return _command.CommandTimeout; }
            set { _command.CommandTimeout = value; }
        }

        public CommandType CommandType
        {
            get { return _command.CommandType; }
            set { _command.CommandType = value; }
        }

        public IDataParameterCollection Parameters
        {
            get { return _command.Parameters; }
        }

        public UpdateRowSource UpdatedRowSource
        {
            get { return _command.UpdatedRowSource; }
            set { _command.UpdatedRowSource = value; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _command != null) _command.Dispose();

            _command = null;
            _connection = null;
            _profiler = null;
        }

        public object Clone()
        {
            var tail = _command as ICloneable;
            if (tail == null)
                throw new NotSupportedException("Underlying " + _command.GetType().Name + " is not cloneable.");

            return new SimpleProfiledCommand((IDbCommand)tail.Clone(), _connection, _profiler);
        }
    } 
}
