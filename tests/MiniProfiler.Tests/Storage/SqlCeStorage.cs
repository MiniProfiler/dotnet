using System;
using System.Data.Common;
using System.Linq;
using System.Data.SqlServerCe;

using StackExchange.Profiling.Data;
using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling.Tests.Storage
{
    /// <summary>
    /// SQL CE Storage.
    /// </summary>
    internal class SqlCeStorage : SqlServerStorage
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="SqlCeStorage"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public SqlCeStorage(string connectionString) : base(connectionString) { }
        
        protected override DbConnection GetConnection() => new SqlCeConnection(ConnectionString);
    }

    public class SqlCeStorageFixture<T> : IDisposable
    {
        public SqlCeStorageFixture()
        {
            var sqlToExecute = SqlServerStorage.TableCreationScript.Replace("nvarchar(max)", "ntext").Split(';').Where(s => !string.IsNullOrWhiteSpace(s));
            var connStr = Utils.CreateSqlCeDatabase<T>(sqlToExecute: sqlToExecute);
            MiniProfiler.Settings.Storage = new SqlCeStorage(connStr);
            Conn = Utils.GetOpenSqlCeConnection<T>();
        }

        private ProfiledDbConnection GetProfiledConnection()
        {
            return new ProfiledDbConnection(Utils.GetOpenSqlCeConnection<T>(), MiniProfiler.Current);
        }

        public void Dispose()
        {
            Conn.Dispose();
            MiniProfiler.Settings.Storage = null;
        }
        public SqlCeConnection Conn { get; private set; }
    }
}