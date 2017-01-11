using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// The profiled database command.
    /// </summary>
    [System.ComponentModel.DesignerCategory("")]
    public class ProfiledDbCommand : DbCommand
#if NET45
, ICloneable
#endif
    {
        /// <summary>
        /// The bind by name cache.
        /// </summary>
        private static Link<Type, Action<IDbCommand, bool>> bindByNameCache;

        private DbCommand _command;
        private DbConnection _connection;
        private DbTransaction _transaction;
        private IDbProfiler _profiler;
        private bool _bindByName;

        /// <summary>
        /// Gets or sets a value indicating whether or not to bind by name.
        /// If the underlying command supports BindByName, this sets/clears the underlying
        /// implementation accordingly. This is required to support OracleCommand from dapper-dot-net
        /// </summary>
        public bool BindByName
        {
            get { return _bindByName; }
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
        /// Initialises a new instance of the <see cref="ProfiledDbCommand"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="profiler">The profiler.</param>
        public ProfiledDbCommand(DbCommand command, DbConnection connection, IDbProfiler profiler)
        {
            _command = command ?? throw new ArgumentNullException(nameof(command));
            _connection = connection;

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

            var prop = commandType
#if !NET45 // TODO: Revisit in .NET Standard 2.0
                .GetTypeInfo()
#endif
                .GetProperty("BindByName", BindingFlags.Public | BindingFlags.Instance);
            action = null;
            ParameterInfo[] indexers;
            MethodInfo setter;
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(bool)
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
            get { return _command.CommandText; }
            set { _command.CommandText = value; }
        }

        /// <summary>
        /// Gets or sets the command timeout.
        /// </summary>
        public override int CommandTimeout
        {
            get { return _command.CommandTimeout; }
            set { _command.CommandTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the command type.
        /// </summary>
        public override CommandType CommandType
        {
            get { return _command.CommandType; }
            set { _command.CommandType = value; }
        }

        /// <summary>
        /// Gets or sets the database connection.
        /// </summary>
        protected override DbConnection DbConnection
        {
            get { return _connection; }
            set
            {
                // TODO: we need a way to grab the IDbProfiler which may not be the same as the MiniProfiler, it could be wrapped
                // allow for command reuse, it is clear the connection is going to need to be reset
                if (MiniProfiler.Current != null)
                {
                    _profiler = MiniProfiler.Current;
                }

                _connection = value;
                var awesomeConn = value as ProfiledDbConnection;
                _command.Connection = awesomeConn == null ? value : awesomeConn.WrappedConnection;
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
            get { return _transaction; }
            set
            {
                _transaction = value;
                var awesomeTran = value as ProfiledDbTransaction;
                _command.Transaction = awesomeTran == null ? value : awesomeTran.WrappedTransaction;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the command is design time visible.
        /// </summary>
        public override bool DesignTimeVisible
        {
            get { return _command.DesignTimeVisible; }
            set { _command.DesignTimeVisible = value; }
        }

        /// <summary>
        /// Gets or sets the updated row source.
        /// </summary>
        public override UpdateRowSource UpdatedRowSource
        {
            get { return _command.UpdatedRowSource; }
            set { _command.UpdatedRowSource = value; }
        }

        /// <summary>
        /// The execute database data reader.
        /// </summary>
        /// <param name="behavior">The behaviour.</param>
        /// <returns>the resulting <see cref="DbDataReader"/>.</returns>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            if (_profiler == null || !_profiler.IsActive)
            {
                return _command.ExecuteReader(behavior);
            }

            DbDataReader result = null;
            _profiler.ExecuteStart(this, SqlExecuteType.Reader);
            try
            {
                result = _command.ExecuteReader(behavior);
                result = new ProfiledDbDataReader(result, _connection, _profiler);
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
        /// execute a non query.
        /// </summary>
        /// <returns>the number of affected records.</returns>
        public override int ExecuteNonQuery()
        {
            if (_profiler == null || !_profiler.IsActive)
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
        /// execute the scalar.
        /// </summary>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public override object ExecuteScalar()
        {
            if (_profiler == null || !_profiler.IsActive)
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
        /// cancel the command.
        /// </summary>
        public override void Cancel() => _command.Cancel();

        /// <summary>
        /// prepare the command.
        /// </summary>
        public override void Prepare() => _command.Prepare();

        /// <summary>
        /// create a database parameter.
        /// </summary>
        /// <returns>The <see cref="DbParameter"/>.</returns>
        protected override DbParameter CreateDbParameter() => _command.CreateParameter();

        /// <summary>
        /// dispose the command.
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

#if NET45
        /// <summary>
        /// clone the command, entity framework expects this behaviour.
        /// </summary>
        /// <returns>The <see cref="ProfiledDbCommand"/>.</returns>
        public ProfiledDbCommand Clone()
        { // EF expects ICloneable
            var tail = _command as ICloneable;
            if (tail == null) throw new NotSupportedException("Underlying " + _command.GetType().Name + " is not cloneable");
            return new ProfiledDbCommand((DbCommand)tail.Clone(), _connection, _profiler);
        }

        /// <summary>
        /// The clone.
        /// </summary>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        object ICloneable.Clone() => Clone();
#endif
    }
}
