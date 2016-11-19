using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// A general implementation of <see cref="IDbCommand"/> that uses an <see cref="IDbProfiler"/>
    /// to collect profiling information.
    /// </summary>
    public class SimpleProfiledCommand : IDbCommand
#if NET45
, ICloneable
#endif
    {
        /// <summary>
        /// The command.
        /// </summary>
        private IDbCommand _command;

        /// <summary>
        /// The connection.
        /// </summary>
        private IDbConnection _connection;

        /// <summary>
        /// The profiler.
        /// </summary>
        private IDbProfiler _profiler;

        /// <summary>
        /// The transaction.
        /// </summary>
        private IDbTransaction _transaction;

        /// <summary>
        /// Initialises a new instance of the <see cref="SimpleProfiledCommand"/> class. 
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

        /// <summary>
        /// prepare the command.
        /// </summary>
        public void Prepare()
        {
            _command.Prepare();
        }

        /// <summary>
        /// cancel the command.
        /// </summary>
        public void Cancel()
        {
            _command.Cancel();
        }

        /// <summary>
        /// create a new parameter.
        /// </summary>
        /// <returns>The <see cref="IDbDataParameter"/>.</returns>
        public IDbDataParameter CreateParameter()
        {
            return _command.CreateParameter();
        }

        /// <summary>
        /// execute a non query.
        /// </summary>
        /// <returns>The <see cref="int"/>.</returns>
        public int ExecuteNonQuery()
        {
            return ProfileWith(SqlExecuteType.NonQuery, _command.ExecuteNonQuery);
        }

        /// <summary>
        /// execute the reader.
        /// </summary>
        /// <returns>The <see cref="IDataReader"/>.</returns>
        public IDataReader ExecuteReader()
        {
            return ProfileWith(
                SqlExecuteType.Reader, () => new SimpleProfiledDataReader(_command.ExecuteReader(), _profiler));
        }

        /// <summary>
        /// execute the reader.
        /// </summary>
        /// <param name="behavior">The <c>behavior</c>.</param>
        /// <returns>the active reader.</returns>
        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            return ProfileWith(
                SqlExecuteType.Reader, () => new SimpleProfiledDataReader(_command.ExecuteReader(behavior), _profiler));
        }

        /// <summary>
        /// execute and return a scalar.
        /// </summary>
        /// <returns>the scalar value.</returns>
        public object ExecuteScalar()
        {
            return ProfileWith(SqlExecuteType.Scalar, () => _command.ExecuteScalar());
        }

        /// <summary>
        /// profile with results.
        /// </summary>
        /// <param name="type">The type of execution.</param>
        /// <param name="func">a function to execute against against the profile result</param>
        /// <typeparam name="TResult">the type of result to return.</typeparam>
        private TResult ProfileWith<TResult>(SqlExecuteType type, Func<TResult> func)
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

        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        public IDbConnection Connection
        {
            get
            {
                return _connection;
            }
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

        /// <summary>
        /// Gets or sets the transaction.
        /// </summary>
        public IDbTransaction Transaction
        {
            get
            {
                return _transaction;
            }
            set
            {
                _transaction = value;
                var wrapped = value as SimpleProfiledTransaction;
                _command.Transaction = wrapped != null ? wrapped.WrappedTransaction : value;
            }
        }

        /// <summary>
        /// Gets or sets the command text.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Handled elsewhere.")]
        public string CommandText
        {
            get { return _command.CommandText; }
            set { _command.CommandText = value; }
        }

        /// <summary>
        /// Gets or sets the command timeout.
        /// </summary>
        public int CommandTimeout
        {
            get { return _command.CommandTimeout; }
            set { _command.CommandTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the command type.
        /// </summary>
        public CommandType CommandType
        {
            get { return _command.CommandType; }
            set { _command.CommandType = value; }
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        public IDataParameterCollection Parameters
        {
            get { return _command.Parameters; }
        }

        /// <summary>
        /// Gets or sets the updated row source.
        /// </summary>
        public UpdateRowSource UpdatedRowSource
        {
            get { return _command.UpdatedRowSource; }
            set { _command.UpdatedRowSource = value; }
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
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _command != null) _command.Dispose();

            _command = null;
            _connection = null;
            _profiler = null;
        }

#if NET45
        /// <summary>
        /// clone the command.
        /// </summary>
        /// <returns>The <see cref="object"/>.</returns>
        public object Clone()
        {
            var tail = _command as ICloneable;
            if (tail == null)
                throw new NotSupportedException("Underlying " + _command.GetType().Name + " is not cloneable.");

            return new SimpleProfiledCommand((IDbCommand)tail.Clone(), _connection, _profiler);
        }
#endif
    } 
}
