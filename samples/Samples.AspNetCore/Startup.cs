using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Samples.AspNetCore.Models;
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
            services.AddMemoryCache();
            // Add MiniProfiler services
            // If using Entity Framework Core, add profiling for it as well
            // Note .AddMiniProfiler() returns a IMiniProfilerBuilder for easy intellisense
            services.AddMiniProfiler(options =>
            {
                // Path to use for profiler URLs
                options.RouteBasePath = "/profiler";

                // Control storage
                options.Storage = new MemoryCacheStorage(services.BuildServiceProvider().GetService<IMemoryCache>(), TimeSpan.FromMinutes(60));

                // Control which SQL formatter to use, InlineFormatter is the default
                //options.SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter();

                // To control authorization, you can use the Func<HttpRequest, bool> options:
                //options.ResultsAuthorize = request => MyGetUserFunction(request).CanSeeMiniProfiler;
                //options.ResultsListAuthorize = request => MyGetUserFunction(request).CanSeeMiniProfiler;

                // To control which requests are profiled, use the Func<HttpRequest, bool> option:
                //options.ShouldProfile = request => MyShouldThisBeProfiledFunction(request);

                // Profiles are stored under a user ID, function to get it:
                //options.UserIdProvider =  request => MyGetUserIdFunction(request);

                // Optionally swap out the entire profiler provider, if you want
                // The default handles async and works fine for almost all appliations
                //options.ProfilerProvider = new MyProfilerProvider();
            }).AddEntityFramework();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
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

            app.UseMiniProfiler();

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
