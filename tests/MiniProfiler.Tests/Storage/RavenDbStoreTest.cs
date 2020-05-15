using System;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests.Storage
{
    public class RavenDbStoreTest : StorageBaseTest, IClassFixture<RavenDbStoreFixture>
    {
        public RavenDbStoreTest(RavenDbStoreFixture fixture, ITestOutputHelper output) 
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
                
                Storage = new RavenDbStorage(store);//.WithIndexCreation();
                
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
        
        
        public void Dispose()
        {
        }
    }
}
