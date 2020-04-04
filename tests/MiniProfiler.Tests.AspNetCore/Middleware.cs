using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests
{
    [Collection(NonParallel)]
    public class Middleware : AspNetCoreTest
    {
        public Middleware(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task BasicProfiling()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services => services
                .AddMemoryCache()
                .AddMiniProfiler(o =>
                {
                    o.ShouldProfile = _ => true;
                    o.UserIdProvider = _ => nameof(BasicProfiling);
                    CurrentOptions = o;
                }))
                .Configure(app =>
                {
                    app.UseMiniProfiler();
                    app.Run(async context => {
                        using (MiniProfiler.Current.Step("Test"))
                        {
                            using (MiniProfiler.Current.CustomTiming("DB", "Select 1", "Reader"))
                            {
                                await Task.Delay(20).ConfigureAwait(false);
                            }
                        }
                        await context.Response.WriteAsync("Heyyyyyy").ConfigureAwait(false);
                    });
                });
            using (var server = new TestServer(builder))
            {
                using (var response = await server.CreateClient().GetAsync("").ConfigureAwait(false))
                {
                    var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Assert.Contains("Heyyy", responseText);
                }

                var unviewedIds = GetProfilerIds();
                Assert.Single(unviewedIds);
                var profiler = CurrentOptions.Storage.Load(unviewedIds[0]);
                Assert.NotNull(profiler);
                Assert.Equal(nameof(BasicProfiling), profiler.User);
                Assert.False(profiler.Stop());

                // Prep is a StepIf, for no noise in the fast case
                if (profiler.Root.Children.Count == 2)
                {
                    Assert.Equal("MiniProfiler Prep", profiler.Root.Children[0].Name);
                }

                var testStep = profiler.Root.Children[1];
                Assert.Equal("Test", testStep.Name);
                Assert.False(testStep.HasChildren);
                Assert.True(testStep.HasCustomTimings);
                Assert.Equal(1, testStep.Depth);

                Assert.Single(testStep.CustomTimings);
                Assert.Contains("DB", testStep.CustomTimings.Keys);
                var customTimings = testStep.CustomTimings["DB"];
                Assert.Single(customTimings);
                Assert.Equal("Select 1", customTimings[0].CommandString);
                Assert.Equal("Reader", customTimings[0].ExecuteType);
                // We can't safely assert this on a test run
                //Assert.True(customTimings[0].DurationMilliseconds >= 20);
            }
        }

        [Fact]
        public async Task StaticFileFetch()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services => services
                .AddMemoryCache()
                .AddMiniProfiler(_ => { }))
                .Configure(app =>
                {
                    app.UseMiniProfiler();
                    app.Run(_ => Task.CompletedTask);
                });
            using (var server = new TestServer(builder))
            {
                // Test CSS
                using (var response = await server.CreateClient().GetAsync("/mini-profiler-resources/includes.min.css").ConfigureAwait(false))
                {
                    Assert.Equal(TimeSpan.FromDays(30), response.Headers.CacheControl.MaxAge);
                    Assert.True(response.Headers.CacheControl.Public);
                    Assert.Equal("text/css", response.Content.Headers.ContentType.MediaType);
                    // Checking for wrapping/scoping class
                    Assert.StartsWith(":root", await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                }
                // Test JS
                using (var response = await server.CreateClient().GetAsync("/mini-profiler-resources/includes.min.js").ConfigureAwait(false))
                {
                    Assert.Equal(TimeSpan.FromDays(30), response.Headers.CacheControl.MaxAge);
                    Assert.True(response.Headers.CacheControl.Public);
                    Assert.Equal("application/javascript", response.Content.Headers.ContentType.MediaType);
                    // Checking for license header
                    Assert.Contains("jQuery", await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                }
            }
        }
    }
}
