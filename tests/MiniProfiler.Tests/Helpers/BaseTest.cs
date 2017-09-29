using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests
{
    public abstract class BaseTest
    {
        /// <summary>
        /// Amount of time each <see cref="MiniProfilerExtensions.Step"/> will take for unit tests.
        /// </summary>
        public const int StepTimeMilliseconds = 1;

        public const string NonParallel = nameof(NonParallel);

        protected MiniProfilerBaseOptions Options { get; set; }

        protected ITestOutputHelper Output { get; }

        protected BaseTest(ITestOutputHelper output)
        {
            Output = output;
            Options = new MiniProfilerBaseOptions()
            {
                StopwatchProvider = () => new UnitTestStopwatch()
            };
        }

        protected MiniProfiler GetBasicProfiler([CallerMemberName]string name = null)
        {
            var mp = Options.StartProfiler(name);
            using (mp.Step("Step 1"))
            {
                using (mp.CustomTiming("Custom", "Custom Command", "Test Exec"))
                {
                    Thread.Sleep(1);
                }
            }
            return mp;
        }

        /// <summary>
        /// Returns a profiler for <paramref name="url"/>. Only child steps will take any time, 
        /// e.g. when <paramref name="childDepth"/> is 0, the resulting <see cref="MiniProfiler.DurationMilliseconds"/> will be zero.
        /// </summary>
        /// <param name="url">The URL of the request.</param>
        /// <param name="childDepth">number of levels of child steps underneath result's <see cref="MiniProfiler.Root"/>.</param>
        /// <param name="stepMs">Amount of time each step will "do work for" in each step.</param>
        /// <returns>The generated <see cref="MiniProfiler"/>.</returns>
        protected virtual MiniProfiler GetProfiler(int childDepth = 0, int stepMs = StepTimeMilliseconds)
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
        protected virtual async Task<MiniProfiler> GetProfilerAsync(int childDepth = 0, int stepMs = StepTimeMilliseconds)
        {
            var result = Options.StartProfiler();
            AddRecursiveChildren(result, childDepth, stepMs);
            await result.StopAsync().ConfigureAwait(false);
            return result;
        }

        protected void AddRecursiveChildren(MiniProfiler profiler, int maxDepth, int stepMs, int curDepth = 0)
        {
            if (curDepth++ < maxDepth)
            {
                using (profiler.Step("Depth " + curDepth))
                {
                    profiler.Increment(stepMs);
                    AddRecursiveChildren(profiler, maxDepth, stepMs, curDepth);
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

        private DateTime TrimToDecisecond(DateTime dateTime) =>
            new DateTime(dateTime.Ticks - (dateTime.Ticks % (TimeSpan.TicksPerSecond / 10)));

        protected void AssertNear(double expected, decimal? actual, double maxDelta = 0.0001)
        {
            Assert.NotNull(actual);
            Assert.InRange((double)actual.Value, expected - maxDelta, expected + maxDelta);
            Output.WriteLine($"Assert.Near (Actual = {actual.Value}, Expected = {expected}, Tolerance = {maxDelta}");
        }
    }

    [CollectionDefinition(BaseTest.NonParallel, DisableParallelization = true)]
    public class NonParallelDefinition
    {
    }
}
