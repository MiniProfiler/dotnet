namespace StackExchange.Profiling.Tests
{
    using System;

    /// <summary>
    /// The unit test stopwatch.
    /// </summary>
    public class UnitTestStopwatch : Helpers.IStopwatch
    {
        /// <summary>
        /// The ticks per second.
        /// </summary>
        public static readonly long TicksPerSecond = TimeSpan.FromSeconds(1).Ticks;

        /// <summary>
        /// The ticks per millisecond.
        /// </summary>
        public static readonly long TicksPerMillisecond = TimeSpan.FromMilliseconds(1).Ticks;

        /// <summary>
        /// _is running.
        /// </summary>
        private bool _isRunning = true;

        /// <summary>
        /// Gets or sets the elapsed ticks.
        /// </summary>
        public long ElapsedTicks { get; set; }

        /// <summary>
        /// Gets the frequency, <see cref="MiniProfiler.GetRoundedMilliseconds"/> method will use this to determine how many ticks actually elapsed, so make it simple.
        /// </summary>
        public long Frequency
        {
            get { return TicksPerSecond; }
        }

        /// <summary>
        /// Gets a value indicating whether is running.
        /// </summary>
        public bool IsRunning
        {
            get { return _isRunning; }
        }

        /// <summary>
        /// stop the profiler.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
        }
    }
}