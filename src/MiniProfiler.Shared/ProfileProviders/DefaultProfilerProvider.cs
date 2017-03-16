using System.Threading;
using System.Threading.Tasks;
#if NET46
using System;
#endif

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
        /// <param name="profilerName">The name for the started <see cref="MiniProfiler"/>.</param>
        public override MiniProfiler Start(string profilerName = null)
        {
            Profiler = new MiniProfiler(profilerName ??
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
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/>, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public override void Stop(bool discardResults)
        {
            var profiler = Profiler;
            if (profiler == null) return;

            StopProfiler(profiler);
            if (discardResults)
            {
                Profiler = null;
            }
            else
            {
                SaveProfiler(profiler);
            }
        }

        /// <summary>
        /// Stops the current profiling session.
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/>, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public override async Task StopAsync(bool discardResults)
        {
            var profiler = Profiler;
            if (profiler != null)
            {
                StopProfiler(profiler);
                if (discardResults)
                {
                    Profiler = null;
                }
                else
                {
                    await SaveProfilerAsync(profiler).ConfigureAwait(false);
                }
            }
        }
    }
}
