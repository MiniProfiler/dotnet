using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Profiling.Storage;
using NUnit.Framework;

namespace StackExchange.Profiling.Tests.Storage
{
    [TestFixture]
    public class TestHttpRuntimeCacheStorage
    {
        [Test]
        public void TestWeCanSaveTheSameProfilerTwice()
        {
            var profiler = new MiniProfiler();
            profiler.Started = DateTime.UtcNow;
            profiler.Id = Guid.NewGuid();
            var storage = new HttpRuntimeCacheStorage(new TimeSpan(1,0,0));
            storage.Save(profiler);
            storage.Save(profiler);
            var guids = storage.List(100);
            Assert.AreEqual(profiler.Id, guids.First());
            Assert.AreEqual(1, guids.Count());
        }

        [Test]
        public void TestRangeQueries()
        {
            var now = DateTime.UtcNow;
            var inASec = now.AddSeconds(1);
            var in2Secs = now.AddSeconds(2);
            var in3Secs = now.AddSeconds(3);
            var profiler = new MiniProfiler { Started = now, Id = Guid.NewGuid() };
            var profiler1 = new MiniProfiler { Started = inASec, Id = Guid.NewGuid() };
            var profiler2 = new MiniProfiler { Started = in2Secs, Id = Guid.NewGuid() };
            var profiler3 = new MiniProfiler { Started = in3Secs, Id = Guid.NewGuid() };
            
            var storage = new HttpRuntimeCacheStorage(new TimeSpan(1, 0, 0));

            storage.Save(profiler);
            storage.Save(profiler3);
            storage.Save(profiler2);
            storage.Save(profiler1);

            var guids = storage.List(100);
            Assert.AreEqual(4, guids.Count());

            guids = storage.List(1);
            Assert.AreEqual(1, guids.Count());

            guids = storage.List(2, now, in2Secs, ListResultsOrder.Decending);
            Assert.AreEqual(profiler2.Id, guids.First());
            Assert.AreEqual(profiler1.Id, guids.Skip(1).First());
            Assert.AreEqual(2, guids.Count());
        }
    }
}
