using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Linq;

namespace StackExchange.Profiling.Mvc
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
        public static IServiceCollection AddMiniProfiler(this IServiceCollection services) =>
            services.AddTransient<IConfigureOptions<MvcOptions>, MiniPofilerSetup>()
                    .AddTransient<IConfigureOptions<MvcViewOptions>, MiniPofilerSetup>();
    }

    internal class MiniPofilerSetup : IConfigureOptions<MvcViewOptions>, IConfigureOptions<MvcOptions>
    {
        public void Configure(MvcViewOptions options)
        {
            var copy = options.ViewEngines.ToList();
            options.ViewEngines.Clear();
            foreach (var item in copy)
            {
                options.ViewEngines.Add(new ProfilingViewEngine(item));
            }
        }

        public void Configure(MvcOptions options)
        {
            options.Filters.Add(new ProfilingActionFilter());
        }
    }
}
