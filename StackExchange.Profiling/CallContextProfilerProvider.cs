using System;
using System.Runtime.Remoting.Messaging;
using System.Web;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Threadsafe CallContext based singleton profiler
    /// instance of a <see cref="MiniProfiler"/> to be the <see cref="MiniProfiler.Current"/> one.
    /// </summary>
    public class CallContextProfilerProvider : IProfilerProvider
    {
        private const string CALLCONTEXT_PARAM_CURRENT_PROFILER = "miniprofiler_current";

        public MiniProfiler GetCurrentProfiler()
        {
            if (HttpContext.Current != null)
                return HttpContext.Current.Items[CALLCONTEXT_PARAM_CURRENT_PROFILER] as MiniProfiler ?? GetCurrentProfilerFromCallContext();

            return GetCurrentProfilerFromCallContext();
        }

        /// <summary>
        /// Starts a new profiling session.
        /// </summary>
        public MiniProfiler Start(ProfileLevel level, string sessionName = null)
        {
            var profiler = new MiniProfiler(sessionName ?? AppDomain.CurrentDomain.FriendlyName) { IsActive = true };
            if (HttpContext.Current != null)
                HttpContext.Current.Items[CALLCONTEXT_PARAM_CURRENT_PROFILER] = profiler;
            SetCurrentProfilerToCallContext(profiler);
            return profiler;
        }

        /// <summary>
        /// Stops the current profiling session.
        /// </summary>
        public void Stop(bool discardResults)
        {
            var profiler = GetCurrentProfilerFromCallContext();
            if (!discardResults && profiler != null) profiler.StopImpl();
            SetCurrentProfilerToCallContext(null);
        }

        #region Private Methods

        private static void SetCurrentProfilerToCallContext(MiniProfiler profiler)
        {
            CallContext.LogicalSetData(CALLCONTEXT_PARAM_CURRENT_PROFILER, profiler);
        }

        private static MiniProfiler GetCurrentProfilerFromCallContext()
        {
            return CallContext.LogicalGetData(CALLCONTEXT_PARAM_CURRENT_PROFILER) as MiniProfiler;
        }

        #endregion
    }
}
