using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

using StackExchange.Profiling;
using StackExchange.Profiling.EntityFramework6;
using StackExchange.Profiling.Mvc;
using StackExchange.Profiling.Storage;
using Samples.Mvc5.Helpers;

namespace Samples.Mvc5
{
    public class MvcApplication : HttpApplication
    {
        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public static string ConnectionString =>
            "Data Source = " + HttpContext.Current.Server.MapPath("~/App_Data/TestMiniProfiler.sqlite");

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            // Note: ProfilingActionFilter is add in the FilterConfig
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            InitProfilerSettings();

            // this is only done for testing purposes so we don't check in the db to source control
            // parameter table is only used in this project for sample queries
            // yes, it is ugly, and do not do this unless you know for sure that the second Store in the MultiStorageProvider is of this type
            ((SqliteMiniProfilerStorage)((MultiStorageProvider)MiniProfiler.Settings.Storage).Stores[1]).RecreateDatabase("create table RouteHits(RouteName,HitCount,unique(RouteName))");

            var entityFrameworkDataPath = HttpContext.Current.Server.MapPath("~/App_Data/Samples.Mvc5.EFCodeFirst.EFContext.sdf");
            if (File.Exists(entityFrameworkDataPath))
            {
                File.Delete(entityFrameworkDataPath);
            }

            // initialize automatic view profiling
            var copy = ViewEngines.Engines.ToList();
            ViewEngines.Engines.Clear();
            foreach (var item in copy)
            {
                ViewEngines.Engines.Add(new ProfilingViewEngine(item));
            }

            MiniProfilerEF6.Initialize();
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
                profiler = MiniProfiler.Start();
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
            MiniProfiler.Stop();
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
            // a powerful feature of the MiniProfiler is the ability to share links to results with other developers.
            // by default, however, long-term result caching is done in HttpRuntime.Cache, which is very volatile.
            // 
            // let's rig up serialization of our profiler results to a database, so they survive app restarts.

            // Setting up a MultiStorage provider. This will store results in the HttpRuntimeCache (normally the default) and in SqlLite as well.
            MiniProfiler.Settings.Storage = new MultiStorageProvider(
                new HttpRuntimeCacheStorage(new TimeSpan(1, 0, 0)),
                new SqliteMiniProfilerStorage(ConnectionString)
                );

            // different RDBMS have different ways of declaring sql parameters - SQLite can understand inline sql parameters just fine
            // by default, sql parameters won't be displayed
            MiniProfiler.Settings.SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter();

            // these settings are optional and all have defaults, any matching setting specified in .RenderIncludes() will
            // override the application-wide defaults specified here, for example if you had both:
            //    MiniProfiler.Settings.PopupRenderPosition = RenderPosition.Right;
            //    and in the page:
            //    @MiniProfiler.RenderIncludes(position: RenderPosition.Left)
            // then the position would be on the left that that page, and on the right (the app default) for anywhere that doesn't
            // specified position in the .RenderIncludes() call.
            MiniProfiler.Settings.PopupRenderPosition = RenderPosition.Right;          // defaults to left
            MiniProfiler.Settings.RouteBasePath = "~/profiler";                        // e.g. /profiler/mini-profiler-includes.js
            MiniProfiler.Settings.PopupMaxTracesToShow = 10;                           // defaults to 15
            MiniProfiler.Settings.ProfilerProvider = new WebRequestProfilerProvider(); // use the web profiler since we're in a web application

            // optional settings to control the stack trace output in the details pane
            // the exclude methods are not thread safe, so be sure to only call these once per appdomain
            MiniProfiler.Settings.ExcludeType("SessionFactory"); // Ignore any class with the name of SessionFactory
            MiniProfiler.Settings.ExcludeAssembly("NHibernate"); // Ignore any assembly named NHibernate
            MiniProfiler.Settings.ExcludeMethod("Flush");        // Ignore any method with the name of Flush
            // MiniProfiler.Settings.ShowControls = true;
            MiniProfiler.Settings.StackMaxLength = 256;          // default is 120 characters

            // because profiler results can contain sensitive data (e.g. sql queries with parameter values displayed), we
            // can define a function that will authorize clients to see the json or full page results.
            // we use it on http://stackoverflow.com to check that the request cookies belong to a valid developer.
            MiniProfilerWebSettings.ResultsAuthorize = request =>
            {
                // you may implement this if you need to restrict visibility of profiling on a per request basis

                // for example, for this specific path, we'll only allow profiling if a query parameter is set
                if ("/Home/ResultsAuthorization".Equals(request.Url.LocalPath, StringComparison.OrdinalIgnoreCase))
                {
                    return (request.Url.Query).ToLower().Contains("isauthorized");
                }

                // all other paths can check our global switch
                return !DisableProfilingResults;
            };

            // the list of all sessions in the store is restricted by default, you must return true to allow it
            MiniProfilerWebSettings.ResultsListAuthorize = request =>
            {
                // you may implement this if you need to restrict visibility of profiling lists on a per request basis 
                return true; // all requests are kosher
            };
        }
    }
}
