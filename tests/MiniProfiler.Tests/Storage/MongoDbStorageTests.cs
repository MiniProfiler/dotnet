using System;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests.Storage
{
    public class MongoDbStorageTests : StorageBaseTest, IClassFixture<MongoDbStorageFixture>
    {
        public MongoDbStorageTests(MongoDbStorageFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }

        [Fact]
        public void RecreationHandling()
        {
            var options = new MongoDbStorageOptions
            {
                ConnectionString = TestConfig.Current.MongoDbConnectionString,
                CollectionName = "MPTest" + Guid.NewGuid().ToString("N").Substring(20),
            };

            var storage = new MongoDbStorage(options);
            Assert.NotNull(storage);
            // Same options, won't throw
            var storage2 = new MongoDbStorage(options);
            Assert.NotNull(storage2);

            options.CacheDuration = TimeSpan.FromSeconds(20);

            // MongoDB.Driver.MongoCommandException : Command createIndexes failed: Index with name: Started_1 already exists with different options.
            var ex = Assert.Throws<MongoCommandException>(() => new MongoDbStorage(options));
            Assert.NotNull(ex);
            Assert.Contains("already exists with different options", ex.Message);

            options.AutomaticallyRecreateIndexes = true;
            // Succeeds, because drop/re-create is allowed now
            var storage4 = new MongoDbStorage(options);
            Assert.NotNull(storage4);
        }
    }

    public class MongoDbStorageFixture : StorageFixtureBase<MongoDbStorage>
    {
        public MongoDbStorageFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.MongoDbConnectionString), TestConfig.Current.MongoDbConnectionString);

            try
            {
                var options = new MongoDbStorageOptions
                {
                    ConnectionString = TestConfig.Current.MongoDbConnectionString,
                    CollectionName = "MPTest" + TestId,
                };

                Storage = new MongoDbStorage(options);

                Storage.GetUnviewedIds("");
            }
            catch (Exception e)
            {
                e.MaybeLog(TestConfig.Current.MongoDbConnectionString);
                ShouldSkip = true;
                SkipReason = e.Message;
            }
        }

        protected override void Dispose(bool disposing)
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
