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
                Storage.GetUnviewedIds("");
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
                Storage.DropDatabase();
                Storage.Dispose();
            }
        }
    }

    public static class MongoDbStorageExtensions
    {
        /// <summary>
        /// Drop database for MongoDb storage.
        /// </summary>
        /// <param name="storage">The storage to drop schema for.</param>
        public static void DropDatabase(this MongoDbStorage storage)
        {
            storage.GetClient().DropDatabase("MiniProfiler");
        }
    }
}
