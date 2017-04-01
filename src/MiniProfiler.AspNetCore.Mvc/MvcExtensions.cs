using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Profiling.Internal;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring MiniProfiler for MVC
    /// </summary>
    public static class MvcExtensions
    {
        /// <summary>
        /// Adds MiniProfiler timings for actions and views
        /// </summary>
        /// <param name="services">The services collection to configure</param>
        public static IMiniProfilerBuilder AddMiniProfiler(this IServiceCollection services)
        {
            services.AddTransient<IConfigureOptions<MvcOptions>, MiniProfilerSetup>()
                    .AddTransient<IConfigureOptions<MvcViewOptions>, MiniProfilerSetup>();
            return new MiniProfilerBuilder(services);
        }
    }

    internal class MiniProfilerSetup : IConfigureOptions<MvcViewOptions>, IConfigureOptions<MvcOptions>
    {
        public void Configure(MvcViewOptions options)
        {
            for (var i = 0; i < options.ViewEngines.Count; i++)
            {
                options.ViewEngines[i] = new ProfilingViewEngine(options.ViewEngines[i]);
            }
        }

        public void Configure(MvcOptions options)
        {
            options.Filters.Add(new ProfilingActionFilter());
        }
    }
}
