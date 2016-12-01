using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using StackExchange.Profiling.Helpers;
using StackExchange.Profiling.SqlFormatters;
using StackExchange.Profiling.Storage;

#if NET45
using System.Web;
#endif

namespace StackExchange.Profiling
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
                var props = from p in typeof(Settings)
#if !NET45 // TODO: Revisit in .NET Standard 2.0
                            .GetTypeInfo()
#endif
                            .GetProperties(BindingFlags.Public | BindingFlags.Static)
                            let t = typeof(DefaultValueAttribute)
                            where p.IsDefined(t, inherit: false)
                            let a = p.GetCustomAttributes(t, inherit: false).Single() as DefaultValueAttribute
                            select new { PropertyInfo = p, DefaultValue = a };

                foreach (var pair in props)
                {
                    pair.PropertyInfo.SetValue(null, Convert.ChangeType(pair.DefaultValue.Value, pair.PropertyInfo.PropertyType), null);
                }

// TODO: Version off of the git hash and/or NuGet instead, set in the build
#if NET45
                // this assists in debug and is also good for prd, the version is a hash of the main assembly 
                string location;
                try
                {
                    location = typeof (Settings).Assembly.Location;
                }
                catch
                {
                    location = HttpContext.Current.Server.MapPath("~/bin/MiniProfiler.dll");
                }

                try
                {
                    List<string> files = new List<string>();
                    files.Add(location);

                    string customUITemplatesPath = "";
                    if (HttpContext.Current != null)
                        customUITemplatesPath = HttpContext.Current.Server.MapPath(MiniProfiler.Settings.CustomUITemplates);

                    if (System.IO.Directory.Exists(customUITemplatesPath))
                    {
                        files.AddRange(System.IO.Directory.EnumerateFiles(customUITemplatesPath));
                    }

                    using (var sha256 = new System.Security.Cryptography.SHA256CryptoServiceProvider())
                    {
                        byte[] hash = new byte[sha256.HashSize / 8];
                        foreach (string file in files)
                        {
                            // sha256 can throw a FIPS exception, but SHA256CryptoServiceProvider is FIPS BABY - FIPS 
                            byte[] contents = System.IO.File.ReadAllBytes(file);
                            byte[] hashfile = sha256.ComputeHash(contents);
                            for (int i = 0; i < (sha256.HashSize / 8); i++)
                            {
                                hash[i] = (byte)(hashfile[i] ^ hash[i]);
                            }
                        }
                        Version = System.Convert.ToBase64String(hash);
                    }
                }
                catch
                {
                    Version = Guid.NewGuid().ToString();
                }
#else
                // TODO: Package and git version, set on the build...
                // Note: this is used as the cache breaker on the client side, so template files matter
                Version = "FIX ME!";
#endif

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
                    typeof(Settings)
#if !NET45
                    .GetTypeInfo()
