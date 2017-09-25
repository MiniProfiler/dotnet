using System.Threading.Tasks;

namespace StackExchange.Profiling.Tests
{
    public static class TestExtensions
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
