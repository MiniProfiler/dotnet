using System;
using System.Data.SqlServerCe;

using StackExchange.Profiling;
using StackExchange.Profiling.Data;
using StackExchange.Profiling.Storage;

namespace Tests.Storage
{
    public class SqlCeStorageFixture<T> : IDisposable
    {
        public SqlCeConnection Conn { get; }
        public IAsyncStorage Storage { get; }
        public string ConnectionString { get; }

        public SqlCeStorageFixture()
        {
            ConnectionString = Utils.CreateSqlCeDatabase<T>(deleteIfExists: true, sqlToExecute: SqlServerCeStorage.TableCreationScripts);
            Storage = new SqlServerCeStorage(ConnectionString);
            Conn = Utils.GetOpenSqlCeConnection<T>();
        }

        private ProfiledDbConnection GetProfiledConnection() =>
            new ProfiledDbConnection(Utils.GetOpenSqlCeConnection<T>(), MiniProfiler.Current);

        public void Dispose()
        {
            Conn.Dispose();
        }
    }
}