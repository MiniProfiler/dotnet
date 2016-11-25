using System.Diagnostics;

namespace StackExchange.Profiling.Helpers
{
    /// <summary>
    /// The Stopwatch interface.
    /// </summary>
    public interface IStopwatch
    {
        /// <summary>
        /// Gets the elapsed ticks.
        /// </summary>
        long ElapsedTicks { get; }

        /// <summary>
        /// Gets the frequency.
        /// </summary>
        long Frequency { get; }

        /// <summary>
        /// Gets a value indicating whether is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// stop the timer.
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// The stopwatch wrapper.
    /// </summary>
    internal class StopwatchWrapper : IStopwatch
    {
        /// <summary>
        /// start a new timer.
        /// </summary>
        /// <returns>
        /// The <see cref="IStopwatch"/>.
        /// </returns>
        public static IStopwatch StartNew()
        {
            return new StopwatchWrapper();
        }

        /// <summary>
        /// The _stopwatch.
        /// </summary>
        private readonly Stopwatch _stopwatch;

        /// <summary>
        /// Prevents a default instance of the <see cref="StopwatchWrapper"/> class from being created.
        /// </summary>
        private StopwatchWrapper()
        {
            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Gets the elapsed ticks.
        /// </summary>
        public long ElapsedTicks
        {
            get { return _stopwatch.ElapsedTicks; }
        }

        /// <summary>
        /// Gets the frequency.
        /// </summary>
        public long Frequency
        {
            get { return Stopwatch.Frequency; }
        }

        /// <summary>
        /// Gets a value indicating whether is running.
        /// </summary>
        public bool IsRunning
        {
            get { return _stopwatch.IsRunning; }
        }

        /// <summary>
        /// stop the timer.
        /// </summary>
        public void Stop()
        {
            _stopwatch.Stop();
        }
    }
}
