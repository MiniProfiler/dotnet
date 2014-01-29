namespace StackExchange.Profiling.Tests.Storage
{
    using System;
    using System.Linq;

    using NUnit.Framework;

    using Profiling.Storage;

    /// <summary>
    /// test the HTTP runtime cache storage.
    /// </summary>
    [TestFixture]
    public class TestHttpRuntimeCacheStorage
    {
        /// <summary>
        /// test we can save the same profiler twice.
        /// </summary>
        [Test]
        public void TestWeCanSaveTheSameProfilerTwice()
        {
            var profiler = new MiniProfiler("/") { Started = DateTime.UtcNow, Id = Guid.NewGuid() };
            var storage = new HttpRuntimeCacheStorage(new TimeSpan(1, 0, 0));
            storage.Save(profiler);
            storage.Save(profiler);
            var guids = storage.List(100).ToArray();
            Assert.AreEqual(profiler.Id, guids.First());
            Assert.AreEqual(1, guids.Count());
        }

        /// <summary>
        /// test range queries.
        /// </summary>
        [Test]
        public void TestRangeQueries()
        {
            var now = DateTime.UtcNow;
            var inASec = now.AddSeconds(1);
            var in2Secs = now.AddSeconds(2);
            var in3Secs = now.AddSeconds(3);
            var profiler = new MiniProfiler("/") { Started = now, Id = Guid.NewGuid() };
            var profiler1 = new MiniProfiler("/") { Started = inASec, Id = Guid.NewGuid() };
            var profiler2 = new MiniProfiler("/") { Started = in2Secs, Id = Guid.NewGuid() };
            var profiler3 = new MiniProfiler("/") { Started = in3Secs, Id = Guid.NewGuid() };
            
            var storage = new HttpRuntimeCacheStorage(new TimeSpan(1, 0, 0));

            storage.Save(profiler);
            storage.Save(profiler3);
            storage.Save(profiler2);
            storage.Save(profiler1);

            var guids = storage.List(100);
            Assert.AreEqual(4, guids.Count());

            guids = storage.List(1);
            Assert.AreEqual(1, guids.Count());

            guids = storage.List(2, now, in2Secs);
            Assert.AreEqual(profiler2.Id, guids.First());
            Assert.AreEqual(profiler1.Id, guids.Skip(1).First());
            Assert.AreEqual(2, guids.Count());
        }
    }
}
