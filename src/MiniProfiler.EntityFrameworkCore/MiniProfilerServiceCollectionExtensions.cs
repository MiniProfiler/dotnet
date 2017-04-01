using StackExchange.Profiling.Data;
using StackExchange.Profiling.Internal;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for the MiniProfiler.EntityFrameworkCore.
    /// </summary>
    public static class MiniProfilerServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Entity Framework Core profiling for MiniProfiler via DiagnosticListener.
        /// </summary>
        /// <param name="builder">The <see cref="IMiniProfilerBuilder" /> to add services to.</param>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
        public static IMiniProfilerBuilder AddEntityFramework(this IMiniProfilerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton<IMiniProfilerDiagnosticListener, RelationalDiagnosticListener>();

            return builder;
        }
    }
}
