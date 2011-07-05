using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Reflection;
using System.Web;

namespace MvcMiniProfiler
{
    partial class MiniProfiler
    {
        /// <summary>
        /// Various configuration properties.
        /// </summary>
        public static class Settings
        {
        	private static readonly HashSet<string> assembliesToExclude;
        	private static readonly HashSet<string> typesToExclude;
        	private static readonly HashSet<string> methodsToExclude;

            static Settings()
            {
                var props = from p in typeof(Settings).GetProperties(BindingFlags.Public | BindingFlags.Static)
                            let t = typeof(DefaultValueAttribute)
                            where p.IsDefined(t, inherit: false)
                            let a = p.GetCustomAttributes(t, inherit: false).Single() as DefaultValueAttribute
                            select new { PropertyInfo = p, DefaultValue = a };

                foreach (var pair in props)
                {
                    pair.PropertyInfo.SetValue(null, Convert.ChangeType(pair.DefaultValue.Value, pair.PropertyInfo.PropertyType), null);
                }

                Version = System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(Settings).Assembly.Location).ProductVersion;

                typesToExclude = new HashSet<string>
                {
                    // while we like our Dapper friend, we don't want to see him all the time
                    "SqlMapper"
                };

                methodsToExclude = new HashSet<string>
                {
                    "lambda_method",
                    ".ctor"
                };

                assembliesToExclude = new HashSet<string>
                {
                    // our assembly
                    "MvcMiniProfiler",

                    // reflection emit
                    "Anonymously Hosted DynamicMethods Assembly",

                    // the man
                    "System.Core",
                    "System.Data",
                    "System.Data.Linq",
                    "System.Web",
                    "System.Web.Mvc",
                };
            }

        	/// <summary>
            /// Assemblies to exclude from the stack trace report.
            /// </summary>
            public static IEnumerable<string> AssembliesToExclude
            {
                get { return assembliesToExclude; }
            }

        	/// <summary>
        	/// Types to exclude from the stack trace report.
        	/// </summary>
        	public static IEnumerable<string> TypesToExclude
        	{
        		get { return typesToExclude; }
        	}

        	/// <summary>
            /// Methods to exclude from the stack trace report.
            /// </summary>
            public static IEnumerable<string> MethodsToExclude
            {
                get { return methodsToExclude; }
            }

            /// <summary>
            /// Excludes the specified assembly from the stack trace output.
            /// </summary>
            /// <param name="assemblyName">The short name of the assembly. AssemblyName.Name</param>
            public static void ExcludeAssembly(string assemblyName)
            {
                assembliesToExclude.Add(assemblyName);
            }

        	/// <summary>
        	/// Excludes the specified type from the stack trace output.
        	/// </summary>
        	/// <param name="typeToExclude">The System.Type name to exclude</param>
        	public static void ExcludeType(string typeToExclude)
        	{
        		typesToExclude.Add(typeToExclude);
        	}

        	/// <summary>
            /// Excludes the specified method name from the stack trace output.
            /// </summary>
            /// <param name="methodName">The name of the method</param>
            public static void ExcludeMethod(string methodName)
            {
                methodsToExclude.Add(methodName);
            }

        	/// <summary>
            /// The max length of the stack string to report back; defaults to 120 chars.
            /// </summary>
            [DefaultValue(120)]
            public static int StackMaxLength { get; set; }

            /// <summary>
            /// Any Timing step with a duration less than or equal to this will be hidden by default in the UI; defaults to 2.0 ms.
            /// </summary>
            [DefaultValue(2.0)]
            public static decimal TrivialDurationThresholdMilliseconds { get; set; }

            /// <summary>
            /// Dictates if the "time with children" column is displayed by default, defaults to false.
            /// For a per-page override you can use .RenderIncludes(showTimeWithChildren: true/false)
            /// </summary>
            [DefaultValue(false)]
            public static bool PopupShowTimeWithChildren { get; set; }

            /// <summary>
            /// Dictates if trivial timings are displayed by default, defaults to false.
            /// For a per-page override you can use .RenderIncludes(showTrivial: true/false)
            /// </summary>
            [DefaultValue(false)]
            public static bool PopupShowTrivial { get; set; }

            /// <summary>
            /// Determines how many traces to show before removing the oldest; defaults to 15.
            /// For a per-page override you can use .RenderIncludes(maxTracesToShow: 10)
            /// </summary>
            [DefaultValue(15)]
            public static int PopupMaxTracesToShow { get; set; }

