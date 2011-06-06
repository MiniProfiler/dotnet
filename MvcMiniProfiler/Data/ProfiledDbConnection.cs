using System;
using System.Data;
using System.Data.Common;

namespace MvcMiniProfiler.Data
{
    public class ProfiledDbConnection : DbConnection
    {

        private DbConnection _conn;
        private MiniProfiler _profiler;
        private SqlProfiler _sqlProfiler;

        public ProfiledDbConnection(DbConnection connection, MiniProfiler profiler)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            _conn = connection;
            _conn.StateChange += StateChangeHandler;

            if (profiler != null)
            {
                _profiler = profiler;
                _sqlProfiler = profiler.SqlProfiler;
            }
        }

        internal DbConnection WrappedConnection
        {
            get { return _conn; }
        }

        protected override bool CanRaiseEvents
        {
            get { return true; }
        }

        public override string ConnectionString
        {
            get { return _conn.ConnectionString; }
            set { _conn.ConnectionString = value; }
        }

        public override int ConnectionTimeout
        {
            get { return _conn.ConnectionTimeout; }
        }

        public override string Database
        {
            get { return _conn.Database; }
        }

        public override string DataSource
        {
            get { return _conn.DataSource; }
        }

        public override string ServerVersion
        {
            get { return _conn.ServerVersion; }
        }

        public override ConnectionState State
        {
            get { return _conn.State; }
        }

        public override void ChangeDatabase(string databaseName)
        {
            _conn.ChangeDatabase(databaseName);
        }

        public override void Close()
        {
            _conn.Close();
        }

        public override void EnlistTransaction(System.Transactions.Transaction transaction)
        {
            _conn.EnlistTransaction(transaction);
        }

        public override DataTable GetSchema()
        {
            return _conn.GetSchema();
        }

        public override DataTable GetSchema(string collectionName)
        {
            return _conn.GetSchema(collectionName);
        }

        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            return _conn.GetSchema(collectionName, restrictionValues);
        }

        public override void Open()
        {
            _conn.Open();
        }

        protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel)
        {
            return new ProfiledDbTransaction(_conn.BeginTransaction(isolationLevel), this);
        }

        protected override DbCommand CreateDbCommand()
        {
            return new ProfiledDbCommand(_conn.CreateCommand(), this, _profiler);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _conn != null)
            {
                _conn.StateChange -= StateChangeHandler;
                _conn.Dispose();
            }
            _conn = null;
            base.Dispose(disposing);
        }

        void StateChangeHandler(object sender, StateChangeEventArgs e)
        {
            OnStateChange(e);
        }

    }
}