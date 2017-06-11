using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Routing;
using StackExchange.Profiling.Helpers;
using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Understands how to route and respond to MiniProfiler UI URLS.
    /// </summary>
    public class MiniProfilerHandler : IRouteHandler, IHttpHandler
    {
        /// <summary>
        /// Embedded resource contents keyed by filename.
        /// </summary>
        private static readonly ConcurrentDictionary<string, string> ResourceCache = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Gets a value indicating whether to keep things static and reusable.
        /// </summary>
        public bool IsReusable => true;

        /// <summary>
        /// Usually called internally, sometimes you may clear the routes during the apps lifecycle, 
        /// if you do that call this to bring back mini profiler.
        /// </summary>
        public static void RegisterRoutes()
        {
            var prefix = MiniProfiler.Settings.RouteBasePath.Replace("~/", string.Empty).EnsureTrailingSlash();

            using (RouteTable.Routes.GetWriteLock())
            {
                var route = new Route(prefix + "{filename}", new MiniProfilerHandler())
                {
                    // specify these, so no MVC route helpers will match, e.g. @Html.ActionLink("Home", "Index", "Home")
                    Defaults = new RouteValueDictionary(new { controller = nameof(MiniProfilerHandler), action = nameof(ProcessRequest) }),
                    Constraints = new RouteValueDictionary(new { controller = nameof(MiniProfilerHandler), action = nameof(ProcessRequest) })
                };

                // put our routes at the beginning, like a boss
                RouteTable.Routes.Insert(0, route);
            }
        }

        /// <summary>
        /// Returns this <see cref="MiniProfilerHandler"/> to handle <paramref name="requestContext"/>.
        /// </summary>
        /// <param name="requestContext">The <see cref="RequestContext"/> to handle.</param>
        IHttpHandler IRouteHandler.GetHttpHandler(RequestContext requestContext) => this;

        /// <summary>
        /// Returns either includes' <c>css/javascript</c> or results' html.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> to process.</param>
        public void ProcessRequest(HttpContext context)
        {
            string output;
            string path = context.Request.AppRelativeCurrentExecutionFilePath;

            switch (Path.GetFileNameWithoutExtension(path).ToLowerInvariant())
            {
                case "includes":
                    output = Includes(context, path);
                    break;

                case "results-index":
                    output = ResultsIndex(context);
                    break;

                case "results-list":
                    output = ResultsList(context);
                    break;

                case "results":
                    output = GetSingleProfilerResult(context);
                    break;

                default:
                    output = NotFound(context);
                    break;
            }

            if (MiniProfilerWebSettings.EnableCompression && output.HasValue())
            {
                Compression.EncodeStreamAndAppendResponseHeaders(context.Request, context.Response);
            }

            context.Response.Write(output);
        }

        /// <summary>
        /// Handles rendering static content files.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> being handled.</param>
        /// <param name="path">The path being requested.</param>
        private static string Includes(HttpContext context, string path)
        {
            var response = context.Response;
            switch (Path.GetExtension(path))
            {
                case ".js":
                    response.ContentType = "application/javascript";
                    break;
                case ".css":
                    response.ContentType = "text/css";
                    break;
                case ".tmpl":
                    response.ContentType = "text/x-jquery-tmpl";
                    break;
                default:
                    return NotFound(context);
            }

            return TryGetResource(Path.GetFileName(path), out string resource) ? resource : NotFound(context);
        }

        private static string ResultsIndex(HttpContext context)
        {
            if (!AuthorizeRequest(context, isList: true, message: out string message))
            {
                return message;
            }

            context.Response.ContentType = "text/html";

            var path = VirtualPathUtility.ToAbsolute(MiniProfiler.Settings.RouteBasePath).EnsureTrailingSlash();
            var version = MiniProfiler.Settings.VersionHash;
            return $@"<html>
  <head>
    <title>List of profiling sessions</title>
    <script id=""mini-profiler"" data-ids="""" src=""{path}includes.js?v={version}""></script>
    <link href=""{path}includes.css?v={version}"" rel=""stylesheet"" />
    <script>MiniProfiler.list.init({{path: '{path}', version: '{version}'}});</script>
  </head>
</html>";
        }

        /// <summary>
        /// Returns true if the current request is allowed to see the profiler response.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context for the request being authorixed.</param>
        /// <param name="isList">Whether this is a list route being accessed.</param>
        /// <param name="message">The access denied message, if present.</param>
        private static bool AuthorizeRequest(HttpContext context, bool isList, out string message)
        {
            message = null;
            var authorize = MiniProfilerWebSettings.ResultsAuthorize;
            var authorizeList = MiniProfilerWebSettings.ResultsListAuthorize;

            if ((authorize != null && !authorize(context.Request)) || (isList && (authorizeList == null || !authorizeList(context.Request))))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "text/plain";
                message = "unauthorized";
                return false;
            }

            return true;
        }

        private static string ResultsList(HttpContext context)
        {
            if (!AuthorizeRequest(context, isList: true, message: out string message))
            {
                return message;
            }
            var guids = MiniProfiler.Settings.Storage.List(100);
            var lastId = context.Request["last-id"];

            if (!lastId.IsNullOrWhiteSpace() && Guid.TryParse(lastId, out var lastGuid))
            {
                guids = guids.TakeWhile(g => g != lastGuid);
            }

            return guids.Reverse()
                        .Select(g => MiniProfiler.Settings.Storage.Load(g))
                        .Where(p => p != null)
                        .Select(p => new
                        {
                            p.Id,
                            p.Name,
                            p.ClientTimings,
                            p.Started,
                            p.HasUserViewed,
                            p.MachineName,
                            p.User,
                            p.DurationMilliseconds
                        }).ToJson();
        }

        /// <summary>
        /// Returns either json or full page html of a previous <see cref="MiniProfiler"/> session, 
        /// identified by its <c>"?id=GUID"</c> on the query.
        /// </summary>
        /// <param name="context">The context to get a profiler response for.</param>
        private static string GetSingleProfilerResult(HttpContext context)
        {
            // when we're rendering as a button/popup in the corner, we'll pass ?popup=1
            // if it's absent, we're rendering results as a full page for sharing
            var isPopup = context.Request["popup"].HasValue();
            // this guid is the MiniProfiler.Id property
            // if this guid is not supplied, the last set of results needs to be
            // displayed. The home page doesn't have profiling otherwise.
            if (!Guid.TryParse(context.Request["id"], out var id) && MiniProfiler.Settings.Storage != null)
                id = MiniProfiler.Settings.Storage.List(1).FirstOrDefault();

            if (id == default(Guid))
                return isPopup ? NotFound(context) : NotFound(context, "text/plain", "No Guid id specified on the query string");

            var profiler = MiniProfiler.Settings.Storage.Load(id);
            string user = MiniProfilerWebSettings.UserIdProvider?.Invoke(context.Request);

            MiniProfiler.Settings.Storage.SetViewed(user, id);

            if (profiler == null)
            {
                return isPopup ? NotFound(context) : NotFound(context, "text/plain", "No MiniProfiler results found with Id=" + id.ToString());
            }

            bool needsSave = false;
            if (profiler.ClientTimings == null)
            {
                profiler.ClientTimings = context.Request.GetClientTimings();
                if (profiler.ClientTimings != null)
                {
                    needsSave = true;
                }
            }

            if (!profiler.HasUserViewed)
            {
                profiler.HasUserViewed = true;
                needsSave = true;
            }

            if (needsSave)
            {
                MiniProfiler.Settings.Storage.Save(profiler);
            }

            if (!AuthorizeRequest(context, isList: false, message: out string authorizeMessage))
            {
                context.Response.ContentType = "application/json";
                return @"""hidden"""; // JSON
            }

            return isPopup ? ResultsJson(context, profiler) : ResultsFullPage(context, profiler);
        }

        private static string ResultsJson(HttpContext context, MiniProfiler profiler)
        {
            context.Response.ContentType = "application/json";
            return profiler.ToJson();
        }

        private static string ResultsFullPage(HttpContext context, MiniProfiler profiler)
        {
            context.Response.ContentType = "text/html";
            return profiler.RenderResultsHtml(VirtualPathUtility.ToAbsolute(MiniProfiler.Settings.RouteBasePath).EnsureTrailingSlash());
        }

#if DEBUG
        private static bool BypassLocalLoad = false;
#endif

        private static bool TryGetResource(string filename, out string resource)
        {
            filename = filename.ToLower();

#if DEBUG
            // attempt to simply load from file system, this lets up modify js without needing to recompile A MILLION TIMES 
            if (!BypassLocalLoad)
            {
                var trace = new System.Diagnostics.StackTrace(true);
                var path = Path.GetDirectoryName(trace.GetFrames()[0].GetFileName()) + "\\ui\\" + filename;
                try
                {
                    resource = File.ReadAllText(path);
                    return true;
                }
                catch
                {
                    BypassLocalLoad = true;
                }
            }
#endif

            if (!ResourceCache.TryGetValue(filename, out resource))
            {
                string customTemplatesPath = HttpContext.Current.Server.MapPath(MiniProfilerWebSettings.CustomUITemplates);
                string customTemplateFile = Path.Combine(customTemplatesPath, filename);

                if (File.Exists(customTemplateFile))
                {
                    resource = File.ReadAllText(customTemplateFile);
                }
                else
                {
                    using (var stream = typeof(MiniProfiler).Assembly.GetManifestResourceStream("StackExchange.Profiling.ui." + filename))
                    {
                        if (stream == null)
                        {
                            return false;
                        }
                        using (var reader = new StreamReader(stream))
                        {
                            resource = reader.ReadToEnd();
                        }
                    }
                }

                ResourceCache[filename] = resource;
            }

            return true;
        }

        private static string NotFound(HttpContext context, string contentType = "text/plain", string message = null)
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = contentType;

            return message;
        }
    }
}
