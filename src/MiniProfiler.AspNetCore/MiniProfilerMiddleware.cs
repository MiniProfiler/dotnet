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
        private readonly PathString _basePath;

        internal readonly MiniProfilerOptions Options;
        internal readonly EmbeddedProvider Embedded;
        internal static MiniProfilerMiddleware Current;

        /// <summary>
        /// Creates a new instance of <see cref="MiniProfilerMiddleware"/>
        /// </summary>
        /// <param name="next">The delegate representing the next middleware in the request pipeline.</param>
        /// <param name="hostingEnvironment">The Hosting Environment.</param>
        /// <param name="options">The middleware options, containing the rules to apply.</param>
        public MiniProfilerMiddleware(
            RequestDelegate next,
            IHostingEnvironment hostingEnvironment,
            MiniProfilerOptions options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _env = hostingEnvironment ?? throw new ArgumentException(nameof(hostingEnvironment));
            Options = options ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrEmpty(Options.BasePath))
            {
                throw new ArgumentException("BasePath cannot be empty", nameof(Options.BasePath));
            }

            _basePath = new PathString(Options.BasePath);
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
        public async Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Request.Path.StartsWithSegments(_basePath, out PathString subPath))
            {
                // This is a request in the MiniProfiler path (e.g. one of "our" routes), HANDLE THE SITUATION.
                await HandleRequest(context, subPath);
                return;
            }

            // Otherwise this is an app request, profile it!
            if (Options.ShouldProfile?.Invoke(context.Request) ?? true)
            {
                MiniProfiler.Start();
                await _next(context);
                await MiniProfiler.StopAsync();
            }
            else
            {
                // Don't profile, only relay
                await _next(context);
            }
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
                    //result = Index(context);
                    break;

                case "/results-list":
                    //result = GetListJson(context);
                    break;

                case "/results":
                    result = GetSingleProfilerResult(context);
                    break;
            }

            result = result ?? NotFound(context);
            context.Response.ContentLength = result.Length;

            await context.Response.WriteAsync(result);
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
        private bool AuthorizeRequest(HttpContext context, bool isList, out string message)
        {
            message = null;
            var authorize = Options.ResultsAuthorize;
            var authorizeList = Options.ResultsListAuthorize;

            if ((authorize != null && !authorize(context.Request)) || (isList && (authorizeList == null || !authorizeList(context.Request))))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "text/plain";
                message = "unauthorized";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns either json or full page html of a previous <c>MiniProfiler</c> session, 
        /// identified by its <c>"?id=GUID"</c> on the query.
        /// </summary>
        private string GetSingleProfilerResult(HttpContext context)
        {
            var form = context.Request.Form;

            // when we're rendering as a button/popup in the corner, we'll pass ?popup=1
            // if it's absent, we're rendering results as a full page for sharing
            bool isPopup = form["popup"].Any();
            // this guid is the MiniProfiler.Id property
            // if this guid is not supplied, the last set of results needs to be
            // displayed. The home page doesn't have profiling otherwise.
            if (!Guid.TryParse(form["id"], out var id) && MiniProfiler.Settings.Storage != null)
                id = MiniProfiler.Settings.Storage.List(1).FirstOrDefault();

            if (id == default(Guid))
                return isPopup ? NotFound(context) : NotFound(context, "text/plain", "No Guid id specified on the query string");

            var profiler = MiniProfiler.Settings.Storage.Load(id);
            string user = Options.UserIdProvider?.Invoke(context.Request);

            MiniProfiler.Settings.Storage.SetViewed(user, id);

            if (profiler == null)
            {
                return isPopup ? NotFound(context) : NotFound(context, "text/plain", "No MiniProfiler results found with Id=" + id.ToString());
            }

            bool needsSave = false;
            if (profiler.ClientTimings == null)
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
                MiniProfiler.Settings.Storage.Save(profiler);
            }

            if (!AuthorizeRequest(context, isList: false, message: out string authorizeMessage))
            {
                context.Response.ContentType = "application/json";
                return "hidden".ToJson();
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
              .Replace("{path}", _basePath.Value.EnsureTrailingSlash())
              .Replace("{json}", MiniProfiler.ToJson(profiler))
              .Replace("{includes}", profiler.RenderIncludes().ToString())
              .Replace("{version}", MiniProfiler.Settings.VersionHash);
            return sb.ToString();
        }
    }
}