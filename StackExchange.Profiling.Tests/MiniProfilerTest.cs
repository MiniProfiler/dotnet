using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using NUnit.Framework;

namespace StackExchange.Profiling.Tests
{
    [TestFixture]
    public class MiniProfilerTest : BaseTest
    {
        [Test]
        public void Simple()
        {
            using (GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                MiniProfiler.Start();
                IncrementStopwatch(); // 1 ms
                MiniProfiler.Stop();

                var c = MiniProfiler.Current;

                Assert.That(c, Is.Not.Null);
                Assert.That(c.DurationMilliseconds, Is.EqualTo(StepTimeMilliseconds));
                Assert.That(c.Name, Is.EqualTo("/Test.aspx"));

                Assert.That(c.Root, Is.Not.Null);
                Assert.That(c.Root.HasChildren, Is.False);
            }
        }

        [Test]
        public void WhenUsingAsyncProvider_SimpleCaseWorking()
        {
            MiniProfiler.Settings.ProfilerProvider = new AsyncWebRequestProfilerProvider();
            using (GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                MiniProfiler.Start();
                IncrementStopwatch(); // 1 ms
                MiniProfiler.Stop();

                var c = MiniProfiler.Current;

                Assert.That(c, Is.Not.Null);
                Assert.That(c.DurationMilliseconds, Is.EqualTo(StepTimeMilliseconds));
                Assert.That(c.Name, Is.EqualTo("/Test.aspx"));

                Assert.That(c.Root, Is.Not.Null);
                Assert.That(c.Root.HasChildren, Is.False);
            }
        }

        [Test]
        public async void Current_WhenAsyncMethodReturns_IsCarried(
            [Values(true,false)]bool comfigureAwait
            )
        {
            MiniProfiler.Settings.ProfilerProvider = new AsyncWebRequestProfilerProvider();
            using (GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                MiniProfiler.Start();

                var c = MiniProfiler.Current;
                await Task.Delay(TimeSpan.FromMilliseconds(1)).ConfigureAwait(comfigureAwait);
                Assert.That(HttpContext.Current, Is.Null);

                Assert.That(MiniProfiler.Current, Is.Not.Null);
                Assert.That(MiniProfiler.Current, Is.EqualTo(c));
            }
        }

        [Test]
        public async void Head_WhenAsyncMethodReturns_IsCarried(
            [Values(true,false)]bool comfigureAwait
            )
        {
            MiniProfiler.Settings.ProfilerProvider = new AsyncWebRequestProfilerProvider();
            using (GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                MiniProfiler.Start();
                var sut = MiniProfiler.Current;
                var head = sut.Head;

                await Task.Delay(TimeSpan.FromMilliseconds(1)).ConfigureAwait(comfigureAwait);
                Assert.That(HttpContext.Current, Is.Null);

                Assert.That(sut.Head, Is.Not.Null);
                Assert.That(sut.Head, Is.EqualTo(head));
            }
        }

        [Test]
        public async void Head_WhenMultipleTasksSpawned_EachSetsItsOwnHead(
            [Values(true,false)]bool comfigureAwait
            )
        {
            MiniProfiler.Settings.ProfilerProvider = new AsyncWebRequestProfilerProvider();
            var allTasks = new SemaphoreSlim(0, 1);
            var completed = new TaskCompletionSource<int>();
            using (GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                MiniProfiler.Start();
                var sut = MiniProfiler.Current;
                var head = sut.Head;

                Task.Run(() => {
                    Assert.That(sut.Head, Is.EqualTo(head));
                    using (sut.Step("test1"))
                    {
                        allTasks.Release();
                        completed.Task.Wait();
                    }
                }).ConfigureAwait(comfigureAwait);
                allTasks.Wait();
                Task.Run(() => {
                    using (sut.Step("test2"))
                    {
                        allTasks.Release();
                        completed.Task.Wait();
                    }
                }).ConfigureAwait(comfigureAwait);
                allTasks.Wait();
                Assert.That(sut.Head, Is.Not.Null);
                Assert.That(sut.Head, Is.EqualTo(head));
                completed.SetResult(0);
            }
        }

        [Test]
        public void StepIf_Basic()
        {
            using (GetRequest())
            {
                MiniProfiler.Start();
                var mp1 = MiniProfiler.Current;

                IncrementStopwatch(); // 1 ms
                Timing goodTiming;
                Timing badTiming;

                using (goodTiming = (Timing)(mp1.StepIf("Yes", 1)))
                {
                    IncrementStopwatch(2);
                }
                using (badTiming = (Timing)(mp1.StepIf("No", 5)))
                {
                    IncrementStopwatch(); // 1 ms
                }
                MiniProfiler.Stop();

                Assert.IsTrue(mp1.Root.Children.Contains(goodTiming));
                Assert.IsTrue(!mp1.Root.Children.Contains(badTiming));
            }
        }

        [Test]
        public void StepIf_IncludeChildren()
        {
            using (GetRequest())
            {
                MiniProfiler.Start();
                var mp1 = MiniProfiler.Current;

                IncrementStopwatch(); // 1 ms
                Timing goodTiming;
                Timing badTiming;

                using (goodTiming = (Timing)(mp1.StepIf("Yes", 5, true)))
                {
                    IncrementStopwatch(2);
                    using (mp1.Step("#1"))
                    {
                        IncrementStopwatch(2);
                    }
                    using (mp1.Step("#2"))
                    {
                        IncrementStopwatch(2);
                    }
                }
                using (badTiming = (Timing)(mp1.StepIf("No", 5, false)))
                {
                    IncrementStopwatch(2);
                    using (mp1.Step("#1"))
                    {
                        IncrementStopwatch(2);
                    }
                    using (mp1.Step("#2"))
                    {
                        IncrementStopwatch(2);
                    }
                }
                MiniProfiler.Stop();

                Assert.IsTrue(mp1.Root.Children.Contains(goodTiming));
                Assert.IsTrue(!mp1.Root.Children.Contains(badTiming));
            }
        }

        [Test]
        public void CustomTimingIf_Basic()
        {
            using (GetRequest())
            {
                MiniProfiler.Start();
                var mp1 = MiniProfiler.Current;

                IncrementStopwatch(); // 1 ms
                CustomTiming goodTiming;
                CustomTiming badTiming;

                using (goodTiming = mp1.CustomTimingIf("Cat1", "Yes", 1))
                {
                    IncrementStopwatch(2);
                }
                using (badTiming = mp1.CustomTimingIf("Cat1", "No", 5))
                {
                    IncrementStopwatch(); // 1 ms
                }
                MiniProfiler.Stop();

                Assert.IsTrue(mp1.Root.CustomTimings["Cat1"].Contains(goodTiming));
                Assert.IsTrue(!mp1.Root.CustomTimings["Cat1"].Contains(badTiming));
            }
        }

        [Test]
        public void DiscardResults()
        {
            using (GetRequest(startAndStopProfiler: false))
            {
                MiniProfiler.Start();
                MiniProfiler.Stop(discardResults: true);

                var c = MiniProfiler.Current;

                Assert.That(c, Is.Null);
            }
        }
    }
}
