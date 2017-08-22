using StackExchange.Profiling;
using StackExchange.Profiling.Storage;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    [Collection("Storage")]
    public abstract class BaseTest
    {
        public const string NonParallel = nameof(NonParallel);

        /// <summary>
        /// Amount of time each <see cref="MiniProfilerExtensions.Step"/> will take for unit tests.
        /// </summary>
        public const int StepTimeMilliseconds = 1;

        /// <summary>
        /// Url that <see cref="GetRequest"/> and <see cref="GetProfiler"/> will hit.
        /// </summary>
        public const string DefaultRequestUrl = "http://localhost/Test.aspx";

        protected IAsyncStorage _testStorage { get; set; }
        protected MiniProfilerOptions Options { get; set; }

        protected ITestOutputHelper Output { get; }

        protected BaseTest(ITestOutputHelper output) : this()
        {
            ThreadPool.SetMinThreads(50, 50);
            Output = output;
        }

        // Reset for each inheritor
        protected BaseTest()
        {
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

                if (_testStorage != null)
                {
                    result.Storage = _testStorage;
                }

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

                if (_testStorage != null)
                {
                    result.Storage = _testStorage;
                }

                await result.StopAsync().ConfigureAwait(false);
            }

            return result;
        }

        public void AssertProfilersAreEqual(MiniProfiler mp1, MiniProfiler mp2)
        {
            Assert.Equal(mp1, mp2);
            AssertPublicPropertiesAreEqual(mp1, mp2);
            AssertTimingsAreEqualAndRecurse(mp1.Root, mp2.Root);
        }

        protected void AssertTimingsAreEqualAndRecurse(Timing t1, Timing t2)
        {
            Assert.NotNull(t1);
            Assert.NotNull(t2);

            AssertPublicPropertiesAreEqual(t1, t2);

            if (t1.CustomTimings != null || t2.CustomTimings != null)
            {
                Assert.NotNull(t1.CustomTimings);
                Assert.NotNull(t2.CustomTimings);

                Assert.Equal(t1.CustomTimings.Count, t2.CustomTimings.Count);

                foreach (var pair1 in t1.CustomTimings)
                {
                    var ct1 = pair1.Value;
                    Assert.True(t2.CustomTimings.TryGetValue(pair1.Key, out var ct2));

                    for (int i = 0; i < ct1.Count; i++)
                    {
                        AssertPublicPropertiesAreEqual(ct1[i], ct2[i]);
                    }
                }
            }

            if (t1.Children != null || t2.Children != null)
            {
                Assert.NotNull(t1.Children);
                Assert.NotNull(t2.Children);

                Assert.Equal(t1.Children.Count, t2.Children.Count);

                for (int i = 0; i < t1.Children.Count; i++)
                {
                    AssertTimingsAreEqualAndRecurse(t1.Children[i], t2.Children[i]);
                }
            }
        }

        /// <summary>
        /// Doesn't handle collection properties!
        /// </summary>
        /// <typeparam name="T">The argument type to compare.</typeparam>
        /// <param name="t1">The first object to compare.</param>
        /// <param name="t2">The second object to compare.</param>
        protected void AssertPublicPropertiesAreEqual<T>(T t1, T t2) where T : class
        {
            Assert.NotNull(t1);
            Assert.NotNull(t2);

            // we'll handle any collections elsewhere
            var props = from p in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        where p.IsDefined(typeof(System.Runtime.Serialization.DataMemberAttribute), false)
                        && !p.PropertyType.GetInterfaces().Any(i => i.Equals(typeof(IDictionary)) || i.Equals(typeof(IList)))
                        select p;

            foreach (var p in props)
            {
                try
                {
                    var val1 = p.GetValue(t1, null);
                    var val2 = p.GetValue(t2, null);

                    // datetimes are sometimes serialized with different precisions - just look care about the 10th of a second
                    if (p.PropertyType == typeof(DateTime))
                    {
                        val1 = TrimToDecisecond((DateTime)val1);
                        val2 = TrimToDecisecond((DateTime)val2);
                    }

                    var name = typeof(T).Name + "." + p.Name;
                    Assert.True(Equals(val1, val2), $"{name} have different values ({val1} vs. {val2}");
                    //Console.WriteLine("{0, 50}: {1} == {2}", name, val1 ?? "<null>", val2 ?? "<null>");
                }
                catch (Xunit.Sdk.TrueException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Assert.True(false, "AssertPublicPropertiesAreEqual had an exception on " + p.Name + "; " + ex);
                }
            }
        }

        private DateTime TrimToDecisecond(DateTime dateTime) =>
            new DateTime(dateTime.Ticks - (dateTime.Ticks % (TimeSpan.TicksPerSecond / 10)));

        protected void AssertNear(double expected, decimal? actual, double maxDelta = 0.0001)
        {
            Assert.NotNull(actual);
            Assert.InRange((double)actual.Value, expected - maxDelta, expected + maxDelta);
            Output.WriteLine($"Assert.Near (Actual = {actual.Value}, Expected = {expected}, Tolerance = {maxDelta}");
        }
    }

    internal static class TestExtensions
    {
        /// <summary>
        /// Increments the currently running <see cref="MiniProfiler.Stopwatch"/> by <paramref name="milliseconds"/>.
        /// </summary>
        /// <param name="profiler">The profile to increment.</param>
        /// <param name="milliseconds">The milliseconds.</param>
        public static void Increment(this MiniProfiler profiler, int milliseconds = BaseTest.StepTimeMilliseconds)
        {
            var sw = (UnitTestStopwatch)profiler.GetStopwatch();
            sw.ElapsedTicks += milliseconds * UnitTestStopwatch.TicksPerMillisecond;
        }

        /// <summary>
        /// Increments the currently running <see cref="MiniProfiler.Stopwatch"/> by <paramref name="milliseconds"/>.
        /// </summary>
        /// <param name="profiler">The profile to increment.</param>
        /// <param name="milliseconds">The milliseconds.</param>
        public static Task IncrementAsync(this MiniProfiler profiler, int milliseconds = BaseTest.StepTimeMilliseconds) =>
            Task.Run(() => Increment(profiler, milliseconds));
    }
}
