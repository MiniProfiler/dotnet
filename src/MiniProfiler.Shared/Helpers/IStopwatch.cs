using System.Diagnostics;

// Expose internal types to tests
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MiniProfiler.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100692acb3f29aa84e21acced216dd3a85ea1ce61fec9c53d36c6cc8c6bdad3292316aedc89feb69c6de1a0dfa59cf0b24ab8402e3abe5a36551cb25c1d9663a35d829fbdb8539bda405d6b2feb73b44b322655228e9e48c37f36663a0e2fac40d1808ff28a13fcdb621ea03dbcad8d016d1fdd5fd91a1377b9814ae24039aff2d5")]

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

    /// <summary>
    /// The stopwatch wrapper.
    /// </summary>
    internal class StopwatchWrapper : IStopwatch
    {
        private readonly Stopwatch _stopwatch;

        /// <summary>
        /// Initializes a new Stopwatch instance, sets the elapsed time property to zero, and starts measuring elapsed time.
        /// </summary>
        /// <returns>The <see cref="IStopwatch"/>.</returns>
        public static IStopwatch StartNew() => new StopwatchWrapper();

        /// <summary>
        /// Prevents a default instance of the <see cref="StopwatchWrapper"/> class from being created.
        /// </summary>
        private StopwatchWrapper()
        {
            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Gets the total elapsed time measured by the current instance, in timer ticks.
        /// </summary>
        public long ElapsedTicks => _stopwatch.ElapsedTicks;

        /// <summary>
        /// Gets the frequency of the timer as the number of ticks per second. This field is read-only.
        /// </summary>
        public long Frequency => Stopwatch.Frequency;

        /// <summary>
        /// Gets a value indicating whether the Stopwatch timer is running.
        /// </summary>
        public bool IsRunning => _stopwatch.IsRunning;

        /// <summary>
        /// Stops measuring elapsed time for an interval.
        /// </summary>
        public void Stop() => _stopwatch.Stop();
    }
}
