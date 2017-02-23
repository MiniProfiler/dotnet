using StackExchange.Profiling.Helpers;
using System;

namespace Tests
{
    /// <summary>
    /// The unit test stopwatch.
    /// </summary>
    public class UnitTestStopwatch : IStopwatch
    {
        private bool _isRunning = true;

        /// <summary>
        /// The ticks per second.
        /// </summary>
        public static readonly long TicksPerSecond = TimeSpan.TicksPerSecond;

        /// <summary>
        /// The ticks per millisecond.
        /// </summary>
        public static readonly long TicksPerMillisecond = TimeSpan.TicksPerMillisecond;

        /// <summary>
        /// Gets or sets the elapsed ticks.
        /// </summary>
        public long ElapsedTicks { get; set; }

        /// <summary>
        /// Gets the frequency, <see cref="MiniProfiler.GetRoundedMilliseconds"/> method will use this to determine how many ticks actually elapsed, so make it simple.
        /// </summary>
        public long Frequency => TicksPerSecond;

        /// <summary>
        /// Gets a value indicating whether is running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Stop the profiler.
        /// </summary>
        public void Stop() => _isRunning = false;
    }
}