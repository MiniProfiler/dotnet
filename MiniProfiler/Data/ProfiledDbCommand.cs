using System;
using System.Data.Common;
using System.Data;

using Profiling;

namespace Profiling.Data
{
    public class ProfiledDbCommand : DbCommand
    {
        private DbCommand _cmd;
        private DbConnection _conn;
        private DbTransaction _tran;

        private MiniProfiler _profiler;
        private SqlProfiler _sqlProfiler;


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

    }
}