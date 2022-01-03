using System;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using StackExchange.Profiling.Storage;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests.Storage
{
    public class RavenDbStoreTests : StorageBaseTest, IClassFixture<RavenDbStoreFixture>
    {
        public RavenDbStoreTests(RavenDbStoreFixture fixture, ITestOutputHelper output) 
            : base(fixture, output)
        {
        }
    }

    public class RavenDbStoreFixture : StorageFixtureBase<RavenDbStorage>, IDisposable
    {
        public RavenDbStoreFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.RavenDbUrls), TestConfig.Current.RavenDbUrls);
            Skip.IfNoConfig(nameof(TestConfig.Current.RavenDatabase), TestConfig.Current.RavenDatabase);

            try
            {
                var store = new DocumentStore
                {
                    Urls = TestConfig.Current.RavenDbUrls.Split(';'), Database = TestConfig.Current.RavenDatabase
                };

                store.Initialize();
                
                try
                {
                    store.Maintenance.ForDatabase(TestConfig.Current.RavenDatabase).Send(new GetStatisticsOperation());
                }
                catch (DatabaseDoesNotExistException)
                {
                    try
                    {
                        store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(TestConfig.Current.RavenDatabase)));
                    }
                    catch (ConcurrencyException)
                    {
                        // The database was already created before calling CreateDatabaseOperation
                    }
                }
                
                store.Dispose();
                store = null;
                
                Storage = new RavenDbStorage(TestConfig.Current.RavenDbUrls.Split(';'), TestConfig.Current.RavenDatabase, waitForIndexes: true);
                Storage.GetUnviewedIds("");
            }
            catch (Exception e)
            {
                e.MaybeLog(TestConfig.Current.RavenDbUrls);
                e.MaybeLog(TestConfig.Current.RavenDatabase);
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
    
    public static class RavenDbDbStorageExtensions
    {
        /// <summary>
        /// Drop database for RavenDB storage.
        /// </summary>
        /// <param name="storage">The storage to drop schema for.</param>
        public static void DropDatabase(this RavenDbStorage storage)
        {
            var store = storage.GetDocumentStore();
            store.Maintenance.Server.Send(new DeleteDatabasesOperation(store.Database, true));
        }
    }
}
