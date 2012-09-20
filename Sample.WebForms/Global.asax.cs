using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using StackExchange.Profiling;

namespace Sample.WebForms
{
    public class Global : System.Web.HttpApplication
    {

        void Application_Start(object sender, EventArgs e)
        {
            InitProfilerSettings();

            // this is only done for testing purposes so we don't check in the db to source control
            ((SampleWeb.Helpers.SqliteMiniProfilerStorage)MiniProfiler.Settings.Storage).RecreateDatabase();
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
                profiler = MiniProfiler.Start();
            }

            using (profiler.Step("Application_BeginRequest"))
            {
                // you can start profiling your code immediately
            }
        }

        protected void Application_EndRequest()
        {
            MiniProfiler.Stop();
        }

        /// <summary>
        /// Customize aspects of the MiniProfiler.
        /// </summary>
        private void InitProfilerSettings()
        {
            // some things should never be seen
            var ignored = MiniProfiler.Settings.IgnoredPaths.ToList();
            ignored.Add("WebResource.axd");
            ignored.Add("/Styles/");
            MiniProfiler.Settings.IgnoredPaths = ignored.ToArray();

            MiniProfiler.Settings.Storage = new SampleWeb.Helpers.SqliteMiniProfilerStorage(SampleWeb.MvcApplication.ConnectionString);
            MiniProfiler.Settings.SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter();
        }
    }
}
