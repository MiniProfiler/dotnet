using Microsoft.Extensions.DependencyInjection;
using System;

namespace StackExchange.Profiling.Internal
{
    /// <summary>
    /// Allows fine grained configuration of MiniProfilder services.
    /// </summary>
    public class MiniProfilerBuilder : IMiniProfilerBuilder
    {
        /// <summary>
        /// Initializes a new <see cref="MiniProfilerBuilder"/> instance.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="services"/> is null.</exception>
        public MiniProfilerBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// Services for this <see cref="MiniProfilerBuilder"/>.
        /// </summary>
        public IServiceCollection Services { get; }
    }
}
