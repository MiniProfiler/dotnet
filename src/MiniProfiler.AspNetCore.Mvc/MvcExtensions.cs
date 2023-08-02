using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;
using StackExchange.Profiling.Internal;
using StackExchange.Profiling.Storage;
using System;

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
        public static IMiniProfilerBuilder AddMiniProfiler(this IServiceCollection services, Action<MiniProfilerOptions>? configureOptions = null)
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

            services.AddSingleton<IMiniProfilerDiagnosticListener, MvcDiagnosticListener>(); // For view and action profiling

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
            options.Storage ??= new MemoryCacheStorage(_cache, TimeSpan.FromMinutes(30));
        }
    }
}
