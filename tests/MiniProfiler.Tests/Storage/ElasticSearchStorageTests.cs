using System;
using StackExchange.Profiling.Storage;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests.Storage
{
    public class ElasticSearchStorageTests : StorageBaseTest, IClassFixture<ElasticSearchStorageFixture>
    {
        public ElasticSearchStorageTests(ElasticSearchStorageFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }
    }

    public class ElasticSearchStorageFixture : StorageFixtureBase<ElasticSearchStorage>, IDisposable
    {
        public ElasticSearchStorageFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.ElasticSearchConnectionString), TestConfig.Current.ElasticSearchConnectionString);

            try
            {
                Storage = new ElasticSearchStorage(TestConfig.Current.ElasticSearchConnectionString);
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
            }
        }
    }

    public static class ElasticSearchStorageExtensions
    {
        /// <summary>
        /// Drop database for ElasticSearch storage.
        /// </summary>
        /// <param name="storage">The storage to drop schema for.</param>
        public static void DropDatabase(this ElasticSearchStorage storage)
        {
            storage.GetClient().DeleteIndex(storage.IndexName);
        }
    }
}
