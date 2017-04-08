using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Profiling;
using StackExchange.Profiling.Helpers;

using Xunit;

namespace Tests.Async
{
    public class AsyncTests : BaseTest
    {
        [Fact]
        public async Task SimpleAsync()
        {
            var profiler = MiniProfiler.Start("root");

            // Add 100ms to root
            await IncrementAsync(100).ConfigureAwait(false);

            // 100ms + 100ms = 200ms
            var step1 = Task.Run(async () =>
            {
                using (profiler.Step("step1.0"))
                {
                    await IncrementAsync(100).ConfigureAwait(false);

                    await Task.Run(async () =>
                    {
                        using (profiler.Step("step1.1"))
                        {
                            await IncrementAsync(100).ConfigureAwait(false);
                        }
                    }).ConfigureAwait(false);
                }
            });

            // 100ms
            var step2 = Task.Run(async () =>
            {
                using (profiler.Step("step2.0"))
                {
                    await IncrementAsync(100).ConfigureAwait(false);
                }
            });

            // Longest task is 200ms
            await Task.WhenAll(step1, step2).ConfigureAwait(false);

            MiniProfiler.Stop();

            //Console.WriteLine(profiler.RenderPlainText());
            //   root = 330.9ms
            //  > step2.0 = 107.6ms
            //  > step1.0 = 212.1ms
            //  >> step1.1 = 107ms
        }

        [Fact]
        public async Task Step_WithParallelTasks_RealTime()
        {
            var profiler = MiniProfiler.Start("root");
            profiler.Stopwatch = StopwatchWrapper.StartNew();

            Timing timing10 = null,
                timing11 = null,
                timing20 = null,
                timing21 = null,
                timing30 = null,
                timing31 = null;

            // Act

            // Add 100ms to root
            await Task.Delay(100).ConfigureAwait(false);

            // Start tasks in parallel
            var whenAllTask = Task.WhenAll(
                Task.Run(async () =>
                {
                    // timing10: 100 + 100 = 200 ms
                    using (timing10 = profiler.Step("step1.0 (Task.Run)"))
                    {
                        await Task.Delay(100).ConfigureAwait(false);
                        await Task.Run(async () =>
                        {
                            using (timing11 = profiler.Step("step1.1 (Task.Run)"))
                            {
                                await Task.Delay(100).ConfigureAwait(false);
                            }
                        }).ConfigureAwait(false);
                    }
                }),
                Task.Factory.StartNew(async () =>
                {
                    // timing20: 200 + 100 = 300 ms
                    using (timing20 = profiler.Step("step2.0 (Task.Factory.StartNew)"))
                    {
                        await Task.Delay(200).ConfigureAwait(false);
                        await Task.Run(async () =>
                        {
                            using (timing21 = profiler.Step("step2.1 (Task.Run)"))
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
                    using (timing30 = profiler.Step("step3.0 (Task.Factory.StartNew:LongRunning)"))
                    {
                        await Task.Delay(300).ConfigureAwait(false);
                        await Task.Run(async () =>
                        {
                            using (timing31 = profiler.Step("step3.1 (Task.Run)"))
                            {
                                await Task.Delay(100).ConfigureAwait(false);
                            }
                        }).ConfigureAwait(false);
                    }
                    // Important to Unwrap() when using the not-for-mortals StartNew()
                }, TaskCreationOptions.LongRunning).Unwrap()
            );

            await whenAllTask;

            MiniProfiler.Stop();

            // Assert
            //Console.WriteLine(profiler.RenderPlainText());

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
            var profiler = MiniProfiler.Start("root");
            // Need real wall-time here - hard to simulate in a fake
            profiler.Stopwatch = StopwatchWrapper.StartNew();

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

            MiniProfiler.Stop();

            // Assert
            //Console.WriteLine(profiler.RenderPlainText());

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

        [Fact]
        public async Task Step_WithParallelTasks_SimulatedTime()
        {
            var profiler = MiniProfiler.Start("root");

            var waiters = new ConcurrentBag<CountdownEvent>();
            Timing timing10 = null, timing11 = null, timing20 = null, timing21 = null, timing30 = null, timing31 = null;

            // Add 1ms to root
            Increment();

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
                        }).ConfigureAwait(false);
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
                        }).ConfigureAwait(false);
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
                        }).ConfigureAwait(false);
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
                Increment();
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
            //Console.WriteLine(profiler.RenderPlainText());

            // 1ms added to root
            AssertNear(5, profiler.DurationMilliseconds, maxDelta: 2);

            // Parent durations are sum of itself and children
            AssertNear(2, timing10.DurationMilliseconds, maxDelta: 2);
            AssertNear(1, timing11.DurationMilliseconds, maxDelta: 2);

            AssertNear(3, timing20.DurationMilliseconds, maxDelta: 2);
            AssertNear(1, timing21.DurationMilliseconds, maxDelta: 2);

            AssertNear(4, timing30.DurationMilliseconds, maxDelta: 2);
            AssertNear(1, timing31.DurationMilliseconds, maxDelta: 2);
        }

        private static void AssertNear(double expected, decimal? actual, double maxDelta = 0.0001)
        {
            Assert.NotNull(actual);
            Assert.InRange((double)actual.Value, expected - maxDelta, expected + maxDelta);
        }
    }
}