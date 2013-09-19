using System;
using System.Web;
using StackExchange.Profiling;
using StackExchange.Profiling.Wcf;

namespace Sample.Wcf
{
    public class Global : HttpApplication
    {
        /// <summary>
        /// Customize aspects of the MiniProfiler.
        /// </summary>
        private void InitProfilerSettings()
        {
            MiniProfiler.Settings.ProfilerProvider = new WcfRequestProfilerProvider();

            // a powerful feature of the MiniProfiler is the ability to share links to results with other developers.
            // by default, however, long-term result caching is done in HttpRuntime.Cache, which is very volatile.
            // 
            // let's rig up serialization of our profiler results to a database, so they survive app restarts.

            // At the moment we're just echoing results back out with every request - we don't need to store these
            // MiniProfiler.Settings.Storage = new WcfRequestInstanceStorage();

            // different RDBMS have different ways of declaring sql parameters - SQLite can understand inline sql parameters just fine
            // by default, sql parameters won't be displayed
            MiniProfiler.Settings.SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter();

            // Ignore the following as we're not displaying anything through this currently
            //// these settings are optional and all have defaults, any matching setting specified in .RenderIncludes() will
            //// override the application-wide defaults specified here, for example if you had both:
            ////    MiniProfiler.Settings.PopupRenderPosition = RenderPosition.Right;
            ////    and in the page:
            ////    @MiniProfiler.RenderIncludes(position: RenderPosition.Left)
            //// then the position would be on the left that that page, and on the right (the app default) for anywhere that doesn't
            //// specified position in the .RenderIncludes() call.
            // MiniProfiler.Settings.PopupRenderPosition = RenderPosition.Right; //defaults to left
            // MiniProfiler.Settings.PopupMaxTracesToShow = 10;                  //defaults to 15
            // MiniProfiler.Settings.RouteBasePath = "~/profiler";               //e.g. /profiler/mini-profiler-includes.js

            // optional settings to control the stack trace output in the details pane
            // the exclude methods are not thread safe, so be sure to only call these once per appdomain
            MiniProfiler.Settings.ExcludeType("SessionFactory"); // Ignore any class with the name of SessionFactory
            MiniProfiler.Settings.ExcludeAssembly("NHibernate"); // Ignore any assembly named NHibernate
            MiniProfiler.Settings.ExcludeMethod("Flush");        // Ignore any method with the name of Flush
            MiniProfiler.Settings.StackMaxLength = 256;          // default is 120 characters
        }

        protected void Application_Start(object sender, EventArgs eventArgs)
        {
            InitProfilerSettings();            
        }
    }
}