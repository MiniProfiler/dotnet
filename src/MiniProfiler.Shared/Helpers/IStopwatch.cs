using System.Diagnostics;

namespace StackExchange.Profiling.Helpers
{
    /// <summary>
    /// The Stopwatch interface.
    /// </summary>
    public interface IStopwatch
    {
        /// <summary>
        /// Gets the total elapsed time measured by the current instance, in timer ticks.
        /// </summary>
        long ElapsedTicks { get; }

        /// <summary>
        /// Gets the frequency of the timer as the number of ticks per second. This field is read-only.
        /// </summary>
        long Frequency { get; }

        /// <summary>
        /// Gets a value indicating whether the Stopwatch timer is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Stops measuring elapsed time for an interval.
        /// </summary>
        void Stop();
    }
}
