using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Profiling;

using Xunit;

namespace Tests.Async
{
    [CollectionDefinition(BaseTest.NonParallel, DisableParallelization = true)]
    public class NonParallelDefintion
    {
        public const string Name = "NonParallel";
    }

    [Collection(NonParallel)]
    public class AsyncTests : BaseTest
    {
        [Fact]
        public async Task SimpleAsync()
        {
            var profiler = Options.StartProfiler("root");

            // Add 100ms to root
            await profiler.IncrementAsync(100).ConfigureAwait(false);

            // 100ms + 100ms = 200ms
            var step1 = Task.Run(async () =>
            {
                using (profiler.Step("step1.0"))
                {
                    await profiler.IncrementAsync(100).ConfigureAwait(false);

                    await Task.Run(async () =>
                    {
                        using (profiler.Step("step1.1"))
                        {
                            await profiler.IncrementAsync(100).ConfigureAwait(false);
                        }
                    }).ConfigureAwait(false);
                }
            });

            // 100ms
            var step2 = Task.Run(async () =>
            {
                using (profiler.Step("step2.0"))
                {
                    await profiler.IncrementAsync(100).ConfigureAwait(false);
                }
            });

            // Longest task is 200ms
            await Task.WhenAll(step1, step2).ConfigureAwait(false);

            profiler.Stop();

            //Console.WriteLine(profiler.RenderPlainText());
            //   root = 330.9ms
            //  > step2.0 = 107.6ms
            //  > step1.0 = 212.1ms
            //  >> step1.1 = 107ms
        }

        [Fact]
        public async Task Step_WithParallelTasks_SimulatedTime()
        {
            var profiler = Options.StartProfiler("root");

            var waiters = new ConcurrentBag<CountdownEvent>();
            Timing timing10 = null, timing11 = null, timing20 = null, timing21 = null, timing30 = null, timing31 = null;

            // Add 1ms to root
            profiler.Increment();

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
                profiler.Increment();
                handlers.ForEach(x =>
                {
                    if (!x.IsSet) x.Signal();
                });

                // TODO Make this a thread safe signaling lock step to avoid sleeping
                // Wait for sub-tasks to run and call their Step() methods
                Thread.Sleep(50);
            }

            await whenAllTask;

            profiler.Stop();

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
    }
}