#endif
                    .Assembly.GetName().Name,

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

                // for normal usage, this will return a System.Diagnostics.Stopwatch to collect times - unit tests can explicitly set how much time elapses
                StopwatchProvider = StopwatchWrapper.StartNew;
            }

            /// <summary>
            /// Assemblies to exclude from the stack trace report.
            /// Add to this using the <see cref="ExcludeAssembly"/> method.
            /// </summary>
            public static IEnumerable<string> AssembliesToExclude => assembliesToExclude;

            /// <summary>
            /// Types to exclude from the stack trace report.
            /// Add to this using the <see cref="ExcludeType"/> method.
            /// </summary>
            public static IEnumerable<string> TypesToExclude => typesToExclude;

            /// <summary>
            /// Methods to exclude from the stack trace report.
            /// Add to this using the <see cref="ExcludeMethod"/> method.
            /// </summary>
            public static IEnumerable<string> MethodsToExclude => methodsToExclude;

            /// <summary>
            /// Excludes the specified assembly from the stack trace output.
            /// </summary>
            /// <param name="assemblyName">The short name of the assembly. AssemblyName.Name</param>
            public static void ExcludeAssembly(string assemblyName) => assembliesToExclude.Add(assemblyName);

            /// <summary>
            /// Excludes the specified type from the stack trace output.
            /// </summary>
            /// <param name="typeToExclude">The System.Type name to exclude</param>
            public static void ExcludeType(string typeToExclude) => typesToExclude.Add(typeToExclude);

            /// <summary>
            /// Excludes the specified method name from the stack trace output.
            /// </summary>
            /// <param name="methodName">The name of the method</param>
            public static void ExcludeMethod(string methodName) => methodsToExclude.Add(methodName);

            /// <summary>
            /// The maximum number of unviewed profiler sessions (set this low cause we don't want to blow up headers)
            /// </summary>
            [DefaultValue(20)]
            public static int MaxUnviewedProfiles { get; set; }

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
            /// Allows showing/hiding of popup results buttons via keyboard.
            /// </summary>
            [DefaultValue("Alt+P")]
            public static string PopupToggleKeyboardShortcut { get; set; }

            /// <summary>
            /// When true, results buttons will not initially be shown, requiring keyboard activation via <see cref="PopupToggleKeyboardShortcut"/>.
            /// </summary>
            [DefaultValue(false)]
            public static bool PopupStartHidden { get; set; }

            /// <summary>
            /// Determines if min-max, clear, etc are rendered; defaults to false.
            /// For a per-page override you can use .RenderIncludes(showControls: true/false)
            /// </summary>
            [DefaultValue(false)]
            public static bool ShowControls { get; set; }

            /// <summary>
            /// By default, <see cref="CustomTiming"/>s created by this assmebly will grab a stack trace to help 
            /// locate where Remote Procedure Calls are being executed.  When this setting is true, no stack trace 
            /// will be collected, possibly improving profiler performance.
            /// </summary>
            [DefaultValue(false)]
            public static bool ExcludeStackTraceSnippetFromCustomTimings { get; set; }

            /// <summary>
            /// When <see cref="Start(string)"/> is called, if the current request url contains any items in this property,
            /// no profiler will be instantiated and no results will be displayed.
            /// Default value is { "/content/", "/scripts/", "/favicon.ico" }.
            /// </summary>
            [DefaultValue(new string[] { "/content/", "/scripts/", "/favicon.ico" })]
            public static string[] IgnoredPaths { get; set; }

            /// <summary>
            /// The path under which ALL routes are registered in, defaults to the application root.  For example, "~/myDirectory/" would yield
            /// "/myDirectory/includes.js" rather than just "/mini-profiler-resources/includes.js"
            /// Any setting here should be in APP RELATIVE FORM, e.g. "~/myDirectory/"
            /// </summary>
            [DefaultValue("~/mini-profiler-resources")]
            public static string RouteBasePath { get; set; }

            /// <summary>
            /// The path where custom ui elements are stored.
            /// If the custom file doesn't exist, the standard resource is used.
            /// This setting should be in APP RELATIVE FORM, e.g. "~/App_Data/MiniProfilerUI"
            /// </summary>
            /// <remarks>A web server restart is required to reload new files.</remarks>
            [DefaultValue("~/App_Data/MiniProfilerUI")]
            public static string CustomUITemplates { get; set; }

            /// <summary>
            /// Maximum payload size for json responses in bytes defaults to 2097152 characters, which is equivalent to 4 MB of Unicode string data.
            /// </summary>
            [DefaultValue(2097152)]
            public static int MaxJsonResponseSize { get; set; }

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
            /// 5) profiler is cached with <see cref="Storage"/>'s implementation of <see cref="IStorage.Save"/>
            /// 6) request ends
            /// 7) page is displayed and profiling results are ajax-fetched down, pulling cached results from 
            ///    <see cref="Storage"/>'s implementation of <see cref="IStorage.Load"/>
            /// </remarks>
            public static IStorage Storage { get; set; }

            /// <summary>
            /// The formatter applied to any SQL before being set in a <see cref="CustomTiming.CommandString"/>.
            /// </summary>
            public static ISqlFormatter SqlFormatter { get; set; }
            
            /// <summary>
            /// Assembly version of this dank MiniProfiler.
            /// </summary>
            public static string Version { get; private set; }

            /// <summary>
            /// The <see cref="IProfilerProvider"/> class that is used to run MiniProfiler
            /// </summary>
            /// <remarks>
            /// If not set explicitly, will default to <see cref="DefaultProfilerProvider"/>
            /// </remarks>
            public static IProfilerProvider ProfilerProvider { get; set; }

            private static Func<IStorage> _defaultStorage = () => new NullStorage();
            private static Func<IProfilerProvider> _defaultProfilerProvider = () => new DefaultProfilerProvider();

            /// <summary>
            /// Sets the default provider generators for MiniProfiler. 
            /// This allows inheriting libraries to set their default providers.
            /// </summary>
            /// <param name="defaultStorage">The getter for the default storage profiler to use.</param>
            /// <param name="defaultProfilerProvider">The getter for the default profiler profiler to use.</param>
            public static void SetDefaults(Func<IStorage> defaultStorage, Func<IProfilerProvider> defaultProfilerProvider)
            {
                _defaultStorage = defaultStorage;
                _defaultProfilerProvider = defaultProfilerProvider;
            }

            /// <summary>
            /// Make sure we can at least store profiler results to the http runtime cache.
            /// </summary>
            public static void EnsureStorageStrategy()
            {
                // TODO: refactor this into an on-demand with Storage access
                if (Storage == null)
                {
                    Storage = _defaultStorage();
                    //Storage = new Storage.HttpRuntimeCacheStorage(TimeSpan.FromDays(1));
                }
            }

            internal static void EnsureProfilerProvider()
            {
                if (ProfilerProvider == null)
                {
                    ProfilerProvider = _defaultProfilerProvider();
                    //ProfilerProvider =  new WebRequestProfilerProvider();
                }
            }
            
            // TODO: IntervalsVisibleTo
            /// <summary>
            /// Allows switching out stopwatches for unit testing.
            /// </summary>
            public static Func<IStopwatch> StopwatchProvider { get; set; }

            /// <summary>
            /// By default, the output of the MiniProfilerHandler is compressed, if the request supports that.
            /// If this setting is false, the output won't be compressed. (Only do this when you take care of compression yourself)
            /// </summary>
            [DefaultValue(true)]
            public static bool EnableCompression { get; set; }
        }
    }
}
