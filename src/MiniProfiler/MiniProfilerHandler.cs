using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
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
        private readonly ConcurrentDictionary<string, string> ResourceCache = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Gets a value indicating whether to keep things static and reusable.
        /// </summary>
        public bool IsReusable => true;

        /// <summary>
        /// The options this handler was created with.
        /// </summary>
        public MiniProfilerOptions Options { get; }

        /// <summary>
        /// Usually called internally, sometimes you may clear the routes during the apps lifecycle, 
        /// if you do that call this to bring back mini profiler.
        /// </summary>
        /// <param name="options">The options to configure this handler with.</param>
        public MiniProfilerHandler(MiniProfilerOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// Creates a MiniProfilerHandler and registers routes for it.
        /// </summary>
        /// <param name="options">The options to configure the handler with.</param>
        /// <returns>The configured and registered handler.</returns>
        public static MiniProfilerHandler Configure(MiniProfilerOptions options)
        {
            var handler = new MiniProfilerHandler(options);
            handler.RegisterRoutes();
            return handler;
        }

        /// <summary>
        /// Registers the routes for this handler to handle.
        /// </summary>
        public void RegisterRoutes()
        {
            var prefix = Options.RouteBasePath.Replace("~/", string.Empty).EnsureTrailingSlash();

            using (RouteTable.Routes.GetWriteLock())
            {
                var route = new Route(prefix + "{filename}", this)
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

            if (Options.EnableCompression && output.HasValue())
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
        private string Includes(HttpContext context, string path)
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
                default:
                    return NotFound(context);
            }

            return TryGetResource(Path.GetFileName(path), out string resource) ? resource : NotFound(context);
        }

        private string ResultsIndex(HttpContext context)
        {
            if (!AuthorizeRequest(context, isList: true, message: out string message))
            {
                return message;
            }

            context.Response.ContentType = "text/html";

            var path = VirtualPathUtility.ToAbsolute(Options.RouteBasePath).EnsureTrailingSlash();
            var version = Options.VersionHash;
            return $@"<html>
  <head>
    <title>List of profiling sessions</title>
    <script id=""mini-profiler"" data-ids="""" src=""{path}includes.js?v={version}""></script>
    <link href=""{path}includes.css?v={version}"" rel=""stylesheet"" />
    <script>MiniProfiler.listInit({{path: '{path}', version: '{version}'}});</script>
  </head>
  <body>
    <table class=""profiler-results-index"">
      <thead>
        <tr>
          <th>Name</th>
          <th>Server</th>
          <th>Started</th>
          <th>Total Duration</th>
          <th>Request Start</th>
          <th>Response Start</th>
          <th>Dom Complete</th>
        </tr>
      </thead>
      <tbody></tbody>
    </table>
  </body>
</html>";
        }

        /// <summary>
        /// Returns true if the current request is allowed to see the profiler response.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context for the request being authorixed.</param>
        /// <param name="isList">Whether this is a list route being accessed.</param>
        /// <param name="message">The access denied message, if present.</param>
        private bool AuthorizeRequest(HttpContext context, bool isList, out string message)
        {
            message = null;
            var authorize = Options.ResultsAuthorize;
            var authorizeList = Options.ResultsListAuthorize;

            if ((authorize?.Invoke(context.Request) == false) || (isList && (authorizeList?.Invoke(context.Request) == false)))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "text/plain";
                message = "unauthorized";
                return false;
            }

            return true;
        }

        private string ResultsList(HttpContext context)
        {
            if (!AuthorizeRequest(context, isList: true, message: out string message))
            {
                return message;
            }
            var guids = Options.Storage.List(100);
            var lastId = context.Request["last-id"];

            if (!lastId.IsNullOrWhiteSpace() && Guid.TryParse(lastId, out var lastGuid))
            {
                guids = guids.TakeWhile(g => g != lastGuid);
            }

            return guids.Reverse()
                        .Select(g => Options.Storage.Load(g))
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
        private string GetSingleProfilerResult(HttpContext context)
        {
            Guid id;
            ResultRequest clientRequest = null;
            // When we're rendering as a button/popup in the corner, it's an AJAX/JSON request.
            // If that's absent, we're rendering results as a full page for sharing.
            var jsonRequest = context.Request.Headers["Accept"]?.Contains("application/json") == true;

            // Try to parse from the JSON payload first
            if (jsonRequest
                && context.Request.ContentLength > 0
                && ResultRequest.TryParse(context.Request.InputStream, out clientRequest)
                && clientRequest.Id.HasValue)
            {
                id = clientRequest.Id.Value;
            }
            else if (Guid.TryParse(context.Request["id"], out id))
            {
                // We got the guid from the querystring
            }
            else if (Options.StopwatchProvider != null)
            {
                // Fall back to the last result
                id = Options.Storage.List(1).FirstOrDefault();
            }

            if (id == default(Guid))
                return jsonRequest ? NotFound(context) : NotFound(context, "text/plain", "No Guid id specified on the query string");

            var profiler = Options.Storage.Load(id);
            string user = Options.UserIdProvider?.Invoke(context.Request);

            Options.Storage.SetViewed(user, id);

            if (profiler == null)
            {
                return jsonRequest ? NotFound(context) : NotFound(context, "text/plain", "No MiniProfiler results found with Id=" + id.ToString());
            }

            bool needsSave = false;
            if (profiler.ClientTimings == null && clientRequest?.TimingCount > 0)
            {
                profiler.ClientTimings = ClientTimings.FromRequest(clientRequest);
                needsSave = true;
            }

            if (!profiler.HasUserViewed)
            {
                profiler.HasUserViewed = true;
                needsSave = true;
            }

            if (needsSave)
            {
                Options.Storage.Save(profiler);
            }

            if (!AuthorizeRequest(context, isList: false, message: out string authorizeMessage))
            {
                context.Response.ContentType = "application/json";
                return @"""hidden"""; // JSON
            }

            return jsonRequest ? ResultsJson(context, profiler) : ResultsFullPage(context, profiler);
        }

        private static string ResultsJson(HttpContext context, MiniProfiler profiler)
        {
            context.Response.ContentType = "application/json";
            return profiler.ToJson();
        }

        private string ResultsFullPage(HttpContext context, MiniProfiler profiler)
        {
            context.Response.ContentType = "text/html";
            return profiler.RenderResultsHtml(VirtualPathUtility.ToAbsolute(Options.RouteBasePath).EnsureTrailingSlash());
        }

#if DEBUG
        private static bool BypassLocalLoad = false;
#endif

        private bool TryGetResource(string filename, out string resource)
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
                string customTemplatesPath = HttpContext.Current.Server.MapPath(Options.CustomUITemplates);
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
