using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

using StackExchange.Profiling;
using StackExchange.Profiling.Mvc;

namespace Samples.Mvc5
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
            RegisterBundles(BundleTable.Bundles);

            InitProfilerSettings();
        }

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new ProfilingActionFilter());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }

        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/bootstrap-theme.css",
                      "~/Content/site.css"));
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
        /// Customize aspects of the MiniProfiler.
        /// </summary>
        private void InitProfilerSettings()
        {
            // A powerful feature of the MiniProfiler is the ability to share links to results with other developers.
            // by default, however, long-term result caching is done in HttpRuntime.Cache, which is very volatile.
            // 
            // Let's rig up serialization of our profiler results to a database, so they survive app restarts.
            MiniProfiler.Configure(new MiniProfilerOptions
                { RouteBasePath = "~/profiler" }
                .AddViewProfiling()    // Add MVC view profiling
                .AddEntityFramework() // Add EF Core
            );
        }
    }
}
