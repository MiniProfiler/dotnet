using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Routing;

using StackExchange.Profiling.Helpers;
using System.Text;

namespace StackExchange.Profiling.UI
{
    /// <summary>
    /// Understands how to route and respond to MiniProfiler UI urls.
    /// </summary>
    public class MiniProfilerHandler : IRouteHandler, IHttpHandler
    {
        internal static HtmlString RenderIncludes(MiniProfiler profiler, RenderPosition? position = null, bool? showTrivial = null, bool? showTimeWithChildren = null, int? maxTracesToShow = null, bool xhtml = false, bool? showControls = null)
        {
            const string format =
@"<script type=""text/javascript"">    
    (function(){{
        var init = function() {{        
                var load = function(s,f){{
                    var sc = document.createElement(""script"");
                    sc.async = ""async"";
                    sc.type = ""text/javascript"";
                    sc.src = s;
                    sc.onload = sc.onreadystatechange  = function(_, abort) {{
                        if (!sc.readyState || /loaded|complete/.test(sc.readyState)) {{
                            if (!abort) f();
                        }}
                    }};

                    document.getElementsByTagName('head')[0].appendChild(sc);
                }};                
                
                var initMp = function(){{
                    load(""{path}mini-profiler-includes.js?v={version}"",function(){{
                        MiniProfiler.init({{
                            ids: {ids},
                            path: '{path}',
                            version: '{version}',
                            renderPosition: '{position}',
                            showTrivial: {showTrivial},
                            showChildrenTime: {showChildren},
                            maxTracesToShow: {maxTracesToShow},
                            showControls: {showControls},
                            currentId: '{currentId}'
                        }});
                    }});
                }};

                if (!window.jQuery) {{
                    load('{path}mini-profiler-jquery.1.6.2.js', initMp);
                }} else {{
                    initMp();
                }}
        }};

        var w = 0;        
        var deferInit = function(){{ 
            if (window.performance && window.performance.timing && window.performance.timing.loadEventEnd == 0 && w < 10000){{
                setTimeout(deferInit, 100);
                w += 100;
            }} else {{
                init();
            }}
        }};
        deferInit(); 
    }})();
</script>";

            var result = "";

            if (profiler != null)
            {
                // HACK: unviewed ids are added to this list during Storage.Save, but we know we haven't see the current one yet,
                // so go ahead and add it to the end - it's usually the only id, but if there was a redirect somewhere, it'll be there, too
                MiniProfiler.Settings.EnsureStorageStrategy();
                var ids = MiniProfiler.Settings.Storage.GetUnviewedIds(profiler.User);
                ids.Add(profiler.Id);

                result = format.Format(new
                {
                    path = VirtualPathUtility.ToAbsolute(MiniProfiler.Settings.RouteBasePath).EnsureTrailingSlash(),
                    version = MiniProfiler.Settings.Version,
                    ids = ids.ToJson(),
                    position = (position ?? MiniProfiler.Settings.PopupRenderPosition).ToString().ToLower(),
                    showTrivial = showTrivial ?? MiniProfiler.Settings.PopupShowTrivial ? "true" : "false",
                    showChildren = showTimeWithChildren ?? MiniProfiler.Settings.PopupShowTimeWithChildren ? "true" : "false",
                    maxTracesToShow = maxTracesToShow ?? MiniProfiler.Settings.PopupMaxTracesToShow,
                    closeXHTML = xhtml ? "/" : "",
                    showControls = showControls ?? MiniProfiler.Settings.ShowControls ? "true" : "false",
                    currentId = profiler.Id
                });
            }

            return new HtmlString(result);
        }

        internal static void RegisterRoutes()
        {
            var urls = new[] 
            { 
                "mini-profiler-jquery.1.6.2.js",
                "mini-profiler-jquery.tmpl.beta1.js",
                "mini-profiler-includes.js", 
                "mini-profiler-includes.css", 
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
                case "mini-profiler-jquery.1.6.2":
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
        /// Handles rendering static content files.
        /// </summary>
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
            var isPopup = !string.IsNullOrWhiteSpace(context.Request["popup"]);

            // this guid is the MiniProfiler.Id property
            Guid id;
            if (!Guid.TryParse(context.Request["id"], out id))
                return isPopup ? NotFound(context) : NotFound(context, "text/plain", "No Guid id specified on the query string");

            MiniProfiler.Settings.EnsureStorageStrategy();
            var profiler = MiniProfiler.Settings.Storage.Load(id);

            var provider = WebRequestProfilerProvider.Settings.UserProvider;
            string user = null;
            if (provider != null)
            {
                user = provider.GetUser(context.Request);
            }

            MiniProfiler.Settings.Storage.SetViewed(user, id);

            if (profiler == null)
            {
                return isPopup ? NotFound(context) : NotFound(context, "text/plain", "No MiniProfiler results found with Id=" + id.ToString());
            }

            bool needsSave = false;
            if (profiler.ClientTimings == null)
            {
                profiler.ClientTimings = ClientTimings.FromRequest(context.Request);
                if (profiler.ClientTimings != null)
                {
                    needsSave = true;
                }
            }

            if (profiler.HasUserViewed == false) 
            {
                profiler.HasUserViewed = true;
                needsSave = true;
            }

            if (needsSave) MiniProfiler.Settings.Storage.Save(profiler);

            // ensure that callers have access to these results
            var authorize = MiniProfiler.Settings.Results_Authorize;
            if (authorize != null && !authorize(context.Request, profiler))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "text/plain";
                return "Unauthorized";
            }

            return isPopup ? ResultsJson(context, profiler) : ResultsFullPage(context, profiler);
        }

        private static string ResultsJson(HttpContext context, MiniProfiler profiler)
        {
            context.Response.ContentType = "application/json";
            return MiniProfiler.ToJson(profiler);
        }

        private static string ResultsFullPage(HttpContext context, MiniProfiler profiler)
        {
            context.Response.ContentType = "text/html";
            return new StringBuilder()
                .AppendLine("<html><head>")
                .AppendFormat("<title>{0} ({1} ms) - StackExchange.Profiling Results</title>", profiler.Name, profiler.DurationMilliseconds)
                .AppendLine()
                .AppendLine("<script type='text/javascript' src='https://ajax.googleapis.com/ajax/libs/jquery/1.6.2/jquery.min.js'></script>")
                .Append("<script type='text/javascript'> var profiler = ")
                .Append(MiniProfiler.ToJson(profiler))
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
                using (var stream = typeof(MiniProfilerHandler).Assembly.GetManifestResourceStream("StackExchange.Profiling.UI." + filename))
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
