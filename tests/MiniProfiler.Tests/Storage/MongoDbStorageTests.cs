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
                Storage = new MongoDbStorage(
                    TestConfig.Current.MongoDbConnectionString,
                    "MPTest" + TestId);
                
                Storage.WithIndexCreation();
                Storage.GetUnviewedIds("");
            }
            catch (Exception e)
            {
                e.MaybeLog(TestConfig.Current.MongoDbConnectionString);
                ShouldSkip = true;
                SkipReason = e.Message;
            }
        }

        public void Dispose()
        {
            if (!ShouldSkip)
            {
                Storage.DropDatabase();
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
            var url = new MongoDB.Driver.MongoUrl(TestConfig.Current.MongoDbConnectionString);
            storage.GetClient().DropDatabase(url.DatabaseName);
        }
    }
}
