using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Represents a middleware that starts and stops a MiniProfiler
    /// </summary>
    public class MiniProfilerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MiniProfilerOptions _options;

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
            _options = options ?? throw new ArgumentNullException(nameof(options));
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
            
            MiniProfiler.Start();
            await _next(context);
            await MiniProfiler.StopAsync();
        }
    }
}