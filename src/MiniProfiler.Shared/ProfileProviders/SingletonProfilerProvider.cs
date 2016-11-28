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

        /// <summary>
        /// The name says it all
        /// </summary>
        public MiniProfiler GetCurrentProfiler() => _profiler;

        /// <summary>
        /// Starts a new profiling session.
        /// </summary>
        public MiniProfiler Start(string sessionName = null)
        {
#if NET45
            _profiler = new MiniProfiler(sessionName ?? AppDomain.CurrentDomain.FriendlyName) { IsActive = true };
#else // TODO: Revisit with .NET Standard 2.0
            _profiler = new MiniProfiler(sessionName ?? "MiniProfiler") { IsActive = true };
#endif
            return _profiler;
        }

        /// <summary>
        /// Stops the current profiling session.
        /// </summary>
        public void Stop(bool discardResults)
        {
            _profiler?.StopImpl();
            if (discardResults)
            {
                _profiler = null;
            }
        }
    }
}
