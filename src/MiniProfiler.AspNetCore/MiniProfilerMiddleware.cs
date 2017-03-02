using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using StackExchange.Profiling.Helpers;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Represents a middleware that starts and stops a MiniProfiler
    /// </summary>
    public class MiniProfilerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostingEnvironment _env;

        internal readonly PathString BasePath;
        internal readonly MiniProfilerOptions Options;
        internal readonly EmbeddedProvider Embedded;
        internal static MiniProfilerMiddleware Current;

        /// <summary>
        /// Creates a new instance of <see cref="MiniProfilerMiddleware"/>
        /// </summary>
        /// <param name="next">The delegate representing the next middleware in the request pipeline.</param>
        /// <param name="hostingEnvironment">The Hosting Environment.</param>
        /// <param name="options">The middleware options, containing the rules to apply.</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="next"/>, <paramref name="hostingEnvironment"/>, or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Throws when <see cref="MiniProfilerOptions.RouteBasePath"/> is <c>null</c> or empty.</exception>
        public MiniProfilerMiddleware(
            RequestDelegate next,
            IHostingEnvironment hostingEnvironment,
            MiniProfilerOptions options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _env = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            Options = options ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrEmpty(Options.RouteBasePath))
            {
                throw new ArgumentException("BasePath cannot be empty", nameof(Options.RouteBasePath));
            }

            var basePath = Options.RouteBasePath;
            // Example transform: ~/mini-profiler-results/ to /mini-profiler-results
            if (basePath.StartsWith("~/")) basePath = basePath.Substring(1);
            if (basePath.EndsWith("/") && basePath.Length > 2) basePath = basePath.Substring(0, basePath.Length - 1);

            BasePath = new PathString(basePath);
            Embedded = new EmbeddedProvider(Options, _env);
            // A static reference back to this middleware for property access.
            // Which is probably a crime against humanity in ways I'm ignorant of.
            Current = this;
        }

        /// <summary>
        /// Executes the MiniProfiler-wrapped middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
        /// <returns>A task that represents the execution of the MiniProfiler-wrapped middleware.</returns>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="context"/> is <c>null</c>.</exception>
        public async Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Request.Path.StartsWithSegments(BasePath, out PathString subPath))
            {
                // This is a request in the MiniProfiler path (e.g. one of "our" routes), HANDLE THE SITUATION.
                await HandleRequest(context, subPath).ConfigureAwait(false);
                return;
            }

            // Otherwise this is an app request, profile it!
            if (Options.ShouldProfile?.Invoke(context.Request) ?? true)
            {
                // Wrap the request in this profiler
                var mp = MiniProfiler.Start();
                // Always add this profiler's header (and any async requests before it)
                await SetHeadersAndState(context, mp).ConfigureAwait(false);
                // Execute the pipe
#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.
                await _next(context);
#pragma warning restore RCS1090 // Call 'ConfigureAwait(false)'.
                // Stop (and record)
                await MiniProfiler.StopAsync().ConfigureAwait(false);
            }
            else
            {
                // Don't profile, only relay
#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.
                await _next(context);
#pragma warning restore RCS1090 // Call 'ConfigureAwait(false)'.
            }
        }

        private async Task SetHeadersAndState(HttpContext context, MiniProfiler current)
        {
            try
            {
                // Are we authorized???
                var isAuthroized = Options.ResultsAuthorize?.Invoke(context.Request) ?? true;

                // Grab any past profilers (e.g. from a previous redirect)
                var profilerIds = (isAuthroized ? await MiniProfiler.Settings.Storage.GetUnviewedIdsAsync(current.User).ConfigureAwait(false) : null)
                                 ?? new List<Guid>(1);

                // Always add the current
                profilerIds.Add(current.Id);

                // Cap us down to MaxUnviewedProfiles
                if (profilerIds.Count > MiniProfiler.Settings.MaxUnviewedProfiles)
                {
                    foreach (var id in profilerIds.Take(profilerIds.Count - MiniProfiler.Settings.MaxUnviewedProfiles))
                    {
                        await MiniProfiler.Settings.Storage.SetViewedAsync(current.User, id).ConfigureAwait(false);
                    }
                }

                if (profilerIds.Count > 0)
                {
                    context.Response.Headers.Add("X-MiniProfiler-Ids", profilerIds.ToJson());
                }

                // Set the state to use in RenderIncludes() down the pipe later
                current.RequestState = new RequestState { IsAuthroized = isAuthroized, RequestIDs = profilerIds };
            }
            catch { /* oh no! headers blew up */ }
        }

        private async Task HandleRequest(HttpContext context, PathString subPath)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            string result = null;

            // File embed
            if (subPath.Value.StartsWith("/includes"))
            {
                result = Embedded.GetFile(context, subPath);
            }

            switch (subPath.Value)
            {
                case "/results-index":
                    result = ResultsIndex(context);
                    break;

                case "/results-list":
                    result = ResultsList(context);
                    break;

                case "/results":
                    result = GetSingleProfilerResult(context);
                    break;
            }

            result = result ?? NotFound(context);
            context.Response.ContentLength = result.Length;

            await context.Response.WriteAsync(result).ConfigureAwait(false);
        }

        private static string NotFound(HttpContext context, string contentType = "text/plain", string message = null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = contentType;

            return message;
        }

        /// <summary>
        /// Returns true if the current request is allowed to see the profiler response.
        /// </summary>
        /// <param name="context">The context to attempt to authroize a user for.</param>
        /// <param name="isList">Whether this is a list route being accessed.</param>
        /// <param name="message">The access denied message, if present.</param>
        private bool AuthorizeRequest(HttpContext context, bool isList, out string message)
        {
            message = null;
            var authorize = Options.ResultsAuthorize;
            var authorizeList = Options.ResultsListAuthorize;

            if ((authorize != null && !authorize(context.Request)) || (isList && (authorizeList != null && !authorizeList(context.Request))))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "text/plain";
                message = "unauthorized";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the list of profiling sessions
        /// </summary>
        /// <param name="context">The results list HTML, if authorized.</param>
        private string ResultsIndex(HttpContext context)
        {
            if (!AuthorizeRequest(context, isList: true, message: out string message))
            {
                return message;
            }

            context.Response.ContentType = "text/html";

            var path = BasePath.Value.EnsureTrailingSlash();
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
        /// Returns the JSON needed for the results list in MiniProfiler
        /// </summary>
        /// <param name="context">The context to get the results list for.</param>
        private string ResultsList(HttpContext context)
        {
            if (!AuthorizeRequest(context, isList: true, message: out string message))
            {
                return message;
            }

            var guids = MiniProfiler.Settings.Storage.List(100);

            if (context.Request.Query.TryGetValue("last-id", out var lastId) && Guid.TryParse(lastId, out var lastGuid))
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
        /// Returns either json or full page html of a previous <c>MiniProfiler</c> session, 
        /// identified by its <c>"?id=GUID"</c> on the query.
        /// </summary>
        /// <param name="context">The context to get a profiler response for.</param>
        private string GetSingleProfilerResult(HttpContext context)
        {
            var isPost = context.Request.HasFormContentType;
            // when we're rendering as a button/popup in the corner, we'll pass ?popup=1
            // if it's absent, we're rendering results as a full page for sharing
            var isPopup = isPost && context.Request.Form["popup"].FirstOrDefault() == "1";
            // this guid is the MiniProfiler.Id property
            // if this guid is not supplied, the last set of results needs to be
            // displayed. The home page doesn't have profiling otherwise.
            var requestId = isPost
                ? context.Request.Form["id"]
                : context.Request.Query["id"];

            if (!Guid.TryParse(requestId, out var id) && MiniProfiler.Settings.Storage != null)
            {
                id = MiniProfiler.Settings.Storage.List(1).FirstOrDefault();
            }

            if (id == default(Guid))
            {
                return isPopup ? NotFound(context) : NotFound(context, "text/plain", "No Guid id specified on the query string");
            }

            var profiler = MiniProfiler.Settings.Storage.Load(id);
            string user = Options.UserIdProvider?.Invoke(context.Request);

            MiniProfiler.Settings.Storage.SetViewed(user, id);

            if (profiler == null)
            {
                return isPopup ? NotFound(context) : NotFound(context, "text/plain", "No MiniProfiler results found with Id=" + id.ToString());
            }

            bool needsSave = false;
            if (profiler.ClientTimings == null && isPost)
            {
                var form = context.Request.Form;
                var dict = new Dictionary<string, string>();
                foreach (var k in form.Keys)
                {
                    dict.Add(k, form[k]);
                }
                profiler.ClientTimings = ClientTimings.FromForm(dict);

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
                return "\"hidden\""; // JSON
            }

            return isPopup ? ResultsJson(context, profiler) : ResultsFullPage(context, profiler);
        }

        private string ResultsJson(HttpContext context, MiniProfiler profiler)
        {
            context.Response.ContentType = "application/json";
            return MiniProfiler.ToJson(profiler);
        }

        private string ResultsFullPage(HttpContext context, MiniProfiler profiler)
        {
            context.Response.ContentType = "text/html";
            if (!Embedded.TryGetResource("share.html", out string template))
                return NotFound(context);
            var sb = new StringBuilder(template);
            sb.Replace("{name}", profiler.Name)
              .Replace("{duration}", profiler.DurationMilliseconds.ToString(CultureInfo.InvariantCulture))
              .Replace("{path}", BasePath.Value.EnsureTrailingSlash())
              .Replace("{json}", MiniProfiler.ToJson(profiler))
              .Replace("{includes}", profiler.RenderIncludes().ToString())
              .Replace("{version}", MiniProfiler.Settings.VersionHash);
            return sb.ToString();
        }
    }
}