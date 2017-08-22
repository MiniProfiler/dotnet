using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        /// <param name="configureOptions">Action to configure options for MiniProfiler.</param>
        /// <exception cref="ArgumentNullException">Thown if <paramref name="builder"/> is null.</exception>
        public static IApplicationBuilder UseMiniProfiler(this IApplicationBuilder builder)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));

            // Register all IMiniProfilerDiagnosticListeners that were registered, e.g. EntityFramework
            var listeners = builder.ApplicationServices.GetServices<IMiniProfilerDiagnosticListener>();
            var initializer = new DiagnosticInitializer(listeners);
            initializer.Start();

            return builder.UseMiddleware<MiniProfilerMiddleware>();
        }
    }
}
