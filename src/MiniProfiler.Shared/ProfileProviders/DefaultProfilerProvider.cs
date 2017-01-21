using System;
using System.Threading;
using System.Threading.Tasks;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Default profile provider, gracefully handles async transitions.
    /// </summary>
    public class DefaultProfilerProvider : BaseProfilerProvider
    {
        private AsyncLocal<MiniProfiler> _profiler = new AsyncLocal<MiniProfiler>();

        private MiniProfiler Profiler
        {
            get { return _profiler.Value; }
            set { _profiler.Value = value; }
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
#if NET46
            Profiler = new MiniProfiler(sessionName ?? AppDomain.CurrentDomain.FriendlyName) { IsActive = true };
#else       // TODO: Revisit with .NET Standard 2.0
            Profiler = new MiniProfiler(sessionName ?? nameof(MiniProfiler)) { IsActive = true };
#endif
            SetProfilerActive(Profiler);

            return Profiler;
        }

        /// <summary>
        /// Stops the current profiling session.
        /// </summary>
        public override void Stop(bool discardResults)
        {
            var profiler = Profiler;
            if (profiler == null) return;

            StopProfiler(profiler);
            if (discardResults)
            {
                Profiler = null;
            }
        }
        
        /// <summary>
        /// Stops the current profiling session.
        /// </summary>
        public override Task StopAsync(bool discardResults)
        {
            var profiler = Profiler;
            if (profiler != null)
            {
                StopProfiler(profiler);
                SaveProfiler(profiler);
                if (discardResults)
                {
                    Profiler = null;
                }
            }
            return Task.CompletedTask;
        }
    }
}
