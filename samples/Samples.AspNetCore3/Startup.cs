﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Samples.AspNetCore.Models;
using StackExchange.Profiling.Storage;

namespace Samples.AspNetCore
{
    public class Startup
    {
        public static string SqliteConnectionString { get; } = "Data Source=Samples; Mode=Memory; Cache=Shared";

        private static readonly SqliteConnection TrapConnection = new SqliteConnection(SqliteConnectionString);

        public Startup(IWebHostEnvironment env)
        {
            TrapConnection.Open(); //Hold the in-memory SQLite database open

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.WebRootPath)
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
            services.AddMvc(options =>
            {
                // Because the samples have some MyAction and MyActionAsync duplicates
                // See: https://github.com/aspnet/AspNetCore/issues/8998
                options.SuppressAsyncSuffixInActionNames = false;
            });

            // Add MiniProfiler services
            // If using Entity Framework Core, add profiling for it as well (see the end)
            // Note .AddMiniProfiler() returns a IMiniProfilerBuilder for easy Intellisense
            services.AddMiniProfiler(options =>
            {
                // ALL of this is optional. You can simply call .AddMiniProfiler() for all defaults
                // Defaults: In-Memory for 30 minutes, everything profiled, every user can see

                // Path to use for profiler URLs, default is /mini-profiler-resources
                options.RouteBasePath = "/profiler";

                // Control storage - the default is 30 minutes
                //(options.Storage as MemoryCacheStorage).CacheDuration = TimeSpan.FromMinutes(60);
                //options.Storage = new SqlServerStorage("Data Source=.;Initial Catalog=MiniProfiler;Integrated Security=True;");

                // Control which SQL formatter to use, InlineFormatter is the default
                options.SqlFormatter = new StackExchange.Profiling.SqlFormatters.SqlServerFormatter();

                // To control authorization, you can use the Func<HttpRequest, bool> options:
                options.ResultsAuthorize = _ => !Program.DisableProfilingResults;
                //options.ResultsListAuthorize = request => MyGetUserFunction(request).CanSeeMiniProfiler;
                //options.ResultsAuthorizeAsync = async request => (await MyGetUserFunctionAsync(request)).CanSeeMiniProfiler;
                //options.ResultsAuthorizeListAsync = async request => (await MyGetUserFunctionAsync(request)).CanSeeMiniProfilerLists;

                // To control which requests are profiled, use the Func<HttpRequest, bool> option:
                //options.ShouldProfile = request => MyShouldThisBeProfiledFunction(request);

                // Profiles are stored under a user ID, function to get it:
                //options.UserIdProvider =  request => MyGetUserIdFunction(request);

                // Optionally swap out the entire profiler provider, if you want
                // The default handles async and works fine for almost all applications
                //options.ProfilerProvider = new MyProfilerProvider();

                // Optionally disable "Connection Open()", "Connection Close()" (and async variants).
                //options.TrackConnectionOpenClose = false;

                // Optionally use something other than the "light" color scheme.
                options.ColorScheme = StackExchange.Profiling.ColorScheme.Auto;

                // Enabled sending the Server-Timing header on responses
                options.EnableServerTimingHeader = true;

                // Optionally disable MVC filter profiling
                //options.EnableMvcFilterProfiling = false;
                // Or only save filters that take over a certain millisecond duration (including their children)
                //options.MvcFilterMinimumSaveMs = 1.0m;

                // Optionally disable MVC view profiling
                //options.EnableMvcViewProfiling = false;
                // Or only save views that take over a certain millisecond duration (including their children)
                //options.MvcViewMinimumSaveMs = 1.0m;

                // This enables debug mode with stacks and tooltips when using memory storage
                // It has a lot of overhead vs. normal profiling and should only be used with that in mind
                //options.EnableDebugMode = true;
                
                // Optionally listen to any errors that occur within MiniProfiler itself
                //options.OnInternalError = e => MyExceptionLogger(e);

                options.IgnoredPaths.Add("/lib");
                options.IgnoredPaths.Add("/css");
                options.IgnoredPaths.Add("/js");
            }).AddEntityFramework();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            
            app.UseMiniProfiler()
               .UseStaticFiles()
               .UseRouting()
               .UseEndpoints(endpoints =>
               {
                   endpoints.MapAreaControllerRoute("areaRoute", "MySpace",
                       "MySpace/{controller=Home}/{action=Index}/{id?}");
                   endpoints.MapControllerRoute("default_route","{controller=Home}/{action=Index}/{id?}");
                  
                   endpoints.MapRazorPages();
                   endpoints.MapGet("/named-endpoint", async httpContext =>
                   {
                       var endpointName = httpContext.GetEndpoint().DisplayName;
                       await httpContext.Response.WriteAsync($"Content from an endpoint named {endpointName}");
                   }).WithDisplayName("Named Endpoint");

                   endpoints.MapGet("implicitly-named-endpoint", async httpContext =>
                   {
                       var endpointName = httpContext.GetEndpoint().DisplayName;
                       await httpContext.Response.WriteAsync($"Content from an endpoint named {endpointName}");
                   });
               });

            var serviceScopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            using (var serviceScope = serviceScopeFactory.CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetService<SampleContext>();
                dbContext.Database.EnsureCreated();
            }
            // For nesting test routes
            new SqliteStorage(SqliteConnectionString).WithSchemaCreation();
        }
    }
}
