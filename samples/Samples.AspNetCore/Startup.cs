using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Samples.AspNetCore.Models;
using StackExchange.Profiling;
using StackExchange.Profiling.Storage;
using System;
using System.IO;

namespace Samples.AspNetCore
{
    public class Startup
    {
        public static string SqliteConnectionString { get; set; }

        public Startup(IHostingEnvironment env)
        {
            SqliteConnectionString = "Data Source = " + Path.Combine(env.ContentRootPath, @"App_Data\TestMiniProfiler.sqlite");
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddDbContext<SampleContext>();

            services.AddMvc();
            services.AddMiniProfiler().AddEntityFramework();
            services.AddMemoryCache();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IMemoryCache cache)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMiniProfiler(new MiniProfilerOptions
            {
                RouteBasePath = "~/profiler",
                SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter(),
                Storage = new MemoryCacheStorage(cache, TimeSpan.FromMinutes(60))
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
