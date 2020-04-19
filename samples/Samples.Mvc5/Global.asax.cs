using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

using StackExchange.Profiling;
using StackExchange.Profiling.EntityFramework6;
using StackExchange.Profiling.Mvc;
using StackExchange.Profiling.Storage;
using Samples.Mvc5.Helpers;
using System.Data.SQLite;

namespace Samples.Mvc5
{
    public class MvcApplication : HttpApplication
    {
        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public static string ConnectionString => "FullUri=file::memory:?cache=shared";
        private static readonly SQLiteConnection TrapConnection = new SQLiteConnection(ConnectionString);

        protected void Application_Start()
        {
            TrapConnection.Open(); //Hold the in-memory SQLite database open

            AreaRegistration.RegisterAllAreas();
            // Note: ProfilingActionFilter is added in the FilterConfig
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            InitProfilerSettings();

            var entityFrameworkDataPath = HttpContext.Current.Server.MapPath("~/App_Data/Samples.Mvc5.EFCodeFirst.EFContext.sdf");
            if (File.Exists(entityFrameworkDataPath))
            {
                File.Delete(entityFrameworkDataPath);
            }
        }

        /// <summary>
        /// The application begin request event.
        /// </summary>
        protected void Application_BeginRequest()
        {
            MiniProfiler profiler = null;

            // might want to decide here (or maybe inside the action) whether you want
            // to profile this request - for example, using an "IsSystemAdmin" flag against
            // the user, or similar; this could also all be done in action filters, but this
            // is simple and practical; just return null for most users. For our test, we'll
            // profile only for local requests (seems reasonable)
            if (Request.IsLocal)
            {
                profiler = MiniProfiler.StartNew();
            }

            using (profiler.Step("Application_BeginRequest"))
            {
                // you can start profiling your code immediately
            }
        }

        /// <summary>
        /// The application end request.
        /// </summary>
        protected void Application_EndRequest()
        {
            MiniProfiler.Current?.Stop();
        }

        /// <summary>
        /// Gets or sets a value indicating whether disable profiling results.
        /// </summary>
        public static bool DisableProfilingResults { get; set; }

        /// <summary>
        /// Customize aspects of the MiniProfiler.
        /// </summary>
        private void InitProfilerSettings()
        {
            // A powerful feature of the MiniProfiler is the ability to share links to results with other developers.
            // by default, however, long-term result caching is done in HttpRuntime.Cache, which is very volatile.
            // 
            // Let's rig up serialization of our profiler results to a database, so they survive app restarts.
            MiniProfiler.Configure(new MiniProfilerOptions
            {
                // Sets up the route to use for MiniProfiler resources:
                // Here, ~/profiler is used for things like /profiler/mini-profiler-includes.js)
                RouteBasePath = "~/profiler",

                // Setting up a MultiStorage provider. This will store results in the MemoryCacheStorage (normally the default) and in SqlLite as well.
                Storage = new MultiStorageProvider(
                    new MemoryCacheStorage(new TimeSpan(1, 0, 0)),
                    // The RecreateDatabase call is only done for testing purposes, so we don't check in the db to source control.
                    new SqliteMiniProfilerStorage(ConnectionString).RecreateDatabase("create table RouteHits(RouteName,HitCount,unique(RouteName))")
                    ),

                // Different RDBMS have different ways of declaring sql parameters - SQLite can understand inline sql parameters just fine.
                // By default, sql parameters will be displayed.
                //SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter(),

                // These settings are optional and all have defaults, any matching setting specified in .RenderIncludes() will
                // override the application-wide defaults specified here, for example if you had both:
                //    PopupRenderPosition = RenderPosition.Right;
                //    and in the page:
                //    @MiniProfiler.Current.RenderIncludes(position: RenderPosition.Left)
                // ...then the position would be on the left on that page, and on the right (the application default) for anywhere that doesn't
                // specified position in the .RenderIncludes() call.
                PopupRenderPosition = RenderPosition.Right,  // defaults to left
                PopupMaxTracesToShow = 10,                   // defaults to 15
                ColorScheme = ColorScheme.Auto,              // defaults to light

                // ResultsAuthorize (optional - open to all by default):
                // because profiler results can contain sensitive data (e.g. sql queries with parameter values displayed), we
                // can define a function that will authorize clients to see the JSON or full page results.
                // we use it on http://stackoverflow.com to check that the request cookies belong to a valid developer.
                ResultsAuthorize = request =>
                {
                    // you may implement this if you need to restrict visibility of profiling on a per request basis

                    // for example, for this specific path, we'll only allow profiling if a query parameter is set
                    if ("/Home/ResultsAuthorization".Equals(request.Url.LocalPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return (request.Url.Query).IndexOf("isauthorized", StringComparison.OrdinalIgnoreCase) >= 0;
                    }

                    // all other paths can check our global switch
                    return !DisableProfilingResults;
                },

                // ResultsListAuthorize (optional - open to all by default)
                // the list of all sessions in the store is restricted by default, you must return true to allow it
                ResultsListAuthorize = _ =>
                {
                    // you may implement this if you need to restrict visibility of profiling lists on a per request basis 
                    return true; // all requests are legit in our happy world
                },

                // Stack trace settings
                StackMaxLength = 256, // default is 120 characters

                // (Optional) You can disable "Connection Open()", "Connection Close()" (and async variant) tracking.
                // (defaults to true, and connection opening/closing is tracked)
                TrackConnectionOpenClose = true
        }
            // Optional settings to control the stack trace output in the details pane
            .ExcludeType("SessionFactory")  // Ignore any class with the name of SessionFactory)
            .ExcludeAssembly("NHibernate")  // Ignore any assembly named NHibernate
            .ExcludeMethod("Flush")         // Ignore any method with the name of Flush
            .AddViewProfiling()              // Add MVC view profiling
            );

            MiniProfilerEF6.Initialize();
        }
    }
}
