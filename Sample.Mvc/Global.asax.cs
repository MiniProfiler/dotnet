using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

using MvcMiniProfiler;
using System.IO;
using SampleWeb.Controllers;

namespace SampleWeb
{

    public class MvcApplication : System.Web.HttpApplication
    {
        public static string ConnectionString
        {
            get { return "Data Source = " + HttpContext.Current.Server.MapPath("~/App_Data/TestMiniProfiler.sqlite"); }
        }

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
            // let's rig up methods to json serialize our profiler results to a database, so they survive app restarts.
            // (note: this method is more to test that the MiniProfiler can be serialized - a real database storage
            // scheme would put each property into its own column, so they could be queried independently of the MiniProfiler's UI)

            MiniProfiler.Settings.LongTermStorage = new Helpers.SqliteMiniProfilerStorage(ConnectionString);

            MiniProfiler.Settings.SqlFormatter = new MvcMiniProfiler.SqlFormatters.SqlServerFormatter();
			
            // these settings are optional and all have defaults, any matching setting specified in .RenderIncludes() will
            // override the application-wide defaults specified here, for example if you had both:
            //    MiniProfiler.Settings.PopupRenderPosition = RenderPosition.Right;
            //    and in the page:
            //    @MiniProfiler.RenderIncludes(position: RenderPosition.Left)
            // then the position would be on the left that that page, and on the right (the app default) for anywhere that doesn't
            // specified position in the .RenderIncludes() call.

            MiniProfiler.Settings.PopupRenderPosition = RenderPosition.Right; //defaults to left
            MiniProfiler.Settings.PopupMaxTracesToShow = 10;                  //defaults to 15
        }

    }
}