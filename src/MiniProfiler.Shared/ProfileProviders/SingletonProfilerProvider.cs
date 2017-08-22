using StackExchange.Profiling.Internal;
using System.Threading.Tasks;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Mostly for unit testing and single-threaded apps, only allows one 
    /// instance of a <see cref="MiniProfiler"/> to be the <see cref="MiniProfiler.Current"/> one.
    /// </summary>
    public class SingletonProfilerProvider : IAsyncProfilerProvider
    {
        private static MiniProfiler _profiler;

        /// <summary>
        /// The name says it all
        /// </summary>
        public MiniProfiler CurrentProfiler => _profiler;

        /// <summary>
        /// Starts a new profiling session.
        /// </summary>
        /// <param name="profilerName">The name for the started <see cref="MiniProfiler"/>.</param>
        public MiniProfiler Start(string profilerName, MiniProfilerBaseOptions options) =>
            _profiler = new MiniProfiler(profilerName, options);

        /// <summary>
        /// Stops the current profiling session.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to stop.</param>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/>, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public void Stopped(MiniProfiler profiler, bool discardResults)
        {
            if (discardResults)
            {
                _profiler = null;
            }
        }

        /// <summary>
        /// Asynchronously stops the current profiling session.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to stop.</param>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/>, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public Task StoppedAsync(MiniProfiler profiler, bool discardResults)
        {
            Stopped(profiler, discardResults);
            return Polyfills.CompletedTask;
        }
    }
}
