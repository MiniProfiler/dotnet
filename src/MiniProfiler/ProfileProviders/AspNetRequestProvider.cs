using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;
using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling
{
    /// <summary>
    /// For ASP.NET NON-Core applications, the MiniPofiler v3 and below style, using <see cref="HttpContext.Items"/> for storage.
    /// This is a <see cref="HttpContext"/>-based profiler provider.
    /// </summary>
    public class AspNetRequestProvider : DefaultProfilerProvider
    {
        private readonly bool _enableFallback;
        private const string CacheKey = ":mini-profiler:";
        /// <summary>
        /// Gets the currently running MiniProfiler for the current HttpContext; null if no MiniProfiler was <see cref="Start(string, MiniProfilerBaseOptions)"/>ed.
        /// </summary>
        public override MiniProfiler CurrentProfiler
        {
            get => HttpContext.Current?.Items[CacheKey] as MiniProfiler ?? (_enableFallback ? base.CurrentProfiler : null);
            protected set
            {
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Items[CacheKey] = value;
                }
                if (_enableFallback)
                {
                    base.CurrentProfiler = value;
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="AspNetRequestProvider"/>, optionally enabling fall back to async context.
        /// </summary>
        /// <param name="enableFallback">Enables AsyncLocal fall back (if not found in HttpContext.Current.Items.</param>
        public AspNetRequestProvider(bool enableFallback = false) => _enableFallback = enableFallback;

        /// <summary>
        /// Starts a new MiniProfiler and associates it with the current <see cref="HttpContext.Current"/>.
        /// </summary>
        /// <param name="profilerName">The name for the started <see cref="MiniProfiler"/>.</param>
        /// <param name="options">The options to start the MiniPofiler with. Likely a more-specific type underneath.</param>
        public override MiniProfiler Start(string profilerName, MiniProfilerBaseOptions options)
        {
            var request = HttpContext.Current?.Request;
            var path = request?.Path;
            if (path == null) return null;

            // If the application is hosted in the root directory (appPath.Length == 1), return entire path
            // Otherwise, return the substring after the path (e.g. a virtual directory)
            // This is for paths like /virtual/path.axd/more/path/omg
            var relativePath = path.Length < request.ApplicationPath.Length || request.ApplicationPath.Length == 1 ? path : path.Substring(request.ApplicationPath.Length);

            foreach (var ignored in options.IgnoredPaths)
            {
                if (relativePath.Contains(ignored ?? string.Empty, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }
            }

            var mpo = options as MiniProfilerOptions;
            if (mpo != null && path.StartsWith(VirtualPathUtility.ToAbsolute(mpo.RouteBasePath), StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return CurrentProfiler = new MiniProfiler(profilerName, options)
            {
                User = mpo?.UserIdProvider?.Invoke(request)
            };
        }

        /// <summary>
        /// Ends the current profiling session, if one exists.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to stop.</param>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/> for this HttpContext, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public override void Stopped(MiniProfiler profiler, bool discardResults)
        {
            var context = HttpContext.Current;
            if (context == null || profiler == null) return;

            if (discardResults && CurrentProfiler == profiler)
            {
                CurrentProfiler = null;
                return;
            }

            // set the profiler name to Controller/Action or /url
            EnsureName(profiler, context.Request);
            Save(profiler);

            try
            {
                var ids = profiler.Options.ExpireAndGetUnviewed(profiler.User);
                // allow profiling of AJAX requests
                if (ids?.Count > 0)
                {
                    context.Response.AppendHeader("X-MiniProfiler-Ids", ids.ToJson());
                }
            }
            catch { /* headers blew up */ }
        }

        /// <summary>
        /// Asynchronously ends the current profiling session, if one exists.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to stop.</param>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/> for this HttpContext, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public override async Task StoppedAsync(MiniProfiler profiler, bool discardResults)
        {
            var context = HttpContext.Current;
            if (context == null || profiler == null) return;

            if (discardResults && CurrentProfiler == profiler)
            {
                CurrentProfiler = null;
                return;
            }

            // set the profiler name to Controller/Action or /url
            EnsureName(profiler, context.Request);
            await SaveAsync(profiler).ConfigureAwait(false);

            try
            {
                var ids = await profiler.Options.ExpireAndGetUnviewedAsync(profiler.User).ConfigureAwait(false);
                // allow profiling of AJAX requests
                if (ids?.Count > 0)
                {
                    context.Response.AppendHeader("X-MiniProfiler-Ids", ids.ToJson());
                }
            }
            catch { /* headers blew up */ }
        }

        /// <summary>
        /// Makes sure <paramref name="profiler"/> has a Name, pulling it from route data or URL.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to ensure a name is set on.</param>
        /// <param name="request">The <see cref="HttpRequest"/> request to get the name from.</param>
        private static void EnsureName(MiniProfiler profiler, HttpRequest request)
        {
            // also set the profiler name to Controller/Action or /url
            if (profiler.Name.IsNullOrWhiteSpace())
            {
                var rc = request.RequestContext;
                RouteValueDictionary values;

                if (rc?.RouteData != null && (values = rc.RouteData.Values).Count > 0)
                {
                    var controller = values["Controller"];
                    var action = values["Action"];

                    if (controller != null && action != null)
                    {
                        profiler.Name = controller + "/" + action;
                    }
                }

                if (profiler.Name.IsNullOrWhiteSpace())
                {
                    profiler.Name = request.Url.AbsolutePath ?? string.Empty;
                    if (profiler.Name.Length > 50)
                        profiler.Name = profiler.Name.Remove(50);
                }

                if (profiler.Name.HasValue() && profiler.Root != null && profiler.Root.Name == null)
                {
                    profiler.Root.Name = profiler.Name;
                }
            }
        }
    }
}
