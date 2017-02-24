using Microsoft.AspNetCore.Http;
using StackExchange.Profiling.SqlFormatters;
using StackExchange.Profiling.Storage;
using System;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Options for configuring MiniProfiler
    /// </summary>
    public class MiniProfilerOptions
    {
        /// <summary>
        /// Seta a function to control whether a given request should be profiled at all.
        /// </summary>
        public Func<HttpRequest, bool> ShouldProfile { get; set; } = r => true;

        /// <summary>
        /// A function that determines who can access the MiniProfiler results url and list url.  It should return true when
        /// the request client has access to results, false for a 401 to be returned. HttpRequest parameter is the current request and
        /// </summary>
        /// <remarks>
        /// The HttpRequest parameter that will be passed into this function should never be null.
        /// </remarks>
        public Func<HttpRequest, bool> ResultsAuthorize { get; set; }

        /// <summary>
        /// Special authorization function that is called for the list results (listing all the profiling sessions), 
        /// we also test for results authorize always. This must be set and return true, to enable the listing feature.
        /// </summary>
        public Func<HttpRequest, bool> ResultsListAuthorize { get; set; }

        /// <summary>
        /// Function to provide the unique user ID based on the request, to store MiniProfiler IDs user
        /// </summary>
        public Func<HttpRequest, string> UserIdProvider { get; set; }

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
        public IAsyncStorage Storage
        {
            get => MiniProfiler.Settings.Storage;
            set => MiniProfiler.Settings.Storage = value;
        }

        /// <summary>
        /// The formatter applied to any SQL before being set in a <see cref="CustomTiming.CommandString"/>.
        /// </summary>
        public ISqlFormatter SqlFormatter
        {
            get => MiniProfiler.Settings.SqlFormatter;
            set => MiniProfiler.Settings.SqlFormatter = value;
        }

        /// <summary>
        /// The <see cref="IAsyncProfilerProvider"/> class that is used to run MiniProfiler
        /// </summary>
        /// <remarks>
        /// If not set explicitly, will default to <see cref="DefaultProfilerProvider"/>
        /// </remarks>
        public IAsyncProfilerProvider ProfilerProvider
        {
            get => MiniProfiler.Settings.ProfilerProvider;
            set => MiniProfiler.Settings.ProfilerProvider = value;
        }

        /// <summary>
        /// Assembly version of this dank MiniProfiler.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// The base path for all MiniProfiler Middleware requests, defaults to ~/mini-profiler-resources
        /// </summary>
        public string RouteBasePath
        {
            get => MiniProfiler.Settings.RouteBasePath;
            set => MiniProfiler.Settings.RouteBasePath = value;
        }

        /// <summary>
        /// Gets or sets the content directory subfolder to load custom template overrides from.
        /// For example, if you're using wwwroot and want to use wwwroot/profiler/, set this to "profiler/"
        /// </summary>
        public string UITemplatesPath { get; set; }
    }
}
