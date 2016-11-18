using System;

namespace StackExchange.Profiling
{
    /// <summary>
    /// A provider used to create <see cref="MiniProfiler"/> instances and maintain the current instance.
    /// </summary>
    public interface IProfilerProvider
    {
        /// <summary>
        /// Starts a new MiniProfiler and sets it to be current.  By the end of this method
        /// <see cref="GetCurrentProfiler"/> should return the new MiniProfiler.
        /// </summary>
        /// <param name="sessionName">
        /// Allows explicit naming of the new profiling session; when null, an appropriate default will be used, e.g. for
        /// a web request, the url will be used for the overall session name.
        /// </param>
        MiniProfiler Start(string sessionName = null);

        /// <summary>
        /// Ends the current profiling session, if one exists.
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/> for this HttpContext, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        void Stop(bool discardResults);

        /// <summary>
        /// Returns the current MiniProfiler.  This is used by <see cref="MiniProfiler.Current"/>.
        /// </summary>
        MiniProfiler GetCurrentProfiler();
    }
}
