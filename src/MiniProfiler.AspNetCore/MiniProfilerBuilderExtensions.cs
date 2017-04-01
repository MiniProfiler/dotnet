using Microsoft.Extensions.DependencyInjection;
using StackExchange.Profiling;
using StackExchange.Profiling.Internal;
using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for the MiniProfiler middleware.
    /// </summary>
    public static class MiniProfilerBuilderExtensions
    {
        /// <summary>
        /// Adds middleware for profiling HTTP requests.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
        /// <param name="options">Options for MiniProfiler.</param>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="options"/> is <c>null</c>.</exception>
        public static IApplicationBuilder UseMiniProfiler(this IApplicationBuilder builder, MiniProfilerOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // Register all IMiniProfilerDiagnosticListeners that were registered, e.g. EntityFramework
            var listeners = builder.ApplicationServices.GetServices<IMiniProfilerDiagnosticListener>();
            var initializer = new DiagnosticInitializer(listeners);
            initializer.Start();

            return builder.UseMiddleware<MiniProfilerMiddleware>(options);
        }
    }
}
