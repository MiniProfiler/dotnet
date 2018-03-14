using System;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests.Storage
{
    public class MongoDbStorageTests : StorageBaseTest, IClassFixture<MongoDbStorageFixture>
    {
        public MongoDbStorageTests(MongoDbStorageFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }
    }

    public class MongoDbStorageFixture : StorageFixtureBase<MongoDbStorage>, IDisposable
    {
        public MongoDbStorageFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.MongoDbConnectionString), TestConfig.Current.MongoDbConnectionString);

            try
            {
                Storage = new MongoDbStorage(TestConfig.Current.MongoDbConnectionString);
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
        }
    }
}
