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
        private static readonly AsyncLocal<MiniProfiler> _profiler = new AsyncLocal<MiniProfiler>();

        private MiniProfiler Profiler
        {
            get => _profiler.Value;
            set => _profiler.Value = value;
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
            Profiler = new MiniProfiler(sessionName ??
#if NET46
            AppDomain.CurrentDomain.FriendlyName
#else       // TODO: Revisit with .NET Standard 2.0
            nameof(MiniProfiler)
#endif
            ) { IsActive = true };
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
