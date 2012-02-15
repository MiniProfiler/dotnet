using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Routing;
using System.Linq;

using StackExchange.Profiling.Helpers;
using System.Text;

namespace StackExchange.Profiling.UI
{
    /// <summary>
    /// Understands how to route and respond to MiniProfiler UI urls.
    /// </summary>
    public class MiniProfilerHandler : IRouteHandler, IHttpHandler
    {
        internal static HtmlString RenderIncludes(MiniProfiler profiler, RenderPosition? position = null, bool? showTrivial = null, bool? showTimeWithChildren = null, int? maxTracesToShow = null, bool? showControls = null)
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
                    var l = false;
                    sc.onload = sc.onreadystatechange  = function(_, abort) {{
                        if (!l && (!sc.readyState || /loaded|complete/.test(sc.readyState))) {{
                            if (!abort){{l=true; f();}}
                        }}
                    }};

                    document.getElementsByTagName('head')[0].appendChild(sc);
                }};                
                
                var initMp = function(){{
                    load(""{path}includes.js?v={version}"",function(){{
                        MiniProfiler.init({{
                            ids: {ids},
                            path: '{path}',
                            version: '{version}',
                            renderPosition: '{position}',
                            showTrivial: {showTrivial},
                            showChildrenTime: {showChildren},
                            maxTracesToShow: {maxTracesToShow},
                            showControls: {showControls},
                            currentId: '{currentId}',
                            authorized: {authorized}
                        }});
                    }});
                }};

                 load('{path}jquery.1.6.2.js?v={version}', initMp);
                
        }};

        var w = 0;        
        var f = false;
        var deferInit = function(){{ 
            if (f) return;
            if (window.performance && window.performance.timing && window.performance.timing.loadEventEnd == 0 && w < 10000){{
                setTimeout(deferInit, 100);
                w += 100;
            }} else {{
                f = true;
                init();
            }}
        }};
        if (document.addEventListener) {{
            document.addEventListener('DOMContentLoaded',deferInit);
        }}
        var o = window.onload;
        window.onload = function(){{if(o)o; deferInit()}};
    }})();
