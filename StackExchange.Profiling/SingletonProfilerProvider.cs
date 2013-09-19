using System;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Mostly for unit testing and single-threaded apps, only allows one 
    /// instance of a <see cref="MiniProfiler"/> to be the <see cref="MiniProfiler.Current"/> one.
    /// </summary>
    public class SingletonProfilerProvider : IProfilerProvider
    {
        /// <summary>
        /// The _profiler.
        /// </summary>
        private MiniProfiler _profiler;

        /// <summary>
        /// The get current profiler.
        /// </summary>
        /// <returns>
        /// The <see cref="MiniProfiler"/>.
        /// </returns>
        public MiniProfiler GetCurrentProfiler()
        {
            return _profiler;
        }

        /// <summary>
        /// The start.
        /// </summary>
        /// <param name="level">
        /// The level.
        /// </param>
        /// <returns>
        /// The <see cref="MiniProfiler"/>.
        /// </returns>
        public MiniProfiler Start(ProfileLevel level)
        {
            _profiler = new MiniProfiler(AppDomain.CurrentDomain.FriendlyName) { IsActive = true };
            return _profiler;
        }

        /// <summary>
        /// The stop.
        /// </summary>
        /// <param name="discardResults">
        /// The discard results.
        /// </param>
        public void Stop(bool discardResults)
        {
            // TODO: refactor IProfilerProvider, as it has knowledge of discarding results, but no other storage knowledge
            var mp = GetCurrentProfiler();
            if (mp != null) mp.StopImpl();
        }
    }
}
