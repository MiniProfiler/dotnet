using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Routing;

using MvcMiniProfiler.Helpers;
using System.Text;

namespace MvcMiniProfiler.UI
{
    /// <summary>
    /// Understands how to route and respond to MiniProfiler UI urls.
    /// </summary>
    public class MiniProfilerHandler : IRouteHandler, IHttpHandler
    {
        internal static HtmlString RenderIncludes(MiniProfiler profiler, RenderPosition? position = null, bool? showTrivial = null, bool? showTimeWithChildren = null, int? maxTracesToShow = null)
        {
            const string format =
@"<link rel=""stylesheet/less"" type=""text/css"" href=""{path}mini-profiler-includes.less?v={version}"">
<script type=""text/javascript"" src=""{path}mini-profiler-yepnope.1.0.1.js""></script>
<script type=""text/javascript"">
    yepnope([
        {{ test: window.jQuery, nope: '{path}mini-profiler-jquery.1.6.1.js' }},
        {{ test: window.jQuery && window.jQuery.tmpl, nope: '{path}mini-profiler-jquery.tmpl.beta1.js' }},
        {{ load: '{path}mini-profiler-includes.js?v={version}',
           complete: function() {{
               jQuery(function() {{
                   MiniProfiler.init({{
                       id: '{id}',
                       path: '{path}',
                       version: '{version}',
                       renderPosition: '{position}',
                       showTrivial: {showTrivial},
                       showChildrenTime: {showChildren},
                       maxTracesToShow: {maxTracesToShow}
                   }});
               }});
         }}
    }}]);
</script>";
            var result = "";

            if (profiler != null)
            {
                result = format.Format(new
                {
                    path = VirtualPathUtility.ToAbsolute(MiniProfiler.Settings.RouteBasePath).EnsureTrailingSlash(),
                    version = MiniProfiler.Settings.Version,
                    id = profiler.Id,
                    position = (position ?? MiniProfiler.Settings.PopupRenderPosition).ToString().ToLower(),
                    showTrivial = showTrivial ?? MiniProfiler.Settings.PopupShowTrivial ? "true" : "false",
                    showChildren = showTimeWithChildren ?? MiniProfiler.Settings.PopupShowTimeWithChildren ? "true" : "false",
                    maxTracesToShow = maxTracesToShow ?? MiniProfiler.Settings.PopupMaxTracesToShow
                });
            }

            return new HtmlString(result);
        }

        internal static void RegisterRoutes()
        {
            var urls = new[] 
            { 
                "mini-profiler-yepnope.1.0.1.js", 
                "mini-profiler-jquery.1.6.1.js",
                "mini-profiler-jquery.tmpl.beta1.js",
                "mini-profiler-includes.js", 
                "mini-profiler-includes.less", 
                "mini-profiler-includes.tmpl", 
                "mini-profiler-results" 
            };
            var routes = RouteTable.Routes;
            var handler = new MiniProfilerHandler();
            var prefix = (MiniProfiler.Settings.RouteBasePath ?? "").Replace("~/", "").EnsureTrailingSlash();

            using (routes.GetWriteLock())
            {
                foreach (var url in urls)
                {
                    var route = new Route(prefix + url, handler)
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
            string path = context.Request.AppRelativeCurrentExecutionFilePath;

            switch (Path.GetFileNameWithoutExtension(path))
            {
                case "mini-profiler-yepnope.1.0.1":
                case "mini-profiler-jquery.1.6.1":
                case "mini-profiler-jquery.tmpl.beta1":
                case "mini-profiler-includes":
                    output = Includes(context, path);
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
        private static string Includes(HttpContext context, string path)
        {
            var response = context.Response;

            switch (Path.GetExtension(path))
            {
                case ".js":
                    response.ContentType = "application/javascript";
                    break;
                case ".less":
                    response.ContentType = "text/plain";
                    break;
                case ".tmpl":
                    response.ContentType = "text/x-jquery-tmpl";
                    break;
                default:
                    return NotFound(context);
            }

            var cache = response.Cache;
            cache.SetCacheability(System.Web.HttpCacheability.Public);
            cache.SetExpires(DateTime.Now.AddDays(7));
            cache.SetValidUntilExpires(true);

            var embeddedFile = Path.GetFileName(path).Replace("mini-profiler-", "");
            return GetResource(embeddedFile);
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

            MiniProfiler.Settings.EnsureStorageStrategies();
            var profiler = MiniProfiler.Settings.ShortTermStorage.LoadMiniProfiler(id);

            if (profiler == null)
                profiler = MiniProfiler.Settings.LongTermStorage.LoadMiniProfiler(id);

            if (profiler == null)
                return isPopup ? NotFound(context) : NotFound(context, "text/html", "No MiniProfiler results found with Id=" + id.ToString());

            if (isPopup)
            {
                return ResultsJson(context, profiler);
            }
            else
            {
                // the first time we hit this route as a full results page, the prof won't be in long term cache, so put it there for sharing
                // each subsequent time the full page is hit, just save again, so we act as a sliding expiration
                MiniProfiler.Settings.LongTermStorage.SaveMiniProfiler(profiler.Id, profiler);
                return ResultsFullPage(context, profiler);
            }
        }

        private static string ResultsJson(HttpContext context, MiniProfiler profiler)
        {
            context.Response.ContentType = "application/json";
            return MiniProfiler.ToFormattedSqlJson(profiler);
        }

        private static string ResultsFullPage(HttpContext context, MiniProfiler profiler)
        {
            context.Response.ContentType = "text/html";
            return new StringBuilder()
                .AppendLine("<html><head>")
                .AppendFormat("<title>{0} ({1} ms) - MvcMiniProfiler Results</title>", profiler.Name, profiler.DurationMilliseconds)
                .AppendLine()
                .AppendLine("<script type='text/javascript' src='https://ajax.googleapis.com/ajax/libs/jquery/1.6.1/jquery.min.js'></script>")
                .Append("<script type='text/javascript'> var profiler = ")
                .Append(MiniProfiler.ToFormattedSqlJson(profiler))
                .AppendLine(";</script>")
                .Append(RenderIncludes(profiler)) // figure out how to better pass display options
                .AppendLine("</head><body><div class='profiler-result-full'></div></body></html>")
                .ToString();
        }

        private static string GetResource(string filename)
        {
            filename = filename.ToLower();
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
