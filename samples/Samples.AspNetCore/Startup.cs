using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Samples.AspNetCore.Models;
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

            // Add MiniProfiler services
            // If using Entity Framework Core, add profiling for it as well (see the end)
            // Note .AddMiniProfiler() returns a IMiniProfilerBuilder for easy intellisense
            services.AddMiniProfiler(options =>
            {
                // ALL of this is optional. You can simply call .AddMiniProfiler() for all defaults
                // Defaults: In-Memory for 30 minutes, everything profiled, every user can see

                // Path to use for profiler URLs, default is /mini-profiler-resources
                options.RouteBasePath = "/profiler";
                
                // Control storage - the default is 30 minutes
                //(options.Storage as MemoryCacheStorage).CacheDuration = TimeSpan.FromMinutes(60);

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
