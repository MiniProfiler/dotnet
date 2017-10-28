using System.IO;
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
        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public static string ConnectionString => "FullUri=file::memory:?cache=shared";

        protected void Application_Start()
        {
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
                RouteBasePath = "~/profiler",
            }
            .AddViewPofiling()    // Add MVC view profiling
            .AddEntityFramework() // Add EF Core
            );
        }
    }
}
