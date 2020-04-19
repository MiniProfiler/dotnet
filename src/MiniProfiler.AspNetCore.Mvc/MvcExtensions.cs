using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StackExchange.Profiling;
using StackExchange.Profiling.Internal;
using StackExchange.Profiling.Storage;
using System;

#if NETCOREAPP3_0
using StackExchange.Profiling.Data;
#else
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
#endif

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring MiniProfiler for MVC.
    /// </summary>
    public static class MvcExtensions
    {
        /// <summary>
        /// Adds MiniProfiler timings for actions and views.
        /// </summary>
        /// <param name="services">The services collection to configure.</param>
        /// <param name="configureOptions">An <see cref="Action{MiniProfilerOptions}"/> to configure options for MiniProfiler.</param>
        public static IMiniProfilerBuilder AddMiniProfiler(this IServiceCollection services, Action<MiniProfilerOptions> configureOptions = null)
        {
            services.AddMemoryCache(); // Unconditionally register an IMemoryCache since it's the most common and default case
            services.AddSingleton<IConfigureOptions<MiniProfilerOptions>, MiniProfilerOptionsDefaults>();
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            // Set background statics
            services.Configure<MiniProfilerOptions>(o => MiniProfiler.Configure(o));
            services.AddSingleton<DiagnosticInitializer>(); // For any IMiniProfilerDiagnosticListener registration

#if NETCOREAPP3_0
            services.AddSingleton<IMiniProfilerDiagnosticListener, MvcDiagnosticListener>(); // For view and action profiling
#else
            services.AddTransient<IConfigureOptions<MvcOptions>, MiniProfilerSetup>()
                    .AddTransient<IConfigureOptions<MvcViewOptions>, MiniProfilerSetup>()
                    .AddTransient<IViewComponentInvokerFactory, ProfilingViewComponentInvokerFactory>();
#endif

            return new MiniProfilerBuilder(services);
        }
    }

    /// <summary>
    /// Configures the default (important: with DI for IMemoryCache) before further user configuration.
    /// </summary>
    internal class MiniProfilerOptionsDefaults : IConfigureOptions<MiniProfilerOptions>
    {
        private readonly IMemoryCache _cache;
        public MiniProfilerOptionsDefaults(IMemoryCache cache) => _cache = cache;

        public void Configure(MiniProfilerOptions options)
        {
            if (options.Storage == null)
            {
                options.Storage = new MemoryCacheStorage(_cache, TimeSpan.FromMinutes(30));
            }
        }
    }

#if !NETCOREAPP3_0
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
#endif
}
