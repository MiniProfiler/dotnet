using System;
using System.Data.Common;
using StackExchange.Profiling.Storage;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests.Storage
{
    public class SqliteStorageTests : StorageBaseTest, IClassFixture<SqliteStorageFixture>
    {
        public SqliteStorageTests(SqliteStorageFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }
    }

    public class SqliteStorageFixture : StorageFixtureBase<SqliteStorage>, IDisposable
    {
        private DbConnection _doorStop;

        public SqliteStorageFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.SqliteConnectionString), TestConfig.Current.SqliteConnectionString);

            Storage = new SqliteStorage(
                TestConfig.Current.SqliteConnectionString,
                "MPTest" + TestId,
                "MPTimingsTest" + TestId,
                "MPClientTimingsTest" + TestId);
            try
            {
                _doorStop = (Storage as IDatabaseStorageConnectable)?.GetConnection();
                _doorStop?.Open();
                Storage.CreateSchema();
            }
            catch (Exception e)
            {
                ShouldSkip = true;
                SkipReason = e.Message;
            }
        }

        public void Dispose()
        {
            if (!ShouldSkip)
            {
                Storage.DropSchema();
            }
            _doorStop?.Dispose();
        }
    }
}
