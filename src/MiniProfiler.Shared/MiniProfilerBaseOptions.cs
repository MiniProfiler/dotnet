using System;
using System.Collections.Generic;
using System.Reflection;
using StackExchange.Profiling.Data;
using StackExchange.Profiling.Helpers;
using StackExchange.Profiling.SqlFormatters;
using StackExchange.Profiling.Storage;
using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Various configuration properties for MiniProfiler.
    /// </summary>
    public class MiniProfilerBaseOptions
    {
        /// <summary>
        /// The path under which ALL routes are registered in, defaults to the application root.  For example, "~/myDirectory/" would yield
        /// "/myDirectory/includes.js" rather than just "/mini-profiler-resources/includes.js"
        /// Any setting here should be in APP RELATIVE FORM, e.g. "~/myDirectory/"
        /// </summary>
        public string RouteBasePath { get; set; } = "~/mini-profiler-resources";

        /// <summary>
        /// Assembly version of this dank MiniProfiler.
        /// </summary>
        public static Version Version { get; } = typeof(MiniProfilerBaseOptions).GetTypeInfo().Assembly.GetName().Version;

        /// <summary>
        /// The hash to use for file cache breaking, this is automatically calculated.
        /// </summary>
        public virtual string VersionHash { get; set; } = Version.ToString();

        /// <summary>
        /// Assemblies to exclude from the stack trace report.
        /// </summary>
        public HashSet<string> ExcludedAssemblies { get; } = new HashSet<string>
        {
            // our assembly
            "MiniProfiler",
            "MiniProfiler.AspNetCore",
            "MiniProfiler.AspNetCore.Mvc",
            "MiniProfiler.EF6",
            "MiniProfiler.EntityFrameworkCore",
            "MiniProfiler.Mvc5",
            "MiniProfiler.Shared",
            // Util friends
            "Dapper",
            // reflection emit
            "Anonymously Hosted DynamicMethods Assembly",
            // the man
            "EntityFramework",
            "EntityFramework.SqlServer",
            "EntityFramework.SqlServerCompact",
            "System.Core",
            "System.Data",
            "System.Data.Linq",
            "System.Web",
            "System.Web.Mvc",
            "mscorlib",
        };

        /// <summary>
        /// Types to exclude from the stack trace report.
        /// </summary>
        public HashSet<string> ExcludedTypes { get; } = new HashSet<string>();

        /// <summary>
        /// Methods to exclude from the stack trace report.
        /// </summary>
        public HashSet<string> ExcludedMethods { get; } = new HashSet<string>
        {
            "lambda_method",
            ".ctor"
        };

        /// <summary>
        /// When <see cref="MiniProfiler.Start(string)"/> is called, if the current request url contains any items in this property,
        /// no profiler will be instantiated and no results will be displayed.
        /// Default value is { "/content/", "/scripts/", "/favicon.ico" }.
        /// </summary>
        public HashSet<string> IgnoredPaths { get; } = new HashSet<string> {
            "/content/",
            "/scripts/",
            "/favicon.ico"
        };

        /// <summary>
        /// The maximum number of unviewed profiler sessions (set this low cause we don't want to blow up headers)
        /// </summary>
        public int MaxUnviewedProfiles { get; set; } = 20;

        /// <summary>
        /// The max length of the stack string to report back; defaults to 120 chars.
        /// </summary>
        public int StackMaxLength { get; set; } = 120;

        /// <summary>
        /// Any Timing step with a duration less than or equal to this will be hidden by default in the UI; defaults to 2.0 ms.
        /// </summary>
        public decimal TrivialDurationThresholdMilliseconds { get; set; } = 2.0M;

        /// <summary>
        /// Dictates if the "time with children" column is displayed by default, defaults to false.
        /// For a per-page override you can use .RenderIncludes(showTimeWithChildren: true/false)
        /// </summary>
        public bool PopupShowTimeWithChildren { get; set; } = false;

        /// <summary>
        /// Dictates if trivial timings are displayed by default, defaults to false.
        /// For a per-page override you can use .RenderIncludes(showTrivial: true/false)
        /// </summary>
        public bool PopupShowTrivial { get; set; } = false;

        /// <summary>
        /// Determines how many traces to show before removing the oldest; defaults to 15.
        /// For a per-page override you can use .RenderIncludes(maxTracesToShow: 10)
        /// </summary>
        public int PopupMaxTracesToShow { get; set; } = 15;

        /// <summary>
        /// Dictates on which side of the page the profiler popup button is displayed; defaults to left.
        /// For a per-page override you can use .RenderIncludes(position: RenderPosition.Left/Right)
        /// </summary>
        public RenderPosition PopupRenderPosition { get; set; } = RenderPosition.Left;

        /// <summary>
        /// Allows showing/hiding of popup results buttons via keyboard.
        /// </summary>
        public string PopupToggleKeyboardShortcut { get; set; } = "Alt+P";

        /// <summary>
        /// When true, results buttons will not initially be shown, requiring keyboard activation via <see cref="PopupToggleKeyboardShortcut"/>.
        /// </summary>
        public bool PopupStartHidden { get; set; } = false;

        /// <summary>
        /// Determines if min-max, clear, etc are rendered; defaults to false.
        /// For a per-page override you can use .RenderIncludes(showControls: true/false)
        /// </summary>
        public bool ShowControls { get; set; } = false;

        /// <summary>
        /// Custom timing ExecuteTypes to ignore as duplicates in the UI.
        /// </summary>
        public HashSet<string> IgnoredDuplicateExecuteTypes { get; } = new HashSet<string>()
        {
            nameof(ProfiledDbConnection.Open),
            nameof(ProfiledDbConnection.OpenAsync),
        };

        /// <summary>
        /// By default, <see cref="CustomTiming"/>s created by this assmebly will grab a stack trace to help 
        /// locate where Remote Procedure Calls are being executed.  When this setting is true, no stack trace 
        /// will be collected, possibly improving profiler performance.
        /// </summary>
        public bool ExcludeStackTraceSnippetFromCustomTimings { get; set; } = false;

        /// <summary>
        /// Understands how to save and load MiniProfilers. Used for caching between when
        /// a profiling session ends and results can be fetched to the client, and for showing shared, full-page results.
        /// On netstandard, defaults to <see cref="NullStorage"/>.
        /// On netfx, defaults to <see cref="MemoryCacheStorage"/> with a duration of 30 minutes.
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
        public IAsyncStorage Storage { get; set; } =
#if NETSTANDARD1_5 || NETSTANDARD2_0
            new NullStorage();
#else
            new MemoryCacheStorage(TimeSpan.FromMinutes(30));
#endif

        /// <summary>
        /// The formatter applied to any SQL before being set in a <see cref="CustomTiming.CommandString"/>.
        /// </summary>
        public ISqlFormatter SqlFormatter { get; set; } = new InlineFormatter();

        /// <summary>
        /// The <see cref="IAsyncProfilerProvider"/> class that is used to run MiniProfiler
        /// </summary>
        /// <remarks>
        /// If not set explicitly, will default to <see cref="DefaultProfilerProvider"/>
        /// </remarks>
        public IAsyncProfilerProvider ProfilerProvider { get; internal set; } = new DefaultProfilerProvider();

        /// <summary>
        /// The profiler provider to use when resolving <see cref="MiniProfiler.Current"/> staticaly.
        /// Set via <see cref="MiniProfilerBaseOptionsExtensions.SetProvider{T}(T, IAsyncProfilerProvider, bool)"/>.
        /// </summary>
        public static IAsyncProfilerProvider CurrentProfilerProvider { get; internal set; } = new DefaultProfilerProvider();

        /// <summary>
        /// Allows switching out stopwatches for unit testing.
        /// </summary>
        public Func<IStopwatch> StopwatchProvider { get; set; } = StopwatchWrapper.StartNew;

        /// <summary>
        /// Starts a new MiniProfiler based on the curren <see cref="ProfilerProvider"/>.
        /// Shotcut for Options.StartProfiler.
        /// </summary>
        /// <param name="profilerName">
        /// Allows explicit naming of the new profiling session; when null, an appropriate default will be used, e.g. for
        /// a web request, the url will be used for the overall session name.
        /// </param>
        public MiniProfiler StartProfiler(string profilerName = null) => ProfilerProvider.Start(profilerName, this);
    }

    public static class MiniProfilerBaseOptionsExtensions
    {
        /// <summary>
        /// Sets a provider as the <see cref="ProfilerProvider"/> and optionally as the provider for <see cref="MiniProfiler.Current"/>.
        /// Extension method to support chaining while retaining the derivative options type.
        /// </summary>
        /// <typeparam name="T">The subtype of <see cref="MiniProfilerBaseOptions"/> to use (inferred for common usage).</typeparam>
        /// <param name="options">The options to set the provider on.</param>
        /// <param name="provider">The provider to use.</param>
        /// <param name="setAsCurrentProvider">Whether it should be set as the provider for getting <see cref="MiniProfiler.Current"/>.</param>
        public static T SetProvider<T>(this T options, IAsyncProfilerProvider provider, bool setAsCurrentProvider = true) where T : MiniProfilerBaseOptions
        {
            options.ProfilerProvider = provider ?? throw new ArgumentException(nameof(provider));
            if (setAsCurrentProvider )
            {
                MiniProfilerBaseOptions.CurrentProfilerProvider = provider;
            }
            return options;
        }

        /// <summary>
        /// Excludes an assembly from stack traces, convenience method for chaining, basiscally <see cref="MiniProfilerBaseOptions.ExcludedAssemblies"/>.Add(assembly)
        /// </summary>
        /// <typeparam name="T">The subtype of <see cref="MiniProfilerBaseOptions"/> to use (inferred for common usage).</typeparam>
        /// <param name="options">The options to exclude the assembly on.</param>
        /// <param name="assembly">The assembly name to exclude from stack traces.</param>
        public static T ExcludeAssembly<T>(this T options, string assembly) where T : MiniProfilerBaseOptions
        {
            options.ExcludedAssemblies.Add(assembly);
            return options;
        }

        /// <summary>
        /// Excludes a method from stack traces, convenience method for chaining, basiscally <see cref="MiniProfilerBaseOptions.ExcludedMethods"/>.Add(assembly)
        /// </summary>
        /// <typeparam name="T">The subtype of <see cref="MiniProfilerBaseOptions"/> to use (inferred for common usage).</typeparam>
        /// <param name="options">The options to exclude the method on.</param>
        /// <param name="method">The method name to exclude from stack traces.</param>
        public static T ExcludeMethod<T>(this T options, string method) where T : MiniProfilerBaseOptions
        {
            options.ExcludedMethods.Add(method);
            return options;
        }

        /// <summary>
        /// Excludes a type from stack traces, convenience method for chaining, basiscally <see cref="MiniProfilerBaseOptions.ExcludedTypes"/>.Add(assembly)
        /// </summary>
        /// <typeparam name="T">The subtype of <see cref="MiniProfilerBaseOptions"/> to use (inferred for common usage).</typeparam>
        /// <param name="options">The options to exclude the type on.</param>
        /// <param name="type">The type name to exclude from stack traces.</param>
        public static T ExcludeType<T>(this T options, string type) where T : MiniProfilerBaseOptions
        {
            options.ExcludedTypes.Add(type);
            return options;
        }
    }
}
