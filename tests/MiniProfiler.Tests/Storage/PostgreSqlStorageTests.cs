#if (NETCOREAPP2_0 || NET461)
using System;
using StackExchange.Profiling.Storage;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests.Storage
{
    public class PostgreSqlStorageTests : StorageBaseTest, IClassFixture<PostgreSqlStorageFixture>
    {
        public PostgreSqlStorageTests(PostgreSqlStorageFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }
    }

    public class PostgreSqlStorageFixture : StorageFixtureBase<PostgreSqlStorage>, IDisposable
    {
        public PostgreSqlStorageFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.PostgreSqlConnectionString), TestConfig.Current.PostgreSqlConnectionString);

            Storage = new PostgreSqlStorage(
                TestConfig.Current.PostgreSqlConnectionString,
                "MPTest" + TestId,
                "MPTimingsTest" + TestId,
                "MPClientTimingsTest" + TestId);
            try
            {
                Storage.CreateSchema();
            }
            catch (Exception e)
            {
                e.MaybeLog(TestConfig.Current.PostgreSqlConnectionString);
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
#endif
