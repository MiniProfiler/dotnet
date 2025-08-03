using System;
using StackExchange.Profiling.Storage;
using Xunit;

namespace StackExchange.Profiling.Tests.Storage
{
    public class MySqlStorageTests(MySqlStorageFixture fixture, ITestOutputHelper output) : StorageBaseTest(fixture, output), IClassFixture<MySqlStorageFixture>
    {
    }

    public class MySqlStorageFixture : StorageFixtureBase<MySqlStorage>, IDisposable
    {
        public MySqlStorageFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.MySQLConnectionString), TestConfig.Current.MySQLConnectionString);

            Storage = new MySqlStorage(
                TestConfig.Current.MySQLConnectionString,
                "MPTest" + TestId,
                "MPTimingsTest" + TestId,
                "MPClientTimingsTest" + TestId);
            try
            {
                Storage.CreateSchema();
            }
            catch (Exception e)
            {
                e.MaybeLog(TestConfig.Current.MySQLConnectionString);
                ShouldSkip = true;
                SkipReason = e.Message;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!ShouldSkip)
            {
                Storage.DropSchema();
            }
        }
    }
}
