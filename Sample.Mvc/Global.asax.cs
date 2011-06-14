using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

using MvcMiniProfiler;
using System.IO;
using SampleWeb.Controllers;
using Dapper;

namespace SampleWeb
{

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );
        }

        protected void Application_Start()
        {
            RegisterRoutes(RouteTable.Routes);

            InitProfilerSettings();
        }

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
                profiler = MvcMiniProfiler.MiniProfiler.Start();
            }

            using (profiler.Step("Application_BeginRequest"))
            {
                // you can start profiling your code immediately
            }
        }

        protected void Application_EndRequest()
        {
            MvcMiniProfiler.MiniProfiler.Stop();
        }


        /// <summary>
        /// Customize aspects of the MiniProfiler.
        /// </summary>
        private void InitProfilerSettings()
        {
            // a powerful feature of the MiniProfiler is the ability to share links to results with other developers.
            // by default, however, long-term result caching is done in HttpRuntime.Cache, which is very volatile.
            // 
            // let's rig up methods to binary serialize our profiler results to a database, so they survive app restarts.
            // (note: this method is more to test that the MiniProfiler can be serialized by protobuf-net - a real database storage
            // scheme would put each property into its own column, so they could be queried independently of the MiniProfiler's UI)

            // a setter will take the current profiler and should save it somewhere by its guid Id
            MiniProfiler.Settings.LongTermCacheSetter = (profiler) =>
            {
                using (var conn = BaseController.GetOpenConnection())
                {
                    // we use the insert to ignore syntax here, because MiniProfiler will
                    conn.Execute("insert or ignore into MiniProfilerResults (Id, Results) values (@id, @results)", new { id = profiler.Id, results = MiniProfiler.ToJson(profiler) });
                }
            };

            // the getter will be passed a guid and should return the saved MiniProfiler
            MiniProfiler.Settings.LongTermCacheGetter = (id) =>
            {
                using (var conn = BaseController.GetOpenConnection())
                {
                    string json = conn.Query<string>("select Results from MiniProfilerResults where Id = @id", new { id = id }).SingleOrDefault();
                    return MiniProfiler.FromJson(json);
                }
            };
        }


    }
}