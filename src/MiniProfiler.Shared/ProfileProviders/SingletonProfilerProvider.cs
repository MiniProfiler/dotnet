using System;
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
        public MiniProfiler GetCurrentProfiler() => _profiler;

        /// <summary>
        /// Starts a new profiling session.
        /// </summary>
        /// <param name="profilerName">The name for the started <see cref="MiniProfiler"/>.</param>
        public MiniProfiler Start(string profilerName = null)
        {
#if NET46
            _profiler = new MiniProfiler(profilerName ?? AppDomain.CurrentDomain.FriendlyName) { IsActive = true };
#else // TODO: Revisit with .NET Standard 2.0
            _profiler = new MiniProfiler(profilerName ?? "MiniProfiler") { IsActive = true };
#endif
            return _profiler;
        }

        /// <summary>
        /// Stops the current profiling session.
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/>, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public void Stop(bool discardResults)
        {
            _profiler?.StopImpl();
            if (discardResults)
            {
                _profiler = null;
            }
        }

        /// <summary>
        /// Asynchronously stops the current profiling session.
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/>, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public Task StopAsync(bool discardResults)
        {
            Stop(discardResults);
            return Task.CompletedTask;
        }
    }
}