</script>";

            var result = "";

            if (profiler != null)
            {
                // HACK: unviewed ids are added to this list during Storage.Save, but we know we haven't see the current one yet,
                // so go ahead and add it to the end - it's usually the only id, but if there was a redirect somewhere, it'll be there, too
                MiniProfiler.Settings.EnsureStorageStrategy();
                
                var authorized = 
                    MiniProfiler.Settings.Results_Authorize == null || 
                    MiniProfiler.Settings.Results_Authorize(HttpContext.Current.Request);

                List<Guid> ids;
                if (authorized)
                {
                    ids = MiniProfiler.Settings.Storage.GetUnviewedIds(profiler.User);
                    ids.Add(profiler.Id);
                }
                else
                {
                    ids = new List<Guid> { profiler.Id };
                }

                result = format.Format(new
                {
                    path = VirtualPathUtility.ToAbsolute(MiniProfiler.Settings.RouteBasePath).EnsureTrailingSlash(),
                    version = MiniProfiler.Settings.Version,
                    ids = ids.ToJson(),
                    position = (position ?? MiniProfiler.Settings.PopupRenderPosition).ToString().ToLower(),
                    showTrivial = showTrivial ?? MiniProfiler.Settings.PopupShowTrivial ? "true" : "false",
                    showChildren = showTimeWithChildren ?? MiniProfiler.Settings.PopupShowTimeWithChildren ? "true" : "false",
                    maxTracesToShow = maxTracesToShow ?? MiniProfiler.Settings.PopupMaxTracesToShow,
                    showControls = showControls ?? MiniProfiler.Settings.ShowControls ? "true" : "false",
                    currentId = profiler.Id,
                    authorized = authorized ? "true" : "false"
                });
                
            }

            return new HtmlString(result);
        }

        /// <summary>
        /// Usually called internally, sometimes you may clear the routes during the apps lifecycle, if you do that call this to bring back mp
        /// </summary>
        public static void RegisterRoutes()
        {
           
            var routes = RouteTable.Routes;
            var handler = new MiniProfilerHandler();
            var prefix = MiniProfiler.Settings.RouteBasePath.Replace("~/", "").EnsureTrailingSlash();

            using (routes.GetWriteLock())
            {
                var route = new Route(prefix + "{filename}", handler)
                {
                    // we have to specify these, so no MVC route helpers will match, e.g. @Html.ActionLink("Home", "Index", "Home")
                    Defaults = new RouteValueDictionary( new { controller = "MiniProfilerHandler", action = "ProcessRequest" }),
                    Constraints = new RouteValueDictionary( new { controller = "MiniProfilerHandler", action = "ProcessRequest" })
                };

                // put our routes at the beginning, like a boss
                routes.Insert(0, route);   
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
                case "jquery.1.6.2":
                case "jquery.tmpl":
                case "includes":
                case "list":
                    output = Includes(context, path);
                    break;

                case "results-index":
                    output = Index(context);
                    break;
                
                case "results-list":
                    output = ResultList(context);
                    break;

                case "results":
                    output = Results(context);
                    break;

                default:
                    output = NotFound(context);
                    break;
            }

            context.Response.Write(output);
        }

        private static string ResultList(HttpContext context)
        {
            string message;
            if (!AuthorizeRequest(context, isList: true, message: out message))
            {
                return message;
            }

            var lastId = context.Request["last-id"];
            Guid lastGuid = Guid.Empty;

            if (!lastId.IsNullOrWhiteSpace()) {
                Guid.TryParse(lastId, out lastGuid);
            }
            
            var guids = MiniProfiler.Settings.Storage.List(100);

            if (lastGuid != Guid.Empty)
            {
                guids = guids.TakeWhile(g => g != lastGuid);
            }

            guids = guids.Reverse();

            return guids.Select(g => 
            {
                var profiler = MiniProfiler.Settings.Storage.Load(g);
                return new 
                {
                    profiler.Id, 
                    profiler.Name, 
                    profiler.DurationMilliseconds,
                    profiler.DurationMillisecondsInSql,
                    profiler.ClientTimings,
                    profiler.Started
                };
            }
            
           ).ToJson();
        }

        private static string Index(HttpContext context)
        {
            string message;
            if (!AuthorizeRequest(context, isList: true, message: out message))
            {
                return message;
            }

            context.Response.ContentType = "text/html";

            var path = VirtualPathUtility.ToAbsolute(MiniProfiler.Settings.RouteBasePath).EnsureTrailingSlash(); 

            return new StringBuilder()
                .AppendLine("<html><head>")
                .AppendFormat("<title>List of profiling sessions</title>")
                .AppendLine()
                .AppendLine("<script type='text/javascript' src='" + path + "jquery.1.6.2.js?v=" + MiniProfiler.Settings.Version + "'></script>")
                .AppendLine("<script type='text/javascript' src='" + path + "jquery.tmpl.js?v=" + MiniProfiler.Settings.Version + "'></script>")
                .AppendLine("<script type='text/javascript' src='" + path + "includes.js?v=" + MiniProfiler.Settings.Version + "'></script>")
                .AppendLine("<script type='text/javascript' src='" + path + "list.js?v=" + MiniProfiler.Settings.Version + "'></script>")
                .AppendLine("<link href='" + path +"list.css?v=" + MiniProfiler.Settings.Version +  "' rel='stylesheet' type='text/css'>")
                .AppendLine("<script type='text/javascript'>MiniProfiler.list.init({path: '" + path + "', version: '" + MiniProfiler.Settings.Version + "'})</script>")
                .AppendLine("</head><body></body></html>")
                .ToString();
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

#if !DEBUG
            var cache = response.Cache;
            cache.SetCacheability(System.Web.HttpCacheability.Public);
            cache.SetExpires(DateTime.Now.AddDays(7));
            cache.SetValidUntilExpires(true);
#endif
            

            var embeddedFile = Path.GetFileName(path);
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


            var authorize = MiniProfiler.Settings.Results_Authorize;


            if (authorize != null && !authorize(context.Request))
            {
                context.Response.ContentType = "application/json";
                return "hidden".ToJson();
            }

            return isPopup ? ResultsJson(context, profiler) : ResultsFullPage(context, profiler);
        }

        private static bool AuthorizeRequest(HttpContext context, bool isList, out string message)
        {
            message = null;
            var authorize = MiniProfiler.Settings.Results_Authorize;
            var authorizeList = MiniProfiler.Settings.Results_List_Authorize;

            if (authorize != null && !authorize(context.Request) || (isList && (authorizeList == null || !authorizeList(context.Request))))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "text/plain";
                message = "unauthorized";
                return false;
            }
            return true;
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

        private static bool bypassLocalLoad = false; 
        private static string GetResource(string filename)
        {
            filename = filename.ToLower();
            string result;

#if DEBUG 
            // attempt to simply load from file system, this lets up modify js without needing to recompile A MILLION TIMES 
            if (!bypassLocalLoad)
            {

                var trace = new System.Diagnostics.StackTrace(true);
                var path = System.IO.Path.GetDirectoryName(trace.GetFrames()[0].GetFileName()) + "\\..\\UI\\" + filename;
                try
                {
                    return File.ReadAllText(path);
                }
                catch 
                {
                    bypassLocalLoad = true;
                }
            }
            
#endif

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
