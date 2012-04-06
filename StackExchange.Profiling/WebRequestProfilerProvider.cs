using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Routing;
using StackExchange.Profiling.Helpers;

namespace StackExchange.Profiling
{
    /// <summary>
    /// HttpContext based profiler provider.  This is the default provider to use in a web context.
    /// The current profiler is associated with a HttpContext.Current ensuring that profilers are 
    /// specific to a individual HttpRequest.
    /// </summary>
    public partial class WebRequestProfilerProvider : BaseProfilerProvider
    {
        /// <summary>
        /// Public constructor.  This also registers any UI routes needed to display results
        /// </summary>
        public WebRequestProfilerProvider()
        {
            UI.MiniProfilerHandler.RegisterRoutes();
        }

        /// <summary>
        /// Starts a new MiniProfiler and associates it with the current <see cref="HttpContext.Current"/>.
        /// </summary>
        public override MiniProfiler Start(ProfileLevel level)
        {
            var context = HttpContext.Current;
            if (context == null) return null;

            var url = context.Request.Url;
            var path = context.Request.AppRelativeCurrentExecutionFilePath.Substring(1).ToUpperInvariant();

            // don't profile /content or /scripts, either - happens in web.dev
            foreach (var ignored in StackExchange.Profiling.MiniProfiler.Settings.IgnoredPaths ?? new string[0])
            {
                if (path.Contains((ignored ?? "").ToUpperInvariant()))
                    return null;
            }

            if (context.Request.Path.StartsWith(VirtualPathUtility.ToAbsolute(MiniProfiler.Settings.RouteBasePath), StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            var result = new MiniProfiler(url.OriginalString, level);
            Current = result;

            SetProfilerActive(result);

            // don't really want to pass in the context to MiniProfler's constructor or access it statically in there, either
            result.User = Settings.UserProvider.GetUser(context.Request);

            return result;
        }


        /// <summary>
        /// Ends the current profiling session, if one exists.
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/> for this HttpContext, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public override void Stop(bool discardResults)
        {
            var context = HttpContext.Current;
            if (context == null)
                return;

            var current = Current;
            if (current == null)
                return;

            // stop our timings - when this is false, we've already called .Stop before on this session
            if (!StopProfiler(current))
                return;

            if (discardResults)
            {
                Current = null;
                return;
            }

            var request = context.Request;
            var response = context.Response;

            // set the profiler name to Controller/Action or /url
            EnsureName(current, request);

            // save the profiler
            SaveProfiler(current);

            try
            {
                var arrayOfIds = StackExchange.Profiling.MiniProfiler.Settings.Storage.GetUnviewedIds(current.User);

                if (arrayOfIds != null && arrayOfIds.Count > MiniProfiler.Settings.MaxUnviewedProfiles) 
                {
                    foreach (var id in arrayOfIds.Take(arrayOfIds.Count - MiniProfiler.Settings.MaxUnviewedProfiles)) 
                    {
                        StackExchange.Profiling.MiniProfiler.Settings.Storage.SetViewed(current.User, id);
                    }
                }

                // allow profiling of ajax requests
                response.AppendHeader("X-MiniProfiler-Ids", arrayOfIds.ToJson());
            }
            catch { } // headers blew up
        }


        /// <summary>
        /// Makes sure 'profiler' has a Name, pulling it from route data or url.
        /// </summary>
        private static void EnsureName(MiniProfiler profiler, HttpRequest request)
        {
            // also set the profiler name to Controller/Action or /url
            if (string.IsNullOrWhiteSpace(profiler.Name))
            {
                var rc = request.RequestContext;
                RouteValueDictionary values;

                if (rc != null && rc.RouteData != null && (values = rc.RouteData.Values).Count > 0)
                {
                    var controller = values["Controller"];
                    var action = values["Action"];

                    if (controller != null && action != null)
                        profiler.Name = controller.ToString() + "/" + action.ToString();
                }

                if (string.IsNullOrWhiteSpace(profiler.Name))
                {
                    profiler.Name = request.Url.AbsolutePath ?? "";
                    if (profiler.Name.Length > 50)
                        profiler.Name = profiler.Name.Remove(50);
                }
            }
        }

        /// <summary>
        /// Returns the current profiler
        /// </summary>
        /// <returns></returns>
        public override MiniProfiler GetCurrentProfiler()
        {
            return Current;
        }


        private const string CacheKey = ":mini-profiler:";

        /// <summary>
        /// Gets the currently running MiniProfiler for the current HttpContext; null if no MiniProfiler was <see cref="Start"/>ed.
        /// </summary>
        private MiniProfiler Current
        {
            get
            {
                var context = HttpContext.Current;
                if (context == null) return null;

                return context.Items[CacheKey] as MiniProfiler;
            }
            set
            {
                var context = HttpContext.Current;
                if (context == null) return;

                context.Items[CacheKey] = value;
            }
        }
    }
}
