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
        private IDbConnection? _connection;
        private IDbProfiler? _profiler;
        private IDbTransaction? _transaction;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleProfiledCommand"/> class, creating a new wrapped command.
        /// </summary>
        /// <param name="command">The wrapped command.</param>
        /// <param name="connection">The wrapped connection the command is attached to.</param>
        /// <param name="profiler">The profiler to use.</param>
        /// <exception cref="ArgumentNullException">Throws then the <paramref name="command"/> is <c>null</c>.</exception>
        public SimpleProfiledCommand(IDbCommand command, IDbConnection connection, IDbProfiler? profiler)
        {
            _command = command ?? throw new ArgumentNullException(nameof(command));
            _connection = connection;

            if (profiler != null)
            {
                _profiler = profiler;
            }
        }

        /// <inheritdoc cref="IDbCommand.Prepare()"/>
        public void Prepare() => _command.Prepare();

        /// <inheritdoc cref="IDbCommand.Cancel()"/>
        public void Cancel() => _command.Cancel();

        /// <inheritdoc cref="IDbCommand.CreateParameter()"/>
        public IDbDataParameter CreateParameter() => _command.CreateParameter();

        /// <inheritdoc cref="IDbCommand.ExecuteNonQuery()"/>
        public int ExecuteNonQuery() => ProfileWith(SqlExecuteType.NonQuery, _command.ExecuteNonQuery);

        /// <inheritdoc cref="IDbCommand.ExecuteReader()"/>
        public IDataReader ExecuteReader() =>
            ProfileWith(SqlExecuteType.Reader, () => new SimpleProfiledDataReader(_command.ExecuteReader(), _profiler));

        /// <inheritdoc cref="IDbCommand.ExecuteReader(CommandBehavior)"/>
        public IDataReader ExecuteReader(CommandBehavior behavior) =>
            ProfileWith(SqlExecuteType.Reader, () => new SimpleProfiledDataReader(_command.ExecuteReader(behavior), _profiler));

        /// <inheritdoc cref="IDbCommand.ExecuteScalar()"/>
        public object? ExecuteScalar() => ProfileWith(SqlExecuteType.Scalar, () => _command.ExecuteScalar());

        /// <summary>
        /// Profile with results.
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

        /// <inheritdoc cref="IDbCommand.Connection"/>
        public IDbConnection? Connection
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

        /// <inheritdoc cref="IDbCommand.Transaction"/>
        public IDbTransaction? Transaction
        {
            get => _transaction;
            set
            {
                _transaction = value;
                _command.Transaction = value is SimpleProfiledTransaction wrapped ? wrapped.WrappedTransaction : value;
            }
        }

        /// <inheritdoc cref="IDbCommand.CommandText"/>
        [AllowNull]
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Handled elsewhere.")]
        public string CommandText
        {
            get => _command.CommandText;
            set => _command.CommandText = value;
        }

        /// <inheritdoc cref="IDbCommand.CommandTimeout"/>
        public int CommandTimeout
        {
            get => _command.CommandTimeout;
            set => _command.CommandTimeout = value;
        }

        /// <inheritdoc cref="IDbCommand.CommandType"/>
        public CommandType CommandType
        {
            get => _command.CommandType;
            set => _command.CommandType = value;
        }

        /// <inheritdoc cref="IDbCommand.Parameters"/>
        public IDataParameterCollection Parameters => _command.Parameters;

        /// <inheritdoc cref="IDbCommand.UpdatedRowSource"/>
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

            _command = null!;
            _connection = null!;
            _profiler = null;
        }
    }
}
