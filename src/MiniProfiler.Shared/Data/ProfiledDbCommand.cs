using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
#if !MINIMAL
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// The profiled database command.
    /// </summary>
    [System.ComponentModel.DesignerCategory("")]
    public partial class ProfiledDbCommand : DbCommand
    {
        private DbCommand _command;
        private DbConnection? _connection;
        private DbTransaction? _transaction;
        private IDbProfiler? _profiler;

        /// <summary>
        /// Whether to always wrap data readers, even if there isn't an active profiler on this connect.
        /// This allows depending on overrides for things inheriting from <see cref="ProfiledDbDataReader"/> to actually execute.
        /// </summary>
        protected virtual bool AlwaysWrapReaders => false;

#if !MINIMAL
        private static Link<Type, Action<IDbCommand, bool>>? bindByNameCache;
        private bool _bindByName;

        /// <summary>
        /// Gets or sets a value indicating whether or not to bind by name.
        /// If the underlying command supports BindByName, this sets/clears the underlying
        /// implementation accordingly. This is required to support OracleCommand from Dapper.
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
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfiledDbCommand"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="profiler">The profiler.</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="command"/> is <c>null</c>.</exception>
        public ProfiledDbCommand(DbCommand command, DbConnection? connection, IDbProfiler? profiler)
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

#if !MINIMAL
        /// <summary>
        /// Get the binding name.
        /// </summary>
        /// <param name="commandType">The command type.</param>
        /// <returns>The <see cref="Action"/>.</returns>
        private static Action<IDbCommand, bool>? GetBindByName(Type commandType)
        {
            if (commandType == null) return null; // GIGO
            if (Link<Type, Action<IDbCommand, bool>>.TryGet(bindByNameCache, commandType, out var action))
            {
                return action;
            }

            var prop = commandType.GetProperty("BindByName", BindingFlags.Public | BindingFlags.Instance);
            action = null;
            ParameterInfo[] indexers;
            MethodInfo? setter;
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
            Link<Type, Action<IDbCommand, bool>>.TryAdd(ref bindByNameCache, commandType, ref action!);
            return action;
        }
#endif

        /// <inheritdoc cref="DbCommand.CommandText"/>
        [AllowNull]
        public override string CommandText
        {
            get => _command.CommandText;
            set => _command.CommandText = value;
        }

        /// <inheritdoc cref="DbCommand.CommandTimeout"/>
        public override int CommandTimeout
        {
            get => _command.CommandTimeout;
            set => _command.CommandTimeout = value;
        }

        /// <inheritdoc cref="DbCommand.CommandType"/>
        public override CommandType CommandType
        {
            get => _command.CommandType;
            set => _command.CommandType = value;
        }

        /// <inheritdoc cref="DbCommand.DbConnection"/>
        protected override DbConnection? DbConnection
        {
            get => _connection;
            set
            {
                _connection = value;
                UnwrapAndAssignConnection(value);
            }
        }

        private void UnwrapAndAssignConnection(DbConnection? value)
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

        /// <inheritdoc cref="DbCommand.DbParameterCollection"/>
        protected override DbParameterCollection DbParameterCollection => _command.Parameters;

        /// <inheritdoc cref="DbCommand.DbTransaction"/>
        protected override DbTransaction? DbTransaction
        {
            get => _transaction;
            set
            {
                _transaction = value;
                _command.Transaction = value is ProfiledDbTransaction awesomeTran ? awesomeTran.WrappedTransaction : value;
            }
        }

        /// <inheritdoc cref="DbCommand.DesignTimeVisible"/>
        public override bool DesignTimeVisible
        {
            get => _command.DesignTimeVisible;
            set => _command.DesignTimeVisible = value;
        }

        /// <inheritdoc cref="DbCommand.UpdatedRowSource"/>
        public override UpdateRowSource UpdatedRowSource
        {
            get => _command.UpdatedRowSource;
            set => _command.UpdatedRowSource = value;
        }

        /// <summary>
        /// Creates a wrapper data reader for <see cref="ExecuteDbDataReader"/> and <see cref="ExecuteDbDataReaderAsync"/> />
        /// </summary>
        protected virtual DbDataReader CreateDbDataReader(DbDataReader original, CommandBehavior behavior, IDbProfiler? profiler)
            => new ProfiledDbDataReader(original, behavior, profiler);

        /// <inheritdoc cref="DbCommand.ExecuteDbDataReader(CommandBehavior)"/>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            DbDataReader? result = null;
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

        /// <inheritdoc cref="DbCommand.ExecuteDbDataReaderAsync(CommandBehavior, CancellationToken)"/>
        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            DbDataReader? result = null;
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

        /// <inheritdoc cref="DbCommand.ExecuteNonQuery()"/>
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

        /// <inheritdoc cref="DbCommand.ExecuteNonQueryAsync(CancellationToken)"/>
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

        /// <inheritdoc cref="DbCommand.ExecuteScalar()"/>
        public override object? ExecuteScalar()
        {
            if (_profiler?.IsActive != true)
            {
                return _command.ExecuteScalar();
            }

            object? result;
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

        /// <inheritdoc cref="DbCommand.ExecuteScalarAsync(CancellationToken)"/>
        public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            if (_profiler?.IsActive != true)
            {
                return await _command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }

            object? result;
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

        /// <inheritdoc cref="DbCommand.Cancel()"/>
        public override void Cancel() => _command.Cancel();

        /// <inheritdoc cref="DbCommand.Prepare()"/>
        public override void Prepare() => _command.Prepare();

        /// <inheritdoc cref="DbCommand.CreateDbParameter()"/>
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
            _command = null!;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Obsolete - please use <see cref="WrappedCommand"/>.
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never), Obsolete($"Please use {nameof(WrappedCommand)}", false)]
        public DbCommand InternalCommand => _command;

        /// <summary>
        /// Gets the internally wrapped <see cref="DbCommand"/>.
        /// </summary>
        public DbCommand WrappedCommand => _command;
    }
}
