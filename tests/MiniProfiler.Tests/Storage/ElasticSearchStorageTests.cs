using System;
using Nest;
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

    public class ElasticSearchStorageFixture : StorageFixtureBase<ElasticsearchStorage>, IDisposable
    {
        public ElasticSearchStorageFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.ElasticSearchConnectionString), TestConfig.Current.ElasticSearchConnectionString);

            try
            {
                var indexName = "mp-" + Guid.NewGuid().ToString();
                Storage = new ElasticsearchStorage(TestConfig.Current.ElasticSearchConnectionString, indexName);
                var response = Storage.CreateIndex();

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
                Storage.DropIndex();
            }
        }
    }

    public static class ElasticSearchStorageExtensions
    {
        /// <summary>
        /// Creates an index.
        /// </summary>
        /// <param name="storage">The storage to drop schema for.</param>
        public static ICreateIndexResponse CreateIndex(this ElasticsearchStorage storage)
        {
            return ((IElasticstorageConnectable)storage).GetClient()
                .CreateIndex(IndexName.From<string>(storage.IndexName));
        }

        /// <summary>
        /// Drop database for ElasticSearch storage.
        /// </summary>
        /// <param name="storage">The storage to drop schema for.</param>
        public static void DropIndex(this ElasticsearchStorage storage)
        {
            ((IElasticstorageConnectable)storage).GetClient().DeleteIndex(storage.IndexName);
        }
    }
}
