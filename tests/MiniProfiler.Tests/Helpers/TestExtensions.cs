using System;
using System.Runtime.CompilerServices;
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
        public static void Increment(this MiniProfiler? profiler, int milliseconds = BaseTest.StepTimeMilliseconds)
        {
            if (profiler?.GetStopwatch() is UnitTestStopwatch sw)
            {
                sw.ElapsedTicks += milliseconds * UnitTestStopwatch.TicksPerMillisecond;
            }
        }

        /// <summary>
        /// Increments the currently running <see cref="MiniProfiler.Stopwatch"/> by <paramref name="milliseconds"/>.
        /// </summary>
        /// <param name="profiler">The profile to increment.</param>
        /// <param name="milliseconds">The milliseconds.</param>
        public static Task IncrementAsync(this MiniProfiler? profiler, int milliseconds = BaseTest.StepTimeMilliseconds) =>
            profiler is null 
            ? Task.CompletedTask
            : Task.Run(() => Increment(profiler, milliseconds));

        internal static void MaybeLog(this Exception ex, string connectionString, [CallerFilePath] string? file = null, [CallerMemberName] string? caller = null)
        {
            if (TestConfig.Current.EnableTestLogging)
            {
                Console.WriteLine($"{file} {caller}: {ex.Message}");
                Console.WriteLine("  " + connectionString);
            }
        }
    }
}
