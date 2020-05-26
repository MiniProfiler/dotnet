using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Profiling.Internal;
using StackExchange.Profiling.Storage;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests
{
    public class InternalErrorTests : BaseTest
    {
        public InternalErrorTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task StopErrorLogging()
        {
            int errorCount = 0;
            Exception lastError = null;
            void Log(Exception ex)
            {
                errorCount++;
                lastError = ex;
            }
            var options = new MiniProfilerBaseOptions()
            {
                Storage = new KaboomStorage(),
                StopwatchProvider = () => new UnitTestStopwatch(),
                OnInternalError = Log
            };

            var profiler = options.StartProfiler();
            AddRecursiveChildren(profiler, 1, 10);
            Assert.Equal(0, errorCount);
            profiler.Stop();
            Assert.Equal(1, errorCount);
            Assert.IsType<KaboomStorage.BoomBoom>(lastError);

            profiler = options.StartProfiler();
            AddRecursiveChildren(profiler, 1, 10);
            Assert.Equal(1, errorCount);
            await profiler.StopAsync().ConfigureAwait(false);
            Assert.Equal(2, errorCount);
            Assert.IsType<KaboomStorage.BoomBoom>(lastError);
        }
    }


    public class KaboomStorage : IAsyncStorage
    {
        public List<Guid> GetUnviewedIds(string user) => throw new BoomBoom();
        public Task<List<Guid>> GetUnviewedIdsAsync(string user) => throw new BoomBoom();
        public IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending) => throw new BoomBoom();
        public Task<IEnumerable<Guid>> ListAsync(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending) => throw new BoomBoom();
        public MiniProfiler Load(Guid id) => throw new BoomBoom();
        public Task<MiniProfiler> LoadAsync(Guid id) => throw new BoomBoom();
        public void Save(MiniProfiler profiler) => throw new BoomBoom();
        public Task SaveAsync(MiniProfiler profiler) => throw new BoomBoom();
        public void SetUnviewed(string user, Guid id) => throw new BoomBoom();
        public Task SetUnviewedAsync(string user, Guid id) => throw new BoomBoom();
        public void SetViewed(string user, Guid id) => throw new BoomBoom();
        public Task SetViewedAsync(string user, Guid id) => throw new BoomBoom();

        public class BoomBoom : Exception { }
    }
}
