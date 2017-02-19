using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Profiling;
using StackExchange.Profiling.Helpers;

using Xunit;

namespace Tests
{
    public class MiniProfilerConcurrencyTest : BaseTest
    {
        [Fact]
        public async Task Step_WithParallelTasks_RealTime()
        {
            MiniProfiler.Settings.StopwatchProvider = StopwatchWrapper.StartNew;

            var profiler = MiniProfiler.Start("root");

            Timing timing10 = null, timing11 = null, timing20 = null, timing21 = null, timing30 = null, timing31 = null;

            // Act

            // Add 100ms to root
            await Task.Delay(100);

            // Start tasks in parallel
            var whenAllTask = Task.WhenAll(
                Task.Run(async () =>
                {
                    // timing10: 100 + 100 = 200 ms
                    using (timing10 = profiler.Step("step1.0 (Task.Run)"))
                    {
                        await Task.Delay(100);
                        await Task.Run(async () =>
                        {
                            using (timing11 = profiler.Step("step1.1 (Task.Run)"))
                            {
                                await Task.Delay(100);
                            }
                        });
                    }
                }),
                Task.Factory.StartNew(async () =>
                {
                    // timing20: 200 + 100 = 300 ms
                    using (timing20 = profiler.Step("step2.0 (Task.Factory.StartNew)"))
                    {
                        await Task.Delay(200);
                        await Task.Run(async () =>
                        {
                            using (timing21 = profiler.Step("step2.1 (Task.Run)"))
                            {
                                await Task.Delay(100);
                            }
                        });
                    }
                    // Important to Unwrap() when using the not-for-mortals StartNew()
                }).Unwrap(),
                Task.Factory.StartNew(async () =>
                {
                    // timing30: 300 + 100 = 400 ms
                    using (timing30 = profiler.Step("step3.0 (Task.Factory.StartNew:LongRunning)"))
                    {
                        await Task.Delay(300);
                        await Task.Run(async () =>
                        {
                            using (timing31 = profiler.Step("step3.1 (Task.Run)"))
                            {
                                await Task.Delay(100);
                            }
                        });
                    }
                    // Important to Unwrap() when using the not-for-mortals StartNew()
                }, TaskCreationOptions.LongRunning).Unwrap()
            );

            await whenAllTask;

            MiniProfiler.Stop();

            // Assert
            Console.WriteLine(profiler.RenderPlainText());

            // 100ms + longest running task (step3.0 with 300 + 100 ms) = 500ms
            AssertNear(500, profiler.DurationMilliseconds, 50);

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
            MiniProfiler.Settings.StopwatchProvider = StopwatchWrapper.StartNew;
            var profiler = MiniProfiler.Start("root");

            // Act

            // Add 100ms to root just to offset the starting point
            Task.Delay(100).Wait();

            // Run up to 10 threads at a time (system and scheduler dependent),
            // each waiting 10 * 50 ms = 500 ms
            Parallel.For(0, 10, i =>
            {
                using (profiler.Step($"thread[{i}]"))
                {
                    foreach (int j in Enumerable.Range(0, 10))
                    {
                        using (profiler.Step($"work[{i}/{j}]"))
                        {
                            Thread.Sleep(50);
                        }
                    }
                }
            });

            MiniProfiler.Stop();

            // Assert
            Console.WriteLine(profiler.RenderPlainText());

            // The total run time is non-deterministic and depends
            // on the system and the scheduler, so we can only assert
            // each thread's duration
            foreach (var timing in profiler.GetTimingHierarchy())
            {

                if (timing.Name.StartsWith("thread"))
                {
                    // 10 work items, 50 ms each
                    AssertNear(500, timing.DurationMilliseconds, 20);
                }
                else if (timing.Name.StartsWith("work"))
                {
                    // 50 ms each work item
                    AssertNear(50, timing.DurationMilliseconds, 20);
                }
            }
        }

