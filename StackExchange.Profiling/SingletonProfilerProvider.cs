using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public MiniProfiler Start(ProfileLevel level)
        {
            _profiler = new MiniProfiler(AppDomain.CurrentDomain.FriendlyName) { IsActive = true };
            return _profiler;
        }

        public void Stop(bool discardResults)
        {
            // TODO: refactor IProfilerProvider, as it has knowledge of discarding results, but no other storage knowledge
            var mp = GetCurrentProfiler();
            if (mp != null) mp.StopImpl();
        }
    }
}
