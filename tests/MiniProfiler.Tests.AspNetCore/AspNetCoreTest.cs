using StackExchange.Profiling.Storage;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace StackExchange.Profiling.Tests
{
    public abstract class AspNetCoreTest : BaseTest
    {
        //protected MiniProfilerOptions Options { get; set; }

        protected AspNetCoreTest(ITestOutputHelper output) : base(output)
        {
            // Instance per class, so multiple tests swapping the provider don't cause issues here
            // It's not a threading issue of the profiler, but rather tests swapping providers
            Options = new MiniProfilerOptions()
            {
                StopwatchProvider = () => new UnitTestStopwatch(),
                Storage = new MemoryCacheStorage(GetMemoryCache(), TimeSpan.FromDays(1))
            };
        }

        protected MemoryCache GetMemoryCache() => new MemoryCache(new MemoryCacheOptions());

        /// <summary>
        /// Returns a profiler for <paramref name="url"/>. Only child steps will take any time, 
        /// e.g. when <paramref name="childDepth"/> is 0, the resulting <see cref="MiniProfiler.DurationMilliseconds"/> will be zero.
        /// </summary>
        /// <param name="url">The URL of the request.</param>
        /// <param name="childDepth">number of levels of child steps underneath result's <see cref="MiniProfiler.Root"/>.</param>
        /// <param name="stepMs">Amount of time each step will "do work for" in each step.</param>
        /// <returns>The generated <see cref="MiniProfiler"/>.</returns>
        public MiniProfiler GetProfiler(int childDepth = 0, int stepMs = StepTimeMilliseconds)
        {
            var result = Options.StartProfiler();
            AddRecursiveChildren(result, childDepth, stepMs);
            result.Stop();
            return result;
        }

        /// <summary>
        /// Returns a profiler for <paramref name="url"/>. Only child steps will take any time, 
        /// e.g. when <paramref name="childDepth"/> is 0, the resulting <see cref="MiniProfiler.DurationMilliseconds"/> will be zero.
        /// </summary>
        /// <param name="url">The URI of the request.</param>
        /// <param name="childDepth">number of levels of child steps underneath result's <see cref="MiniProfiler.Root"/></param>
        /// <param name="stepMs">Amount of time each step will "do work for" in each step</param>
        /// <returns>The generated <see cref="MiniProfiler"/>.</returns>
        public async Task<MiniProfiler> GetProfilerAsync(int childDepth = 0, int stepMs = StepTimeMilliseconds)
        {
            var result = Options.StartProfiler();
            AddRecursiveChildren(result, childDepth, stepMs);
            await result.StopAsync().ConfigureAwait(false);
            return result;
        }
    }
}
