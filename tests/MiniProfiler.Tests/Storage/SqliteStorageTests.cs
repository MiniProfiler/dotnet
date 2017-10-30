using System;
using System.Data.Common;
using System.IO;
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
        private readonly string fileName;

        public SqliteStorageFixture()
        {
            fileName = Guid.NewGuid() + ".sqlite";

            Storage = new SqliteStorage(
                $"Data Source={fileName}",
                "MPTest" + TestId,
                "MPTimingsTest" + TestId,
                "MPClientTimingsTest" + TestId);
            try
            {
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
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }
}
