﻿using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// The profiled database command.
    /// </summary>
    [System.ComponentModel.DesignerCategory("")]
    public partial class ProfiledDbCommand : DbCommand
    {
        private static Link<Type, Action<IDbCommand, bool>> bindByNameCache;
        private DbCommand _command;
        private DbConnection _connection;
        private DbTransaction _transaction;
        private IDbProfiler _profiler;
        private bool _bindByName;

        /// <summary>
        /// Whether to always wrap data readers, even if there isn't an active profiler on this connect.
        /// This allows depending on overrides for things inheriting from <see cref="ProfiledDbDataReader"/> to actually execute.
        /// </summary>
        protected virtual bool AlwaysWrapReaders => false;

        /// <summary>
        /// Gets or sets a value indicating whether or not to bind by name.
        /// If the underlying command supports BindByName, this sets/clears the underlying
        /// implementation accordingly. This is required to support OracleCommand from Dapper
        /// </summary>
        public bool BindByName
        {
            get => _bindByName;
            set
            {
                if (_bindByName != value)
                {
                    if (_command != null)
                    {
                        GetBindByName(_command.GetType())?.Invoke(_command, value);
                    }

                    _bindByName = value;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfiledDbCommand"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="profiler">The profiler.</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="command"/> is <c>null</c>.</exception>
        public ProfiledDbCommand(DbCommand command, DbConnection connection, IDbProfiler profiler)
        {
            _command = command ?? throw new ArgumentNullException(nameof(command));

            if (connection != null)
            {
                _connection = connection;
                UnwrapAndAssignConnection(connection);
            }

            if (profiler != null)
            {
                _profiler = profiler;
            }
        }

        /// <summary>
        /// Get the binding name.
        /// </summary>
        /// <param name="commandType">The command type.</param>
        /// <returns>The <see cref="Action"/>.</returns>
        private static Action<IDbCommand, bool> GetBindByName(Type commandType)
        {
            if (commandType == null) return null; // GIGO
            if (Link<Type, Action<IDbCommand, bool>>.TryGet(bindByNameCache, commandType, out var action))
            {
                return action;
            }

            var prop = commandType.GetProperty("BindByName", BindingFlags.Public | BindingFlags.Instance);
            action = null;
            ParameterInfo[] indexers;
            MethodInfo setter;
            if (prop?.CanWrite == true && prop.PropertyType == typeof(bool)
                && ((indexers = prop.GetIndexParameters()) == null || indexers.Length == 0)
                && (setter = prop.GetSetMethod()) != null)
            {
                var method = new DynamicMethod(commandType.Name + "_BindByName", null, new[] { typeof(IDbCommand), typeof(bool) });
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, commandType);
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(OpCodes.Callvirt, setter, null);
                il.Emit(OpCodes.Ret);
                action = (Action<IDbCommand, bool>)method.CreateDelegate(typeof(Action<IDbCommand, bool>));
            }

            // cache it            
            Link<Type, Action<IDbCommand, bool>>.TryAdd(ref bindByNameCache, commandType, ref action);
            return action;
        }

        /// <summary>
        /// Gets or sets the text command to run against the data source.
        /// </summary>
        public override string CommandText
        {
            get => _command.CommandText;
            set => _command.CommandText = value;
        }

        /// <summary>
        /// Gets or sets the command timeout.
        /// </summary>
        public override int CommandTimeout
        {
            get => _command.CommandTimeout;
            set => _command.CommandTimeout = value;
        }

        /// <summary>
        /// Gets or sets the command type.
        /// </summary>
        public override CommandType CommandType
        {
            get => _command.CommandType;
            set => _command.CommandType = value;
        }

        /// <summary>
        /// Gets or sets the database connection.
        /// </summary>
        protected override DbConnection DbConnection
        {
            get => _connection;
            set
            {
                _connection = value;
                UnwrapAndAssignConnection(value);
            }
        }

        private void UnwrapAndAssignConnection(DbConnection value)
        {
            if (value is ProfiledDbConnection profiledConn)
            {
                _profiler = profiledConn.Profiler;
                _command.Connection = profiledConn.WrappedConnection;
            }
            else
            {
                _command.Connection = value;
            }
        }

        /// <summary>
        /// Gets the database parameter collection.
        /// </summary>
        protected override DbParameterCollection DbParameterCollection => _command.Parameters;

        /// <summary>
        /// Gets or sets the database transaction.
        /// </summary>
        protected override DbTransaction DbTransaction
        {
            get => _transaction;
            set
            {
                _transaction = value;
                _command.Transaction = value is ProfiledDbTransaction awesomeTran ? awesomeTran.WrappedTransaction : value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the command is design time visible.
        /// </summary>
        public override bool DesignTimeVisible
        {
            get => _command.DesignTimeVisible;
            set => _command.DesignTimeVisible = value;
        }

        /// <summary>
        /// Gets or sets the updated row source.
        /// </summary>
        public override UpdateRowSource UpdatedRowSource
        {
            get => _command.UpdatedRowSource;
            set => _command.UpdatedRowSource = value;
        }

        /// <summary>
        /// Creates a wrapper data reader for <see cref="ExecuteDbDataReader"/> and <see cref="ExecuteDbDataReaderAsync"/> />
        /// </summary>
        protected virtual DbDataReader CreateDbDataReader(DbDataReader original, CommandBehavior behavior, IDbProfiler profiler)
            => new ProfiledDbDataReader(original, behavior, profiler);

        /// <summary>
        /// Executes a database data reader.
        /// </summary>
        /// <param name="behavior">The command behavior to use.</param>
        /// <returns>The resulting <see cref="DbDataReader"/>.</returns>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            DbDataReader result = null;
            if (_profiler?.IsActive != true)
            {
                result = _command.ExecuteReader(behavior);
                return AlwaysWrapReaders ? CreateDbDataReader(result, behavior, null) : result;
            }

            _profiler.ExecuteStart(this, SqlExecuteType.Reader);
            try
            {
                result = _command.ExecuteReader(behavior);
                result = CreateDbDataReader(result, behavior, _profiler);
            }
            catch (Exception e)
            {
                _profiler.OnError(this, SqlExecuteType.Reader, e);
                throw;
            }
            finally
            {
                _profiler.ExecuteFinish(this, SqlExecuteType.Reader, result);
            }

            return result;
        }

        /// <summary>
        /// Executes a database data reader asynchronously.
        /// </summary>
        /// <param name="behavior">The command behavior to use.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for this async operation.</param>
        /// <returns>The resulting <see cref="DbDataReader"/>.</returns>
        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            DbDataReader result = null;
            if (_profiler?.IsActive != true)
            {
                result = await _command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
                return AlwaysWrapReaders ? CreateDbDataReader(result, behavior, null) : result;
            }

            _profiler.ExecuteStart(this, SqlExecuteType.Reader);
            try
            {
                result = await _command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
                result = CreateDbDataReader(result, behavior, _profiler);
            }
            catch (Exception e)
            {
                _profiler.OnError(this, SqlExecuteType.Reader, e);
                throw;
            }
            finally
            {
                _profiler.ExecuteFinish(this, SqlExecuteType.Reader, result);
            }

            return result;
        }

        /// <summary>
        /// Executes a SQL statement against a connection object.
        /// </summary>
        /// <returns>The number of rows affected.</returns>
        public override int ExecuteNonQuery()
        {
            if (_profiler?.IsActive != true)
            {
                return _command.ExecuteNonQuery();
            }

            int result;
            _profiler.ExecuteStart(this, SqlExecuteType.NonQuery);
            try
            {
                result = _command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                _profiler.OnError(this, SqlExecuteType.NonQuery, e);
                throw;
            }
            finally
            {
                _profiler.ExecuteFinish(this, SqlExecuteType.NonQuery, null);
            }

            return result;
        }

        /// <summary>
        /// Asynchronously executes a SQL statement against a connection object asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for this async operation.</param>
        /// <returns>The number of rows affected.</returns>
        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            if (_profiler?.IsActive != true)
            {
                return await _command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            int result;
            _profiler.ExecuteStart(this, SqlExecuteType.NonQuery);
            try
            {
                result = await _command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _profiler.OnError(this, SqlExecuteType.NonQuery, e);
                throw;
            }
            finally
            {
                _profiler.ExecuteFinish(this, SqlExecuteType.NonQuery, null);
            }

            return result;
        }

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query. 
        /// Additional columns or rows are ignored.
        /// </summary>
        /// <returns>The first column of the first row in the result set.</returns>
        public override object ExecuteScalar()
        {
            if (_profiler?.IsActive != true)
            {
                return _command.ExecuteScalar();
            }

            object result;
            _profiler.ExecuteStart(this, SqlExecuteType.Scalar);
            try
            {
                result = _command.ExecuteScalar();
            }
            catch (Exception e)
            {
                _profiler.OnError(this, SqlExecuteType.Scalar, e);
                throw;
            }
            finally
            {
                _profiler.ExecuteFinish(this, SqlExecuteType.Scalar, null);
            }

            return result;
        }

        /// <summary>
        /// Asynchronously executes the query, and returns the first column of the first row in the result set returned by the query. 
        /// Additional columns or rows are ignored.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for this async operation.</param>
        /// <returns>The first column of the first row in the result set.</returns>
        public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            if (_profiler?.IsActive != true)
            {
                return await _command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }

            object result;
            _profiler.ExecuteStart(this, SqlExecuteType.Scalar);
            try
            {
                result = await _command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _profiler.OnError(this, SqlExecuteType.Scalar, e);
                throw;
            }
            finally
            {
                _profiler.ExecuteFinish(this, SqlExecuteType.Scalar, null);
            }

            return result;
        }

        /// <summary>
        /// Attempts to cancels the execution of this command.
        /// </summary>
        public override void Cancel() => _command.Cancel();

        /// <summary>
        /// Creates a prepared (or compiled) version of the command on the data source.
        /// </summary>
        public override void Prepare() => _command.Prepare();

        /// <summary>
        /// Creates a new instance of an <see cref="DbParameter"/> object.
        /// </summary>
        /// <returns>The <see cref="DbParameter"/>.</returns>
        protected override DbParameter CreateDbParameter() => _command.CreateParameter();

        /// <summary>
        /// Releases all resources used by this command.
        /// </summary>
        /// <param name="disposing">false if this is being disposed in a <c>finalizer</c>.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _command != null)
            {
                _command.Dispose();
            }
            _command = null;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets the internal command.
        /// </summary>
        public DbCommand InternalCommand => _command;
    }
}