        [Fact]
        public async Task Step_WithParallelTasks_SimulatedTime()
        {
            MiniProfiler.Settings.StopwatchProvider = () => new UnitTestStopwatch();
            var profiler = MiniProfiler.Start("root");

            var waiters = new ConcurrentBag<CountdownEvent>();
            Timing timing10 = null, timing11 = null, timing20 = null, timing21 = null, timing30 = null, timing31 = null;

            // Act

            // Add 1ms to root
            IncrementStopwatch();

            // Start tasks in parallel
            var whenAllTask = Task.WhenAll(
                Task.Run(async () =>
                {
                    // timing10: 1 + 1 = 2 ms
                    using (timing10 = profiler.Step("step1.0 (Task.Run)"))
                    {

                        var ce = new CountdownEvent(1);
                        waiters.Add(ce);
                        ce.Wait();

                        await Task.Run(() =>
                        {
                            using (timing11 = profiler.Step("step1.1 (Task.Run)"))
                            {
                                var ce2 = new CountdownEvent(1);
                                waiters.Add(ce2);
                                ce2.Wait();
                            }
                        });
                    }
                }),
                Task.Factory.StartNew(async () =>
                {
                    // timing20: 2 + 1 = 2 ms
                    using (timing20 = profiler.Step("step2.0 (Task.Factory.StartNew)"))
                    {
                        var ce = new CountdownEvent(2);
                        waiters.Add(ce);
                        ce.Wait();

                        await Task.Run(() =>
                        {
                            using (timing21 = profiler.Step("step2.1 (Task.Run)"))
                            {
                                var ce2 = new CountdownEvent(1);
                                waiters.Add(ce2);
                                ce2.Wait();
                            }
                        });
                    }
                }),
                Task.Factory.StartNew(async () =>
                {
                    // timing20: 3 + 1 = 2 ms
                    using (timing30 = profiler.Step("step3.0 (Task.Factory.StartNew:LongRunning)"))
                    {
                        var ce = new CountdownEvent(3);
                        waiters.Add(ce);
                        ce.Wait();

                        await Task.Run(() =>
                        {
                            using (timing31 = profiler.Step("step3.1 (Task.Run)"))
                            {
                                var ce2 = new CountdownEvent(1);
                                waiters.Add(ce2);
                                ce2.Wait();
                            }
                        });
                    }
                }, TaskCreationOptions.LongRunning)
            );

            Func<List<CountdownEvent>, bool> hasPendingTasks =
                handlers2 => (handlers2.Count == 0) || handlers2.Any(y => !y.IsSet);

            // TODO Make this a thread safe signaling lock step to avoid sleeping
            // Wait for tasks to run and call their Step() methods
            Thread.Sleep(50);

            List<CountdownEvent> handlers;
            while (hasPendingTasks(handlers = waiters.ToList()))
            {
                IncrementStopwatch();
                handlers.ForEach(x =>
                {
                    if (!x.IsSet) x.Signal();
                });

                // TODO Make this a thread safe signaling lock step to avoid sleeping
                // Wait for sub-tasks to run and call their Step() methods
                Thread.Sleep(50);
            }

            await whenAllTask;

            MiniProfiler.Stop();

            // Assert
            Console.WriteLine(profiler.RenderPlainText());

            // 1ms added to root
            AssertNear(5, profiler.DurationMilliseconds);

            // Parent durations are sum of itself and children
            AssertNear(2, timing10.DurationMilliseconds);
            AssertNear(1, timing11.DurationMilliseconds);

            AssertNear(3, timing20.DurationMilliseconds);
            AssertNear(1, timing21.DurationMilliseconds);

            AssertNear(4, timing30.DurationMilliseconds);
            AssertNear(1, timing31.DurationMilliseconds);
        }

        private static void AssertNear(double expected, decimal? actual, double maxDelta = 0.0001)
        {
            Assert.NotNull(actual);
            Assert.InRange((double)actual.Value, expected - maxDelta, expected + maxDelta);
        }
    }
}