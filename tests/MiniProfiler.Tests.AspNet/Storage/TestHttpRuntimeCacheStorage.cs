using System;
using System.Linq;

using StackExchange.Profiling.Storage;
using Xunit;

namespace StackExchange.Profiling.Tests.Storage
{
    public class TestMemoryCacheStorage
    {
        private MiniProfilerOptions Options { get; }
        public TestMemoryCacheStorage()
        {
            Options = new MiniProfilerOptions()
            {
                Storage = new MemoryCacheStorage(new TimeSpan(1, 0, 0))
            };
        }

        [Fact(WindowsOnly = true)]
        public void TestWeCanSaveTheSameProfilerTwice()
        {
            var profiler = new MiniProfiler("/", Options) { Started = DateTime.UtcNow, Id = Guid.NewGuid() };
            Options.Storage.Save(profiler);
            Options.Storage.Save(profiler);
            var guids = Options.Storage.List(100).ToArray();
            Assert.Equal(profiler.Id, guids[0]);
            Assert.Single(guids);
        }

        [Fact(WindowsOnly = true)]
        public void TestRangeQueries()
        {
            var now = DateTime.UtcNow;
            var inASec = now.AddSeconds(1);
            var in2Secs = now.AddSeconds(2);
            var in3Secs = now.AddSeconds(3);
            var profiler = new MiniProfiler("/", Options) { Started = now, Id = Guid.NewGuid() };
            var profiler1 = new MiniProfiler("/", Options) { Started = inASec, Id = Guid.NewGuid() };
            var profiler2 = new MiniProfiler("/", Options) { Started = in2Secs, Id = Guid.NewGuid() };
            var profiler3 = new MiniProfiler("/", Options) { Started = in3Secs, Id = Guid.NewGuid() };

            Options.Storage.Save(profiler);
            Options.Storage.Save(profiler3);
            Options.Storage.Save(profiler2);
            Options.Storage.Save(profiler1);

            var guids = Options.Storage.List(100);
            Assert.Equal(4, guids.Count());

            guids = Options.Storage.List(1);
            Assert.Single(guids);

            guids = Options.Storage.List(2, now, in2Secs);
            Assert.Equal(profiler2.Id, guids.First());
            Assert.Equal(profiler1.Id, guids.Skip(1).First());
            Assert.Equal(2, guids.Count());
        }
    }
}