            /// <summary>
            /// Dictates on which side of the page the profiler popup button is displayed; defaults to left.
            /// For a per-page override you can use .RenderIncludes(position: RenderPosition.Left/Right)
            /// </summary>
            [DefaultValue(RenderPosition.Left)]
            public static RenderPosition PopupRenderPosition { get; set; }

            /// <summary>
            /// By default, SqlTimings will grab a stack trace to help locate where queries are being executed.
            /// When this setting is true, no stack trace will be collected, possibly improving profiler performance.
            /// </summary>
            [DefaultValue(false)]
            public static bool ExcludeStackTraceSnippetFromSqlTimings { get; set; }

            /// <summary>
            /// When <see cref="MiniProfiler.Start"/> is called, if the current request url starts with this property,
            /// no profiler will be instantiated and no results will be displayed.  
            /// Default value is { "/mini-profiler-", "/content/", "/scripts/" }.
            /// </summary>
            [DefaultValue(new string[] { "/mini-profiler-", "/content/", "/scripts/" })]
            public static string[] IgnoredRootPaths { get; set; }

            /// <summary>
            /// The path under which ALL routes are registered in, defaults to the application root.  For example, "~/myDirectory/" would yield
            /// "/myDirectory/mini-profiler-includes.js" rather than just "/mini-profiler-includes.js"
            /// Any setting here should be in APP RELATIVE FORM, e.g. "~/myDirectory/"
            /// </summary>
            [DefaultValue("~/")]
            public static string RouteBasePath { get; set; }

            /// <summary>
            /// Understands how to save and load MiniProfilers for a very limited time. Used for caching between when
            /// a profiling session ends and results can be fetched to the client.
            /// </summary>
            /// <remarks>
            /// The normal profiling session life-cycle is as follows:
            /// 1) request begins
            /// 2) profiler is started
            /// 3) normal page/controller/request execution
            /// 4) profiler is stopped
            /// 5) profiler is cached with <see cref="ShortTermStorage"/>'s implementation of <see cref="Storage.IStorage.SaveMiniProfiler"/>
            /// 6) request ends
            /// 7) page is displayed and profiling results are ajax-fetched down, pulling cached results from 
            ///    <see cref="ShortTermStorage"/>'s implementation of <see cref="Storage.IStorage.LoadMiniProfiler"/>
            /// </remarks>
            public static Storage.IStorage ShortTermStorage { get; set; }

            /// <summary>
            /// Understands how to save and load MiniProfilers for an extended (even indefinite) time, allowing results to be
            /// shared with other developers or even tracked over time.
            /// </summary>
            public static Storage.IStorage LongTermStorage { get; set; }

            /// <summary>
            /// The formatter applied to the SQL being rendered (used only for UI)
            /// </summary>
            public static ISqlFormatter SqlFormatter { get; set; }

            /// <summary>
            /// Assembly version of this dank MiniProfiler.
            /// </summary>
            public static string Version { get; private set; }

            /// <summary>
            /// A function that determines who can access the MiniProfiler results url.  It should return true when
            /// the request client has access, false for a 401 to be returned. HttpRequest parameter is the current request and
            /// MiniProfiler parameter is the results that were profiled.
            /// </summary>
            /// <remarks>
            /// Both the HttpRequest and MiniProfiler parameters that will be passed into this function should never be null.
            /// </remarks>
            public static Func<HttpRequest, MiniProfiler, bool> Results_Authorize { get; set; }

            /// <summary>
            /// Ensures that <see cref="ShortTermStorage"/> and <see cref="LongTermStorage"/> objects are initialized. Null values will
            /// be initialized to use the default <see cref="Storage.HttpRuntimeCacheStorage"/> strategy.
            /// </summary>
            internal static void EnsureStorageStrategies()
            {
                if (ShortTermStorage == null)
                {
                    ShortTermStorage = new Storage.HttpRuntimeCacheStorage(TimeSpan.FromMinutes(20));
                }

                if (LongTermStorage == null)
                {
                    LongTermStorage = new Storage.HttpRuntimeCacheStorage(TimeSpan.FromDays(1));
                }
            }

            private static void SetProfilerIntoRuntimeCache(string key, MiniProfiler prof, DateTime expires)
            {
                HttpRuntime.Cache.Add(
                    key: key,
                    value: prof,
                    dependencies: null,
                    absoluteExpiration: expires,
                    slidingExpiration: System.Web.Caching.Cache.NoSlidingExpiration,
                    priority: System.Web.Caching.CacheItemPriority.Low,
                    onRemoveCallback: null);
            }

        }
    }
}
