using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Routing;

using MvcMiniProfiler.Helpers;

namespace MvcMiniProfiler.UI
{
    /// <summary>
    /// Understands how to route and respond to MiniProfiler UI urls.
    /// </summary>
    public class MiniProfilerHandler : IRouteHandler, IHttpHandler
    {
        internal static void RegisterRoutes()
        {
            var urls = new[] { "mini-profiler-includes.js", "mini-profiler-includes.less", "mini-profiler-results" };
            var routes = RouteTable.Routes;
            var handler = new MiniProfilerHandler();

            using (routes.GetWriteLock())
            {
                foreach (var url in urls)
                {
                    var route = new Route(url, handler)
                    {
                        // we have to specify these, so no MVC route helpers will match, e.g. @Html.ActionLink("Home", "Index", "Home")
                        Defaults = new RouteValueDictionary(new { controller = "MiniProfilerHandler", action = "ProcessRequest" })
                    };

                    // put our routes at the beginning, like a boss
                    routes.Insert(0, route);
                }
            }
        }

        /// <summary>
        /// Returns this <see cref="MiniProfilerHandler"/> to handle <paramref name="requestContext"/>.
        /// </summary>
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return this; // elegant? I THINK SO.
        }

        /// <summary>
        /// Try to keep everything static so we can easily be reused.
        /// </summary>
        public bool IsReusable
        {
            get { return true; }
        }

        /// <summary>
        /// Returns either includes' css/javascript or results' html.
        /// </summary>
        public void ProcessRequest(HttpContext context)
        {
            string output;

            switch (Path.GetFileNameWithoutExtension(context.Request.Url.AbsolutePath))
            {
                case "mini-profiler-includes":
                    output = Includes(context);
                    break;

                case "mini-profiler-results":
                    output = Results(context);
                    break;

                default:
                    output = NotFound(context);
                    break;
            }

            context.Response.Write(output);
        }

        /// <summary>
        /// Handles rendering our .js and .less static content files.
        /// </summary>
        private static string Includes(HttpContext context)
        {
            var extension = Path.GetExtension(context.Request.Url.AbsolutePath);
            if (string.IsNullOrWhiteSpace(extension)) return NotFound(context);

            var response = context.Response;
            var filename = "Includes" + extension;

            switch (extension)
            {
                case ".js":
                    response.ContentType = "application/javascript";
                    break;
                case ".less":
                    response.ContentType = "text/plain";
                    break;
                default:
                    return NotFound(context);
            }

            var cache = response.Cache;
            cache.SetCacheability(System.Web.HttpCacheability.Public);
            cache.SetExpires(DateTime.Now.AddDays(7));
            cache.SetValidUntilExpires(true);

            return GetResource(filename);
        }

        /// <summary>
        /// Handles rendering a previous MiniProfiler session, identified by its "?id=GUID" on the query.
        /// </summary>
        private static string Results(HttpContext context)
        {
            // when we're rendering as a button/popup in the corner, we'll pass ?popup=1
            // if it's absent, we're rendering results as a full page for sharing
            var isPopup = !string.IsNullOrWhiteSpace(context.Request.QueryString["popup"]);

            // this guid is the MiniProfiler.Id property
            Guid id;
            if (!Guid.TryParse(context.Request.QueryString["id"], out id))
                return isPopup ? NotFound(context) : NotFound(context, "text/html", "No Guid id specified on the query string");

            MiniProfiler.Settings.EnsureCacheMethods();
            var profiler = MiniProfiler.Settings.ShortTermCacheGetter(id);

            if (profiler == null)
                profiler = MiniProfiler.Settings.LongTermCacheGetter(id);

            if (profiler == null)
                return isPopup ? NotFound(context) : NotFound(context, "text/html", "No MiniProfiler results found with Id=" + id.ToString());

            // the first time we hit this route as a full results page, the prof won't be in long term cache, so put it there for sharing
            // each subsequent time the full page is hit, just save again, so we act as a sliding expiration
            if (!isPopup)
                MiniProfiler.Settings.LongTermCacheSetter(profiler);

            var html = GetResource("MiniProfilerResults.cshtml");
            var model = new MiniProfilerResultsModel { MiniProfiler = profiler, IsPopup = isPopup };

            return RazorCompiler.Render(html, model);
        }

        private static string GetResource(string filename)
        {
            string result;

            if (!_ResourceCache.TryGetValue(filename, out result))
            {
                using (var stream = typeof(MiniProfilerHandler).Assembly.GetManifestResourceStream("MvcMiniProfiler.UI." + filename))
                using (var reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }

                _ResourceCache[filename] = result;
            }

            return result;
        }

        /// <summary>
        /// Embedded resource contents keyed by filename.
        /// </summary>
        private static readonly Dictionary<string, string> _ResourceCache = new Dictionary<string, string>();

        /// <summary>
        /// Helper method that sets a proper 404 response code.
        /// </summary>
        private static string NotFound(HttpContext context, string contentType = "text/plain", string message = null)
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = contentType;

            return message;
        }

    }
}
