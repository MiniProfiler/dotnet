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
    {
        private IDbCommand _command;
        private IDbConnection _connection;
        private IDbProfiler _profiler;
        private IDbTransaction _transaction;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleProfiledCommand"/> class, creating a new wrapped command.
        /// </summary>
        /// <param name="command">The wrapped command.</param>
        /// <param name="connection">The wrapped connection the command is attached to.</param>
        /// <param name="profiler">The profiler to use.</param>
        /// <exception cref="ArgumentNullException">Throws then the <paramref name="command"/> is <c>null</c>.</exception>
        public SimpleProfiledCommand(IDbCommand command, IDbConnection connection, IDbProfiler profiler)
        {
            _command = command ?? throw new ArgumentNullException(nameof(command));
            _connection = connection;

            if (profiler != null)
            {
                _profiler = profiler;
            }
        }

        /// <summary>
        /// Prepare the command.
        /// </summary>
        public void Prepare() => _command.Prepare();

        /// <summary>
        /// Cancel the command.
        /// </summary>
        public void Cancel() => _command.Cancel();

        /// <summary>
        /// Create a new parameter.
        /// </summary>
        /// <returns>The <see cref="IDbDataParameter"/>.</returns>
        public IDbDataParameter CreateParameter() => _command.CreateParameter();

        /// <summary>
        /// Execute a non query.
        /// </summary>
        /// <returns>The <see cref="int"/>.</returns>
        public int ExecuteNonQuery() => ProfileWith(SqlExecuteType.NonQuery, _command.ExecuteNonQuery);

        /// <summary>
        /// Execute the reader.
        /// </summary>
        /// <returns>The <see cref="IDataReader"/>.</returns>
        public IDataReader ExecuteReader() =>
            ProfileWith(SqlExecuteType.Reader, () => new SimpleProfiledDataReader(_command.ExecuteReader(), _profiler));

        /// <summary>
        /// Execute the reader.
        /// </summary>
        /// <param name="behavior">The <c>behavior</c>.</param>
        /// <returns>the active reader.</returns>
        public IDataReader ExecuteReader(CommandBehavior behavior) =>
            ProfileWith(SqlExecuteType.Reader, () => new SimpleProfiledDataReader(_command.ExecuteReader(behavior), _profiler));

        /// <summary>
        /// Execute and return a scalar.
        /// </summary>
        /// <returns>the scalar value.</returns>
        public object ExecuteScalar() => ProfileWith(SqlExecuteType.Scalar, () => _command.ExecuteScalar());

        /// <summary>
        /// profile with results.
        /// </summary>
        /// <param name="type">The type of execution.</param>
        /// <param name="func">A function to execute against the profile result.</param>
        /// <typeparam name="TResult">the type of result to return.</typeparam>
        private TResult ProfileWith<TResult>(SqlExecuteType type, Func<TResult> func)
        {
            if (_profiler?.IsActive != true)
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
            get => _connection;
            set
            {
                if (MiniProfiler.Current != null)
                {
                    _profiler = MiniProfiler.Current;
                }

                _connection = value;

                _command.Connection = value is SimpleProfiledConnection wrapped ? wrapped.WrappedConnection : value;
            }
        }

        /// <summary>
        /// Gets or sets the transaction.
        /// </summary>
        public IDbTransaction Transaction
        {
            get => _transaction;
            set
            {
                _transaction = value;
                _command.Transaction = value is SimpleProfiledTransaction wrapped ? wrapped.WrappedTransaction : value;
            }
        }

        /// <summary>
        /// Gets or sets the command text.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Handled elsewhere.")]
        public string CommandText
        {
            get => _command.CommandText;
            set => _command.CommandText = value;
        }

        /// <summary>
        /// Gets or sets the command timeout.
        /// </summary>
        public int CommandTimeout
        {
            get => _command.CommandTimeout;
            set => _command.CommandTimeout = value;
        }

        /// <summary>
        /// Gets or sets the command type.
        /// </summary>
        public CommandType CommandType
        {
            get => _command.CommandType;
            set => _command.CommandType = value;
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        public IDataParameterCollection Parameters => _command.Parameters;

        /// <summary>
        /// Gets or sets the updated row source.
        /// </summary>
        public UpdateRowSource UpdatedRowSource
        {
            get => _command.UpdatedRowSource;
            set => _command.UpdatedRowSource = value;
        }

        /// <summary>
        /// Dispose the command / connection and profiler.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the command / connection and profiler.
        /// </summary>
        /// <param name="disposing">false if the dispose is called from a <c>finalizer</c></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing) _command?.Dispose();

            _command = null;
            _connection = null;
            _profiler = null;
        }
    }
}
