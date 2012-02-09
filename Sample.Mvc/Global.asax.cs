using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

using StackExchange.Profiling;
using System.IO;
using SampleWeb.Controllers;
using Dapper;
using StackExchange.Profiling.Storage;
using SampleWeb.Helpers;
using System.Data.SqlServerCe;
using SampleWeb.EFCodeFirst;
using System.Data.Entity.Infrastructure;
using System.Data.Entity;
using StackExchange.Profiling.MVCHelpers;

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

            // this is only done for testing purposes so we don't check in the db to source control
            // parameter table is only used in this project for sample queries
            ((SqliteMiniProfilerStorage)MiniProfiler.Settings.Storage).RecreateDatabase("create table RouteHits(RouteName,HitCount)");

            var efDb = HttpContext.Current.Server.MapPath("~/App_Data/SampleWeb.EFCodeFirst.EFContext.sdf");
            if (File.Exists(efDb))
            {
                File.Delete(efDb);
            }

            //Setup profiler for Controllers via a Global ActionFilter
            GlobalFilters.Filters.Add(new ProfilingActionFilter());

            // initialize automatic view profiling
            var copy = ViewEngines.Engines.ToList();
            ViewEngines.Engines.Clear();
            foreach (var item in copy)
            {
                ViewEngines.Engines.Add(new ProfilingViewEngine(item));
            }

            MiniProfilerEF.Initialize(false);
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
                profiler = StackExchange.Profiling.MiniProfiler.Start();
            }

            using (profiler.Step("Application_BeginRequest"))
            {
                // you can start profiling your code immediately
            }
        }

        protected void Application_EndRequest()
        {
            StackExchange.Profiling.MiniProfiler.Stop();
        }


        /// <summary>
        /// Customize aspects of the MiniProfiler.
        /// </summary>
        private void InitProfilerSettings()
        {
            // a powerful feature of the MiniProfiler is the ability to share links to results with other developers.
            // by default, however, long-term result caching is done in HttpRuntime.Cache, which is very volatile.
            // 
            // let's rig up serialization of our profiler results to a database, so they survive app restarts.
            MiniProfiler.Settings.Storage = new Helpers.SqliteMiniProfilerStorage(ConnectionString);

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
            MiniProfiler.Settings.PopupRenderPosition = RenderPosition.Right; //defaults to left
            MiniProfiler.Settings.PopupMaxTracesToShow = 10;                  //defaults to 15
            MiniProfiler.Settings.RouteBasePath = "~/profiler";               //e.g. /profiler/mini-profiler-includes.js

            // optional settings to control the stack trace output in the details pane
            // the exclude methods are not thread safe, so be sure to only call these once per appdomain

            MiniProfiler.Settings.ExcludeType("SessionFactory"); // Ignore any class with the name of SessionFactory
            MiniProfiler.Settings.ExcludeAssembly("NHibernate"); // Ignore any assembly named NHibernate
            MiniProfiler.Settings.ExcludeMethod("Flush");        // Ignore any method with the name of Flush
            MiniProfiler.Settings.StackMaxLength = 256;          // default is 120 characters

            // because profiler results can contain sensitive data (e.g. sql queries with parameter values displayed), we
            // can define a function that will authorize clients to see the json or full page results.
            // we use it on http://stackoverflow.com to check that the request cookies belong to a valid developer.
            MiniProfiler.Settings.Results_Authorize = (request) =>
            {
                // you may implement this if you need to restrict visibility of profiling on a per request basis 
                return true; // all requests are kosher
            };

            // the list of all sessions in the store is restricted by default, you must return true to alllow it
            MiniProfiler.Settings.Results_List_Authorize = (request) =>
            {
                // you may implement this if you need to restrict visibility of profiling lists on a per request basis 
                return true; // all requests are kosher
            };
        }

    }
}