using System;
using System.Data.Common;
using System.Data;
using MvcMiniProfiler;
using System.Reflection;
using System.Reflection.Emit;

namespace MvcMiniProfiler.Data
{
    public class ProfiledDbCommand : DbCommand, ICloneable
    {
        private DbCommand _cmd;
        private DbConnection _conn;
        private DbTransaction _tran;

        private MiniProfiler _profiler;
        private SqlProfiler _sqlProfiler;

        private bool bindByName;
        /// <summary>
        /// If the underlying command supports BindByName, this sets/clears the underlying
        /// implementation accordingly. This is required to support OracleCommand from dapper-dot-net
        /// </summary>
        public bool BindByName
        {
            get { return bindByName; }
            set
            {
                if (bindByName != value)
                {
                    if (_cmd != null)
                    {
                        var inner = GetBindByName(_cmd.GetType());
                        if (inner != null) inner(_cmd, value);
                    }
                    bindByName = value;
                }
            }
        }
        static Link<Type, Action<IDbCommand, bool>> bindByNameCache;
        static Action<IDbCommand, bool> GetBindByName(Type commandType)
        {
            if (commandType == null) return null; // GIGO
            Action<IDbCommand, bool> action;
            if (Link<Type, Action<IDbCommand, bool>>.TryGet(bindByNameCache, commandType, out action))
            {
                return action;
            }
            var prop = commandType.GetProperty("BindByName", BindingFlags.Public | BindingFlags.Instance);
            action = null;
            ParameterInfo[] indexers;
            MethodInfo setter;
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(bool)
                && ((indexers = prop.GetIndexParameters()) == null || indexers.Length == 0)
                && (setter = prop.GetSetMethod()) != null
                )
            {
                var method = new DynamicMethod(commandType.Name + "_BindByName", null, new Type[] { typeof(IDbCommand), typeof(bool) });
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
        public ProfiledDbCommand(DbCommand cmd, DbConnection conn, MiniProfiler profiler)
        {
            if (cmd == null) throw new ArgumentNullException("cmd");

            _cmd = cmd;
            _conn = conn;

            if (profiler != null)
            {
                _profiler = profiler;
                _sqlProfiler = profiler.SqlProfiler;
            }
        }


        public override string CommandText
        {
            get { return _cmd.CommandText; }
            set { _cmd.CommandText = value; }
        }

        public override int CommandTimeout
        {
            get { return _cmd.CommandTimeout; }
            set { _cmd.CommandTimeout = value; }
        }

        public override CommandType CommandType
        {
            get { return _cmd.CommandType; }
            set { _cmd.CommandType = value; }
        }

        protected override DbConnection DbConnection
        {
            get { return _conn; }
            set
            {
                _conn = value;
                var awesomeConn = value as ProfiledDbConnection;
                _cmd.Connection = awesomeConn == null ? value : awesomeConn.WrappedConnection;
            }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get { return _cmd.Parameters; }
        }

        protected override DbTransaction DbTransaction
        {
            get { return _tran; }
            set
            {
                this._tran = value;
                var awesomeTran = value as ProfiledDbTransaction;
                _cmd.Transaction = awesomeTran == null ? value : awesomeTran.WrappedTransaction;
            }
        }

        public override bool DesignTimeVisible
        {
            get { return _cmd.DesignTimeVisible; }
            set { _cmd.DesignTimeVisible = value; }
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get { return _cmd.UpdatedRowSource; }
            set { _cmd.UpdatedRowSource = value; }
        }


        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            _sqlProfiler.ExecuteStart(this, ExecuteType.Reader);

            var result = _cmd.ExecuteReader(behavior);

            if (_sqlProfiler != null)
            {
                result = new ProfiledDbDataReader(result, _conn, _profiler);
                _sqlProfiler.ExecuteFinish(this, ExecuteType.Reader, result);
            }

            return result;
        }

        public override int ExecuteNonQuery()
        {
            _sqlProfiler.ExecuteStart(this, ExecuteType.NonQuery);
            var result = _cmd.ExecuteNonQuery();
            _sqlProfiler.ExecuteFinish(this, ExecuteType.NonQuery);
            return result;
        }

        public override object ExecuteScalar()
        {
            _sqlProfiler.ExecuteStart(this, ExecuteType.Scalar);
            object result = _cmd.ExecuteScalar();
            _sqlProfiler.ExecuteFinish(this, ExecuteType.Scalar);
            return result;
        }

        public override void Cancel()
        {
            _cmd.Cancel();
        }

        public override void Prepare()
        {
            _cmd.Prepare();
        }

        protected override DbParameter CreateDbParameter()
        {
            return _cmd.CreateParameter();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _cmd != null)
            {
                _cmd.Dispose();
            }
            _cmd = null;
            base.Dispose(disposing);
        }


        public ProfiledDbCommand Clone()
        { // EF expects ICloneable
            ICloneable tail = _cmd as ICloneable;
            if (tail == null) throw new NotSupportedException("Underlying " + _cmd.GetType().Name + " is not cloneable");
            return new ProfiledDbCommand((DbCommand)tail.Clone(), _conn, _profiler);
        }
        object ICloneable.Clone() { return Clone(); }
    }
}