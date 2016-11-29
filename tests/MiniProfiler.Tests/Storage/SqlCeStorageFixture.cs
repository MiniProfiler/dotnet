using System;
using System.Data.SqlServerCe;

using StackExchange.Profiling.Data;
using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling.Tests.Storage
{
    public class SqlCeStorageFixture<T> : IDisposable
    {
        public SqlCeConnection Conn { get; private set; }

        public SqlCeStorageFixture()
        {
            var connStr = Utils.CreateSqlCeDatabase<T>(deleteIfExists: true, sqlToExecute: SqlServerCeStorage.TableCreationScripts);
            MiniProfiler.Settings.Storage = new SqlServerCeStorage(connStr);
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
    }
}