using System;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Mostly for unit testing and single-threaded apps, only allows one 
    /// instance of a <see cref="MiniProfiler"/> to be the <see cref="MiniProfiler.Current"/> one.
    /// </summary>
    public class SingletonProfilerProvider : IProfilerProvider
    {
        private MiniProfiler _profiler;

        public MiniProfiler GetCurrentProfiler()
        {
            return _profiler;
        }

        /// <summary>
        /// Starts a new profiling session.
        /// </summary>
        public MiniProfiler Start(ProfileLevel level, string sessionName = null)
        {
            _profiler = new MiniProfiler(sessionName ?? AppDomain.CurrentDomain.FriendlyName) { IsActive = true };
            return _profiler;
        }

        /// <summary>
        /// Stops the current profiling session.
        /// </summary>
        public void Stop(bool discardResults)
        {
            if (_profiler != null) _profiler.StopImpl();
        }
    }
}
