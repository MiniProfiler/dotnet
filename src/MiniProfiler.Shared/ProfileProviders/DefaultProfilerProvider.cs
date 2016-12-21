#if NET45
using System;
using System.Runtime.Remoting.Messaging;
using System.Web; 
using StackExchange.Profiling.Helpers.Net45;
#else
using System.Threading;
#endif

namespace StackExchange.Profiling
{
    /// <summary>
    /// Default profile provider, gracefully handles async transitions.
    /// </summary>
    public class DefaultProfilerProvider : BaseProfilerProvider
    {
        private readonly AsyncLocal<MiniProfiler> _profiler = new AsyncLocal<MiniProfiler>();
        private readonly AsyncLocal<Timing> _currentTiming = new AsyncLocal<Timing>();

        private MiniProfiler Profiler
        {
            get { return _profiler.Value; }
            set { _profiler.Value = value; }
        }

        /// <summary>
        /// Current head timing.
        /// </summary>
        public override Timing CurrentHead
        {
            get { return _currentTiming.Value; }
            set { _currentTiming.Value = value; }
        }

        /// <summary>
        /// The name says it all.
        /// </summary>
        public override MiniProfiler GetCurrentProfiler() => Profiler;

        /// <summary>
        /// Starts a new profiling session.
        /// </summary>
        public override MiniProfiler Start(string sessionName = null)
        {
#if NET45
            Profiler = new MiniProfiler(sessionName ?? AppDomain.CurrentDomain.FriendlyName) { IsActive = true };
#else       // TODO: Revisit with .NET Standard 2.0
            Profiler = new MiniProfiler(sessionName ?? "MiniProfiler") { IsActive = true };
#endif
            SetProfilerActive(Profiler);

            return Profiler;
        }

        /// <summary>
        /// Stops the current profiling session.
        /// </summary>
        public override void Stop(bool discardResults)
        {
            Profiler?.StopImpl();
            if (discardResults)
            {
                Profiler = null;
            }
        }
    }
}
