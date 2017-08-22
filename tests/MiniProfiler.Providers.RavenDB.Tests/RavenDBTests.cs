using Raven.Client;
using Raven.Client.Document;
using StackExchange.Profiling;
using StackExchange.Profiling.RavenDb;
using Xunit;

namespace Tests.RavenDB
{
    /// <summary>
    /// Only sanity checking connectivity at the moment - this needs a lot more work once
    /// we figure out the best possible way to hook into the RavenDB profiling
    /// </summary>
    public class RavenDBTests
    {
        // TODO: Gate all test skipping based on if the connectiong endpoint is running, so they skip rather than fail
        public RavenDBTests()
        {
        }

        private IDocumentStore GetStore() =>
            new DocumentStore() { Url = "http://localhost:8080" }.Initialize().AddMiniProfiler();

        //[Fact(Skip ="Disabled until we have a is-RavenDB-running control in place")]
        //public void BasicQueryTest()
        //{
        //    var mp = MiniProfiler.Start();
        //    using (var store = GetStore())
        //    {
        //        var products = store.DatabaseCommands.GetIndex("products");
        //    }
        //    Assert.True(mp.Head.HasCustomTimings);
        //    mp.Stop(false);
        //}
    }
}
