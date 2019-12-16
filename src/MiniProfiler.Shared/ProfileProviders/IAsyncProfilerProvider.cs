using System.Threading.Tasks;
using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling
{
    /// <summary>
    /// A provider used to create <see cref="MiniProfiler"/> instances and maintain the current instance.
    /// Options are passed into the <see cref="Start(string, MiniProfilerBaseOptions)"/> method (they can be a more specific type).
    /// For later events, <see cref="MiniProfiler.Options"/> can be accessed from the <see cref="MiniProfiler"/> parameter.
    /// </summary>
    public interface IAsyncProfilerProvider
    {
        /// <summary>
        /// Returns the current MiniProfiler. This is used by <see cref="MiniProfiler.Current"/>.
        /// </summary>
        MiniProfiler CurrentProfiler { get; }

        /// <summary>
        /// Starts a new MiniProfiler and sets it to be current.  By the end of this method
        /// <see cref="CurrentProfiler"/> should return the new MiniProfiler. 
        /// Unless one was not created due to ignore rules, etc.
        /// </summary>
        /// <param name="profilerName">
        /// Allows explicit naming of the new profiling session; when null, an appropriate default will be used, e.g. for
        /// a web request, the URL will be used for the overall session name.
        /// </param>
        /// <param name="options">The options to start the MiniProfiler with. Likely a more-specific type underneath.</param>
        MiniProfiler Start(string profilerName, MiniProfilerBaseOptions options);

        /// <summary>
        /// Ends the current profiling session, if one exists.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to stop.</param>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/>, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        void Stopped(MiniProfiler profiler, bool discardResults);

        /// <summary>
        /// Asynchronously ends the current profiling session, if one exists.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to stop.</param>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/>, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        Task StoppedAsync(MiniProfiler profiler, bool discardResults);
    }
}
