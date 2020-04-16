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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
        public static IApplicationBuilder UseMiniProfiler(this IApplicationBuilder builder)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));

            // Register all IMiniProfilerDiagnosticListeners that were registered, e.g. EntityFramework
            // Note: this is a no-op after the first pass, e.g. for middleware branching support
            builder.ApplicationServices.GetService<DiagnosticInitializer>()?.Start();

            return builder.UseMiddleware<MiniProfilerMiddleware>();
        }
    }
}
