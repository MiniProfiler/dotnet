using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using StackExchange.Profiling.Helpers;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Represents a middleware that starts and stops a MiniProfiler
    /// </summary>
    public class MiniProfilerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostingEnvironment _env;
        private readonly MiniProfilerOptions _options;
        private readonly PathString _basePath;
        private readonly EmbeddedProvider _embedded;

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
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrEmpty(_options.BasePath))
            {
                throw new ArgumentException("BasePath cannot be empty", nameof(_options.BasePath));
            }

            _basePath = new PathString(_options.BasePath);
            _embedded = new EmbeddedProvider(_options, _env);
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
                await HandleRequest(context, subPath);

                return;
            }

            // Otherwise this is an app request, profile it!
            // TODO: Config of what to profile on options
            MiniProfiler.Start();
            await _next(context);
            await MiniProfiler.StopAsync();
        }

        private async Task HandleRequest(HttpContext context, PathString subPath)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            string result = null;

            // File embed
            if (subPath.Value.StartsWith("/includes"))
            {
                result = _embedded.GetFile(context, subPath);
            }

            switch (subPath.Value)
            {
                case "results-index":
                    //result = Index(context);
                    break;

                case "results-list":
                    //result = GetListJson(context);
                    break;

                case "results":
                    //result = GetSingleProfilerResult(context);
                    break;
            }

            result = result ?? NotFound(context);

            context.Response.ContentLength = result.Length;

            await context.Response.WriteAsync(result);
        }

        private static string NotFound(HttpContext context, string contentType = "text/plain", string message = null)
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = contentType;

            return message;
        }
    }
}