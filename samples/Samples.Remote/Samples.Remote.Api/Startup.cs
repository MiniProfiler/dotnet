using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Samples.Remote.Api.Data;
using StackExchange.Profiling;

namespace Samples.Remote.Api
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Register MiniProfiler
            services.AddMiniProfiler()
                .AddEntityFramework();

            var connection = new SqliteConnection("DataSource=:memory:");
            // The Sqlite in memory dB is alive till the connection is open, so for the sake of simplicity we're opening
            // the connection here and keep it open forever, this should not be done in real world scenario
            connection.Open();

            services.AddDbContext<SampleContext>(options =>
            {
                options.UseSqlite(connection);
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseMiniProfiler();
            app.Use((ctx, next) =>
            {
                // Add the ID of the current to the response headers
                ctx.Response.Headers.Add("MiniProfiler-Remote-Id", MiniProfiler.Current.Id.ToString());

                return next();
            });

            app.UseHttpsRedirection();
            app.UseMvc();

            // Initialize the dB - don try this at home!
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<SampleContext>();
                context.Database.EnsureCreated();
                context.Samples.AddRange(
                    Enumerable.Range(1, 10).Select(index => new Sample { Name = $"Smaple-{index}" }));
                context.SaveChanges();
            }
        }
    }
}
