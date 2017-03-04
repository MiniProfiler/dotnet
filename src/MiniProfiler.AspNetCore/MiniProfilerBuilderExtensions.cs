using StackExchange.Profiling;
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

            return builder.UseMiddleware<MiniProfilerMiddleware>(options);
        }
    }
}
