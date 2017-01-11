#if NET45
using System;
using System.Runtime.Remoting.Messaging;
using System.Web;
#else
using System.Threading;
#endif
using System.Threading.Tasks;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Default profile provider, gracefully handles async transitions.
    /// </summary>
    public class DefaultProfilerProvider : BaseProfilerProvider
    {
#if NET45
        private const string ContextKey = ":miniprofiler:";

        private MiniProfiler Profiler
        {
            get
            {
                if (HttpContext.Current != null)
                {
                    return HttpContext.Current?.Items[ContextKey] as MiniProfiler;
                }
                else
                {
                    return CallContext.LogicalGetData(ContextKey) as MiniProfiler;
                }
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Items[ContextKey] = value;
                }
                else
                {
                    CallContext.LogicalSetData(ContextKey, value);
                }
            }
        }
#else
        private AsyncLocal<MiniProfiler> _profiler = new AsyncLocal<MiniProfiler>();

        private MiniProfiler Profiler
        {
            get { return _profiler.Value; }
            set { _profiler.Value = value; }
        }
#endif

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

        private static readonly Task _completed = Task.FromResult(false);
        /// <summary>
        /// Stops the current profiling session.
        /// </summary>
        public override Task StopAsync(bool discardResults)
        {
            Profiler?.StopImpl();
            if (discardResults)
            {
                Profiler = null;
            }
            return _completed;
        }
    }
}
