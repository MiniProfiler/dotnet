using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Samples.Remote.Mvc.Client;

namespace Samples.Remote.Mvc
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Add the Http client with a custom message handler
            services.AddTransient<SamplesApiHttpClientHandler>();
            services.AddHttpClient<SamplesApiHttpClient>(client =>
                {
                    client.BaseAddress = new Uri("http://localhost:63227");
                })
                .AddHttpMessageHandler<SamplesApiHttpClientHandler>();

            // Add MiniProfiler
            services.AddMiniProfiler(options =>
            {
                // Replace the build in storage with our wrapping storage that loads the remote profiling session
                options.Storage = new RemoteAsyncStorage(options.Storage);
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
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseMiniProfiler();
            app.UseMvc();
        }
    }
}
