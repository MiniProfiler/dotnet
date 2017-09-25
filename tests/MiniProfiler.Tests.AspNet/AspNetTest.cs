using StackExchange.Profiling.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests
{
    [Collection("Storage")]
    public abstract class AspNetTest : BaseTest
    {
        /// <summary>
        /// Url that <see cref="GetRequest"/> and <see cref="GetProfiler"/> will hit.
        /// </summary>
        public const string DefaultRequestUrl = "http://localhost/Test.aspx";
        //protected MiniProfilerOptions Options { get; set; }

        protected AspNetTest(ITestOutputHelper output) : base(output)
        {
            ThreadPool.SetMinThreads(50, 50);
            // Instance per class, so multiple tests swapping the provider don't cause issues here
            // It's not a threading issue of the profiler, but rather tests swapping providers
            Options = new MiniProfilerOptions()
            {
                StopwatchProvider = () => new UnitTestStopwatch(),
                Storage = new MemoryCacheStorage(TimeSpan.FromDays(1))
            };
            // To reset the static specifically, can probably remove this...
            //Options.SetProvider(new DefaultProfilerProvider());
        }

        /// <summary>
        /// Returns a simulated http request to <paramref name="url"/>.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <param name="startAndStopProfiler">The start And Stop Profiler.</param>
        /// <returns>the request</returns>
        public IDisposable GetRequest(string url = DefaultRequestUrl, bool startAndStopProfiler = true)
        {
            var result = new Subtext.TestLibrary.HttpSimulator();

            result.SimulateRequest(new Uri(url));

            if (startAndStopProfiler)
            {
                var mp = Options.StartProfiler();
                result.OnBeforeDispose += () => mp.Stop();
            }

            return result;
        }

        /// <summary>
        /// Returns a profiler for <paramref name="url"/>. Only child steps will take any time, 
        /// e.g. when <paramref name="childDepth"/> is 0, the resulting <see cref="MiniProfiler.DurationMilliseconds"/> will be zero.
        /// </summary>
        /// <param name="url">The uri of the request.</param>
        /// <param name="childDepth">number of levels of child steps underneath result's <see cref="MiniProfiler.Root"/>.</param>
        /// <param name="stepsEachTakeMilliseconds">Amount of time each step will "do work for" in each step.</param>
        /// <returns>The generated <see cref="MiniProfiler"/>.</returns>
        public MiniProfiler GetProfiler(
            string url = DefaultRequestUrl,
            int childDepth = 0,
            int stepsEachTakeMilliseconds = StepTimeMilliseconds)
        {
            MiniProfiler result = null;
            Action step = null;
            var curDepth = 0;

            // recursively add child steps
            step = () =>
            {
                if (curDepth++ < childDepth)
                {
                    using (result.Step("Depth " + curDepth))
                    {
                        result.Increment(stepsEachTakeMilliseconds);
                        step();
                    }
                }
            };

            using (GetRequest(url, startAndStopProfiler: false))
            {
                result = Options.StartProfiler(url);
                step();

                result.Stop();
            }

            return result;
        }

        /// <summary>
        /// Returns a profiler for <paramref name="url"/>. Only child steps will take any time, 
        /// e.g. when <paramref name="childDepth"/> is 0, the resulting <see cref="MiniProfiler.DurationMilliseconds"/> will be zero.
        /// </summary>
        /// <param name="url">The uri of the request.</param>
        /// <param name="childDepth">number of levels of child steps underneath result's <see cref="MiniProfiler.Root"/></param>
        /// <param name="stepsEachTakeMilliseconds">Amount of time each step will "do work for" in each step</param>
        /// <returns>The generated <see cref="MiniProfiler"/>.</returns>
        public async Task<MiniProfiler> GetProfilerAsync(
            string url = DefaultRequestUrl,
            int childDepth = 0,
            int stepsEachTakeMilliseconds = StepTimeMilliseconds)
        {
            // TODO: Consolidate with above, maybe some out params
            MiniProfiler result = null;
            Action step = null;
            var curDepth = 0;

            // recursively add child steps
            step = () =>
            {
                if (curDepth++ < childDepth)
                {
                    using (result.Step("Depth " + curDepth))
                    {
                        result.Increment(stepsEachTakeMilliseconds);
                        step();
                    }
                }
            };

            using (GetRequest(url, startAndStopProfiler: false))
            {
                result = Options.StartProfiler();
                step();

                await result.StopAsync().ConfigureAwait(false);
            }

            return result;
        }
    }
}
