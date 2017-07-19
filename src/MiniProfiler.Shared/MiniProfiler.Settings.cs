using System;
using System.Collections.Generic;
using System.Reflection;
using StackExchange.Profiling.Data;
using StackExchange.Profiling.Helpers;
using StackExchange.Profiling.SqlFormatters;
using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling
{
    public partial class MiniProfiler
    {
        /// <summary>
        /// Various configuration properties.
        /// </summary>
        public static class Settings
        {
            /// <summary>
            /// The path under which ALL routes are registered in, defaults to the application root.  For example, "~/myDirectory/" would yield
            /// "/myDirectory/includes.js" rather than just "/mini-profiler-resources/includes.js"
            /// Any setting here should be in APP RELATIVE FORM, e.g. "~/myDirectory/"
            /// </summary>
            public static string RouteBasePath { get; set; } = "~/mini-profiler-resources";

            /// <summary>
            /// Assembly version of this dank MiniProfiler.
            /// </summary>
            public static Version Version { get; } = typeof(Settings).GetTypeInfo().Assembly.GetName().Version;

            /// <summary>
            /// The hash to use for file cache breaking, this is automatically calculated.
            /// </summary>
            public static string VersionHash { get; set; } = Version.ToString();

            /// <summary>
            /// Assemblies to exclude from the stack trace report.
            /// Add to this using the <see cref="ExcludeAssembly"/> method.
            /// </summary>
            public static HashSet<string> AssembliesToExclude { get; } = new HashSet<string>
                {
                    // our assembly
                    "MiniProfiler",
                    "MiniProfiler.Mvc",
                    "MiniProfiler.Shared",
                    "MiniProfiler.AspNetCore",
                    "MiniProfiler.AspNetCore.Mvc",
                    // reflection emit
                    "Anonymously Hosted DynamicMethods Assembly",
                    // the man
                    "System.Core",
                    "System.Data",
                    "System.Data.Linq",
                    "System.Web",
                    "System.Web.Mvc",
                    "mscorlib",
                };

            /// <summary>
            /// Types to exclude from the stack trace report.
            /// Add to this using the <see cref="ExcludeType"/> method.
            /// </summary>
            public static HashSet<string> TypesToExclude { get; } = new HashSet<string>
                {
                    // while we like our Dapper friend, we don't want to see him all the time
                    "SqlMapper"
                };

            /// <summary>
            /// Methods to exclude from the stack trace report.
            /// Add to this using the <see cref="ExcludeMethod"/> method.
            /// </summary>
            public static HashSet<string> MethodsToExclude { get; } = new HashSet<string>
                {
                    "lambda_method",
                    ".ctor"
                };

            /// <summary>
            /// Excludes the specified assembly from the stack trace output.
            /// </summary>
            /// <param name="assemblyName">The short name of the assembly. AssemblyName.Name</param>
            public static void ExcludeAssembly(string assemblyName) => AssembliesToExclude.Add(assemblyName);

            /// <summary>
            /// Excludes the specified type from the stack trace output.
            /// </summary>
            /// <param name="typeToExclude">The System.Type name to exclude</param>
            public static void ExcludeType(string typeToExclude) => TypesToExclude.Add(typeToExclude);

            /// <summary>
            /// Excludes the specified method name from the stack trace output.
            /// </summary>
            /// <param name="methodName">The name of the method</param>
            public static void ExcludeMethod(string methodName) => MethodsToExclude.Add(methodName);

            /// <summary>
            /// The maximum number of unviewed profiler sessions (set this low cause we don't want to blow up headers)
            /// </summary>
            public static int MaxUnviewedProfiles { get; set; } = 20;

            /// <summary>
            /// The max length of the stack string to report back; defaults to 120 chars.
            /// </summary>
            public static int StackMaxLength { get; set; } = 120;

            /// <summary>
            /// Any Timing step with a duration less than or equal to this will be hidden by default in the UI; defaults to 2.0 ms.
            /// </summary>
            public static decimal TrivialDurationThresholdMilliseconds { get; set; } = 2.0M;

            /// <summary>
            /// Dictates if the "time with children" column is displayed by default, defaults to false.
            /// For a per-page override you can use .RenderIncludes(showTimeWithChildren: true/false)
            /// </summary>
            public static bool PopupShowTimeWithChildren { get; set; } = false;

            /// <summary>
            /// Dictates if trivial timings are displayed by default, defaults to false.
            /// For a per-page override you can use .RenderIncludes(showTrivial: true/false)
            /// </summary>
            public static bool PopupShowTrivial { get; set; } = false;

            /// <summary>
            /// Determines how many traces to show before removing the oldest; defaults to 15.
            /// For a per-page override you can use .RenderIncludes(maxTracesToShow: 10)
            /// </summary>
            public static int PopupMaxTracesToShow { get; set; } = 15;

            /// <summary>
            /// Dictates on which side of the page the profiler popup button is displayed; defaults to left.
            /// For a per-page override you can use .RenderIncludes(position: RenderPosition.Left/Right)
            /// </summary>
            public static RenderPosition PopupRenderPosition { get; set; } = RenderPosition.Left;

            /// <summary>
            /// Allows showing/hiding of popup results buttons via keyboard.
            /// </summary>
            public static string PopupToggleKeyboardShortcut { get; set; } = "Alt+P";

            /// <summary>
            /// When true, results buttons will not initially be shown, requiring keyboard activation via <see cref="PopupToggleKeyboardShortcut"/>.
            /// </summary>
            public static bool PopupStartHidden { get; set; } = false;

            /// <summary>
            /// Determines if min-max, clear, etc are rendered; defaults to false.
            /// For a per-page override you can use .RenderIncludes(showControls: true/false)
            /// </summary>
            public static bool ShowControls { get; set; } = false;

            /// <summary>
            /// Custom timing ExecuteTypes to ignore as duplicates in the UI.
            /// </summary>
            public static HashSet<string> IgnoredDuplicateExecuteTypes { get; } = new HashSet<string>()
            {
                nameof(ProfiledDbConnection.Open),
                nameof(ProfiledDbConnection.OpenAsync),
            };

            /// <summary>
            /// By default, <see cref="CustomTiming"/>s created by this assmebly will grab a stack trace to help 
            /// locate where Remote Procedure Calls are being executed.  When this setting is true, no stack trace 
            /// will be collected, possibly improving profiler performance.
            /// </summary>
            public static bool ExcludeStackTraceSnippetFromCustomTimings { get; set; } = false;

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
            public static IAsyncStorage Storage { get; set; } =
#if NETSTANDARD1_5 || NETSTANDARD2_0
                new NullStorage();
#else
                new MemoryCacheStorage(TimeSpan.FromMinutes(30));
#endif

            /// <summary>
            /// The formatter applied to any SQL before being set in a <see cref="CustomTiming.CommandString"/>.
            /// </summary>
            public static ISqlFormatter SqlFormatter { get; set; }

            /// <summary>
            /// The <see cref="IAsyncProfilerProvider"/> class that is used to run MiniProfiler
            /// </summary>
            /// <remarks>
            /// If not set explicitly, will default to <see cref="DefaultProfilerProvider"/>
            /// </remarks>
            public static IAsyncProfilerProvider ProfilerProvider { get; set; } = new DefaultProfilerProvider();

            /// <summary>
            /// Allows switching out stopwatches for unit testing.
            /// </summary>
            public static Func<IStopwatch> StopwatchProvider { get; set; } = StopwatchWrapper.StartNew;
        }
    }
}
