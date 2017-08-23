using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Profiling;
using StackExchange.Profiling.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Async
{
    [Collection(NonParallel)]
    public class AsyncRealTimeTests : BaseTest
    {
        public AsyncRealTimeTests(ITestOutputHelper output) : base(output)
        {
            Options.SetProvider(new DefaultProfilerProvider());
            Options.StopwatchProvider = StopwatchWrapper.StartNew;
        }

        [Fact]
        public async Task Step_WithParallelTasks_RealTime()
        {
            Thread.Sleep(1000); // calm down there stupid laptop
            var profiler = Options.StartProfiler("root");

            Timing timing10 = null,
                timing11 = null,
                timing20 = null,
                timing21 = null,
                timing30 = null,
                timing31 = null;

            // Add 100ms to root
            await Task.Delay(100).ConfigureAwait(false);

            // Start tasks in parallel
            var whenAllTask = Task.WhenAll(
                Task.Run(async () =>
                {
                    // timing10: 100 + 100 = 200 ms
                    using (timing10 = MiniProfiler.Current.Step("step1.0 (Task.Run)"))
                    {
                        await Task.Delay(100).ConfigureAwait(false);

                        await Task.Run(async () =>
                        {
                            using (timing11 = MiniProfiler.Current.Step("step1.1 (Task.Run)"))
                            {
                                await Task.Delay(100).ConfigureAwait(false);
                            }
                        }).ConfigureAwait(false);
                    }
                }),
                Task.Factory.StartNew(async () =>
                {
                    // timing20: 200 + 100 = 300 ms
                    using (timing20 = MiniProfiler.Current.Step("step2.0 (Task.Factory.StartNew)"))
                    {
                        await Task.Delay(200).ConfigureAwait(false);

                        await Task.Run(async () =>
                        {
                            using (timing21 = MiniProfiler.Current.Step("step2.1 (Task.Run)"))
                            {
                                await Task.Delay(100).ConfigureAwait(false);
                            }
                        }).ConfigureAwait(false);
                    }
                    // Important to Unwrap() when using the not-for-mortals StartNew()
                }).Unwrap(),
                Task.Factory.StartNew(async () =>
                {
                    // timing30: 300 + 100 = 400 ms
                    using (timing30 = MiniProfiler.Current.Step("step3.0 (Task.Factory.StartNew:LongRunning)"))
                    {
                        await Task.Delay(300).ConfigureAwait(false);

                        await Task.Run(async () =>
                        {
                            using (timing31 = MiniProfiler.Current.Step("step3.1 (Task.Run)"))
                            {
                                await Task.Delay(100).ConfigureAwait(false);
                            }
                        }).ConfigureAwait(false);
                    }
                    // Important to Unwrap() when using the not-for-mortals StartNew()
                }, TaskCreationOptions.LongRunning).Unwrap()
            );

            await whenAllTask;

            profiler.Stop();

            // Full diagnostic output
            Output.WriteLine(profiler.RenderPlainText());

            // 100ms + longest running task (step3.0 with 300 + 100 ms) = 500ms
            AssertNear(500, profiler.DurationMilliseconds, 125);

            // Parent durations are sum of itself and children
            AssertNear(200, timing10.DurationMilliseconds, 50);
            AssertNear(100, timing11.DurationMilliseconds, 50);

            AssertNear(300, timing20.DurationMilliseconds, 50);
            AssertNear(100, timing21.DurationMilliseconds, 50);

            AssertNear(400, timing30.DurationMilliseconds, 50);
            AssertNear(100, timing31.DurationMilliseconds, 50);
        }

        [Fact]
        public void Step_WithParallelThreads_RealTime()
        {
            var profiler = Options.StartProfiler("root");

            // Add 100ms to root just to offset the starting point
            Thread.Sleep(100);

            // Run up to 3 threads at a time (system and scheduler dependent),
            // each waiting 10 * 50 ms = 500 ms
            Parallel.For(0, 3, i =>
            {
                using (profiler.Step($"thread[{i}]"))
                {
                    foreach (int j in Enumerable.Range(0, 5))
                    {
                        using (profiler.Step($"work[{i}/{j}]"))
                        {
                            Thread.Sleep(50);
                        }
                    }
                }
            });

            profiler.Stop();

            Output.WriteLine(profiler.RenderPlainText());

            // The total run time is non-deterministic and depends
            // on the system and the scheduler, so we can only assert
            // each thread's duration
            var hierarchy = profiler.GetTimingHierarchy().ToList();
            foreach (var timing in hierarchy)
            {
                if (timing.Name.StartsWith("thread", StringComparison.Ordinal))
                {
                    // 3 work items, 50 ms each
                    AssertNear(250, timing.DurationMilliseconds, 100);
                }
                else if (timing.Name.StartsWith("work", StringComparison.Ordinal))
                {
                    // 50 ms each work item
                    AssertNear(50, timing.DurationMilliseconds, 20);
                }
            }
        }
    }
}