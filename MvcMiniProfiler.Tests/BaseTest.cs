using System;

namespace MvcMiniProfiler.Tests
{
    public abstract class BaseTest
    {
        /// <summary>
        /// Amount of time each <see cref="MiniProfilerExtensions.Step"/> will take for unit tests.
        /// </summary>
        public const int StepTimeMilliseconds = 1;

        static BaseTest()
        {
            // allows us to manually set ticks during tests
            MiniProfiler.Settings.StopwatchProvider = () => new UnitTestStopwatch();
        }

        public static IDisposable SimulateRequest(string url)
        {
            var result = new Subtext.TestLibrary.HttpSimulator();

            result.SimulateRequest(new Uri(url));

            return result;
        }

        /// <summary>
        /// Returns a profiler for <paramref name="url"/>. Only child steps will take any time, e.g. when <paramref name="childDepth"/> is 0, the
        /// resulting <see cref="MiniProfiler.DurationMilliseconds"/> will be zero.
        /// </summary>
        /// <param name="childDepth">number of levels of child steps underneath result's <see cref="MiniProfiler.Root"/></param>
        /// <param name="stepsEachTakeMilliseconds">Amount of time each step will "do work for" in each step</param>
        public static MiniProfiler GetProfiler(string url = "http://localhost/Test.aspx", int childDepth = 0, int stepsEachTakeMilliseconds = StepTimeMilliseconds)
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
                        IncrementStopwatch(stepsEachTakeMilliseconds);
                        step();
                    }
                }
            };

            using (SimulateRequest(url))
            {
                result = MiniProfiler.Start();
                step();
                MiniProfiler.Stop();
            }

            return result;
        }

        /// <summary>
        /// Increments the currently running <see cref="MiniProfiler.Stopwatch"/> by <paramref name="milliseconds"/>.
        /// </summary>
        /// <param name="milliseconds"></param>
        public static void IncrementStopwatch(int milliseconds = StepTimeMilliseconds)
        {
            var sw = (UnitTestStopwatch)MiniProfiler.Current.Stopwatch;
            sw.ElapsedTicks += milliseconds * UnitTestStopwatch.TicksPerMillisecond;
        }
    }

    public class UnitTestStopwatch : Helpers.IStopwatch
    {
        bool _isRunning = true;

        public long ElapsedTicks { get; set; }

        public static readonly long TicksPerSecond = TimeSpan.FromSeconds(1).Ticks;
        public static readonly long TicksPerMillisecond = TimeSpan.FromMilliseconds(1).Ticks;

        /// <summary>
        /// <see cref="MiniProfiler.GetRoundedMilliseconds"/> method will use this to determine how many ticks actually elapsed, so make it simple.
        /// </summary>
        public long Frequency
        {
            get { return TicksPerSecond; }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public void Stop()
        {
            _isRunning = false;
        }

    }

}
