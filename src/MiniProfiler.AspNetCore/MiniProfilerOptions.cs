using StackExchange.Profiling.SqlFormatters;
using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Options for configuring MiniProfiler
    /// </summary>
    public class MiniProfilerOptions
    {
        /// <summary>
        /// Understands how to save and load MiniProfilers. Used for caching between when
        /// a profiling session ends and results can be fetched to the client, and for showing shared, full-page results.
        /// </summary>
        /// <remarks>
        /// The normal profiling session life-cycle is as follows:
        /// 1) request begins
        /// 2) profiler is started
        /// 3) normal page/controller/request execution
        /// 4) profiler is stopped
        /// 5) profiler is cached with <see cref="Storage"/>'s implementation of <see cref="IAsyncStorage.Save"/>
        /// 6) request ends
        /// 7) page is displayed and profiling results are ajax-fetched down, pulling cached results from 
        ///    <see cref="Storage"/>'s implementation of <see cref="IAsyncStorage.Load"/>
        /// </remarks>
        public IAsyncStorage Storage { get; set; }

        /// <summary>
        /// The formatter applied to any SQL before being set in a <see cref="CustomTiming.CommandString"/>.
        /// </summary>
        public ISqlFormatter SqlFormatter { get; set; }

        /// <summary>
        /// The <see cref="IAsyncProfilerProvider"/> class that is used to run MiniProfiler
        /// </summary>
        /// <remarks>
        /// If not set explicitly, will default to <see cref="DefaultProfilerProvider"/>
        /// </remarks>
        public IAsyncProfilerProvider ProfilerProvider { get; set; }

        /// <summary>
        /// Assembly version of this dank MiniProfiler.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// The base path for all MiniProfiler Middleware requests, defaults to /mini-profiler-resources
        /// </summary>
        /// <remarks>
        /// Do NOT end this in a slash, due to how .StartWithSegments behaves
        /// </remarks>
        public string BasePath { get; set; } = "/mini-profiler-resources";

        /// <summary>
        /// Gets or sets the content directory subfolder to load custom template overrides from.
        /// For example, if you're using wwwroot and want to use wwwroot/profiler/, set this to "profiler/"
        /// </summary>
        public string UITemplatesPath { get; set; }
    }
}
