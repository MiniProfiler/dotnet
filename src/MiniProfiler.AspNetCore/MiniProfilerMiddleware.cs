using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using StackExchange.Profiling.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Represents a middleware that starts and stops a MiniProfiler
    /// </summary>
    public class MiniProfilerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostingEnvironment _env;
        private readonly IOptions<MiniProfilerOptions> _options;
        internal MiniProfilerOptions Options => _options.Value;

        internal readonly PathString BasePath;
        internal readonly PathString MatchPath;
        internal readonly EmbeddedProvider Embedded;
        internal static MiniProfilerMiddleware Current;

        /// <summary>
        /// Creates a new instance of <see cref="MiniProfilerMiddleware"/>
        /// </summary>
        /// <param name="next">The delegate representing the next middleware in the request pipeline.</param>
        /// <param name="hostingEnvironment">The Hosting Environment.</param>
        /// <param name="options">The middleware options, containing the rules to apply.</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="next"/>, <paramref name="hostingEnvironment"/>, or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Throws when <see cref="MiniProfilerBaseOptions.RouteBasePath"/> is <c>null</c> or empty.</exception>
        public MiniProfilerMiddleware(
            RequestDelegate next,
            IHostingEnvironment hostingEnvironment,
            IOptions<MiniProfilerOptions> options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _env = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _ = options.Value ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrEmpty(Options.RouteBasePath))
            {
                throw new ArgumentException("BasePath cannot be empty", nameof(Options.RouteBasePath));
            }

            var basePath = Options.RouteBasePath;
            // Example transform: ~/mini-profiler-results/ to /mini-profiler-results
            if (basePath.StartsWith("~/", StringComparison.Ordinal)) basePath = basePath.Substring(1);
            if (basePath.EndsWith("/", StringComparison.Ordinal) && basePath.Length > 2) basePath = basePath.Substring(0, basePath.Length - 1);

            MatchPath = basePath;
            BasePath = basePath.EnsureTrailingSlash();
            Embedded = new EmbeddedProvider(_options, _env);
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

            if (context.Request.Path.StartsWithSegments(MatchPath, out PathString subPath))
            {
                // This is a request in the MiniProfiler path (e.g. one of "our" routes), HANDLE THE SITUATION.
                await HandleRequest(context, subPath).ConfigureAwait(false);
                return;
            }

            // Otherwise this is an app request, profile it!
            if (Options.ShouldProfile?.Invoke(context.Request) ?? true)
            {
                // Wrap the request in this profiler
                var mp = Options.StartProfiler();
                // Always add this profiler's header (and any async requests before it)
                using (mp.Step("MiniProfiler Prep"))
                {
                    await SetHeadersAndState(context, mp).ConfigureAwait(false);
                }
                // Execute the pipe
#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.
                await _next(context);
#pragma warning restore RCS1090 // Call 'ConfigureAwait(false)'.
                // Stop (and record)
                await mp.StopAsync().ConfigureAwait(false);
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
                var isAuthorized = Options.ResultsAuthorize?.Invoke(context.Request) ?? true;

                // Grab any past profilers (e.g. from a previous redirect)
                var profilerIds = (isAuthorized ? await Options.ExpireAndGetUnviewedAsync(current.User).ConfigureAwait(false) : null)
                                 ?? new List<Guid>(1);

                // Always add the current
                profilerIds.Add(current.Id);

                if (profilerIds.Count > 0)
                {
                    context.Response.Headers.Add("X-MiniProfiler-Ids", profilerIds.ToJson());
                }

                // Set the state to use in RenderIncludes() down the pipe later
                new RequestState { IsAuthorized = isAuthorized, RequestIDs = profilerIds }.Store(context);
            }
            catch { /* oh no! headers blew up */ }
        }

        private async Task HandleRequest(HttpContext context, PathString subPath)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            string result = null;

            // File embed
            if (subPath.Value.StartsWith("/includes", StringComparison.Ordinal))
            {
                result = Embedded.GetFile(context, subPath);
            }

            switch (subPath.Value)
            {
                case "/results-index":
                    result = ResultsIndex(context);
                    break;

                case "/results-list":
                    result = await ResultsListAsync(context).ConfigureAwait(false);
                    break;

                case "/results":
                    result = await GetSingleProfilerResultAsync(context).ConfigureAwait(false);
                    break;
            }

            result = result ?? NotFound(context, "Not Found: " + subPath);
            context.Response.ContentLength = result != null ? Encoding.UTF8.GetByteCount(result) : 0;

            await context.Response.WriteAsync(result).ConfigureAwait(false);
        }

        private static string NotFound(HttpContext context, string message = null, string contentType = "text/plain")
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
            var version = Options.VersionHash;
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
        private async Task<string> ResultsListAsync(HttpContext context)
        {
            if (!AuthorizeRequest(context, isList: true, message: out string message))
            {
                return message;
            }

            var guids = await Options.Storage.ListAsync(100).ConfigureAwait(false);

            if (context.Request.Query.TryGetValue("last-id", out var lastId) && Guid.TryParse(lastId, out var lastGuid))
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
        /// Returns either json or full page html of a previous <c>MiniProfiler</c> session, 
        /// identified by its <c>"?id=GUID"</c> on the query.
        /// </summary>
        /// <param name="context">The context to get a profiler response for.</param>
        private async Task<string> GetSingleProfilerResultAsync(HttpContext context)
        {
            bool jsonRequest = false;
            IFormCollection form = null;

            // When we're rendering as a button/popup in the corner, we'll pass { popup: 1 } from jQuery
            // If it's absent, we're rendering results as a full page for sharing.
            if (context.Request.HasFormContentType)
            {
                form = await context.Request.ReadFormAsync().ConfigureAwait(false);
                // TODO: Get rid of popup and switch to application/json Accept header detection
                jsonRequest = form["popup"] == "1";
            }

            // This guid is the MiniProfiler.Id property. If a guid is not supplied, 
            // the last set of results needs to be displayed.
            string requestId = form?["id"] ?? context.Request.Query["id"];

            if (!Guid.TryParse(requestId, out var id) && Options.Storage != null)
            {
                id = (await Options.Storage.ListAsync(1).ConfigureAwait(false)).FirstOrDefault();
            }

            if (id == default(Guid))
            {
                return NotFound(context, jsonRequest ? null : "No GUID id specified on the query string");
            }

            var profiler = await Options.Storage.LoadAsync(id).ConfigureAwait(false);
            string user = Options.UserIdProvider?.Invoke(context.Request);

            await Options.Storage.SetViewedAsync(user, id).ConfigureAwait(false);

            if (profiler == null)
            {
                return NotFound(context, jsonRequest ? null : "No MiniProfiler results found with Id=" + id.ToString());
            }

            bool needsSave = false;
            if (profiler.ClientTimings == null && form != null)
            {
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
                await Options.Storage.SaveAsync(profiler).ConfigureAwait(false);
            }

            if (!AuthorizeRequest(context, isList: false, message: out string authorizeMessage))
            {
                context.Response.ContentType = "application/json";
                return @"""hidden"""; // JSON
            }

            return jsonRequest ? ResultsJson(context, profiler) : ResultsFullPage(context, profiler);
        }

        private string ResultsJson(HttpContext context, MiniProfiler profiler)
        {
            context.Response.ContentType = "application/json";
            return profiler.ToJson();
        }

        private string ResultsFullPage(HttpContext context, MiniProfiler profiler)
        {
            context.Response.ContentType = "text/html";
            return profiler.RenderResultsHtml(Current.BasePath.Value);
        }
    }
}
