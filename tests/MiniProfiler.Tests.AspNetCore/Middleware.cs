using System;
using System.Drawing.Text;
using System.Linq;
using System.Net;
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

        private TestServer GetTestServer(Action<MiniProfilerOptions> configOptions)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services => services
                    .AddMemoryCache()
                    .AddMiniProfiler(configOptions)
                )
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
            return new TestServer(builder);
        }

        [Fact]
        public async Task BasicProfiling()
        {
            using (var server = GetTestServer(o =>
            {
                o.ShouldProfile = _ => true;
                o.UserIdProvider = _ => nameof(BasicProfiling);
                CurrentOptions = o;
            }))
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
                    Assert.Equal("MiniProfiler Init", profiler.Root.Children[0].Name);
                }

                var testStep = profiler.Root.Children.Last();
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

        [Theory]
        [InlineData("All allowed - No config", null, null, null, null, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK)]
        [InlineData("All allowed - Both", true, true, true, true, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK)]
        [InlineData("All allowed - Sync", true, null, null, null, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK)]
        [InlineData("All allowed - Async", null, true, null, null, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK)]
        [InlineData("Denied - Both", false, false, false, false, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized)]
        [InlineData("Denied - Sync", false, null, null, null, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized)]
        [InlineData("Denied - Async", null, false, null, null, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized)]
        [InlineData("Denied - Async Wins", true, false, null, null, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized)]
        [InlineData("Denied - Sync Wins", false, true, null, null, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized)]
        [InlineData("ResultsAuthorize can only deny access - Both", true, true, false, false, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.OK)]
        [InlineData("ResultsAuthorize can only deny access - Sync", true, null, false, null, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.OK)]
        [InlineData("ResultsAuthorize can only deny access - Async", null, true, null, false, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.OK)]
        [InlineData("ResultsAuthorize can only deny access - Mix 1", null, true, false, null, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.OK)]
        [InlineData("ResultsAuthorize can only deny access - Mix 2", true, null, null, false, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.OK)]
        [InlineData("No lists because no single - Both", false, false, true, true, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized)]
        [InlineData("No lists because no single - Sync", false, null, true, null, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized)]
        [InlineData("No lists because no single - Async", null, false, null, true, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized)]
        [InlineData("No lists because no single - Mix 1", null, false, true, null, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized)]
        [InlineData("No lists because no single - Mix 2", false, null, null, true, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized)]
        public async Task ResultsAuthorization(
            string name,
            bool? auth,
            bool? authAsync,
            bool? listAuth,
            bool? listAuthAsync,
            HttpStatusCode indexExpected,
            HttpStatusCode listExpected,
            HttpStatusCode singleExpected)
        {
            using (var server = GetTestServer(o =>
            {
                o.ShouldProfile = _ => true;
                o.UserIdProvider = _ => nameof(ResultsAuthorization);
                if (auth.HasValue)
                {
                    o.ResultsAuthorize = req => auth.Value;
                }
                if (authAsync.HasValue)
                {
                    o.ResultsAuthorizeAsync = req => Task.FromResult(authAsync.Value);
                }
                if (listAuth.HasValue)
                {
                    o.ResultsListAuthorize = req => listAuth.Value;
                }
                if (listAuthAsync.HasValue)
                {
                    o.ResultsListAuthorizeAsync = req => Task.FromResult(listAuthAsync.Value);
                }
                CurrentOptions = o;
            }))
            {
                Output.WriteLine("Testing: " + name);
                var client = server.CreateClient();
                string id;
                using (var response = await client.GetAsync(""))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    id = Assert.Single(response.Headers.GetValues("X-MiniProfiler-Ids"));
                }
                Assert.NotNull(id);

                string Path(string path) => CurrentOptions.RouteBasePath + "/" + path;

                using (var response = await client.GetAsync(Path("results-index")))
                {
                    Output.WriteLine("Hitting: " + response.RequestMessage.RequestUri);
                    Output.WriteLine("  Response: " + await response.Content.ReadAsStringAsync());
                    Output.WriteLine("  Code: " + response.StatusCode);
                    Output.WriteLine("  Expected Code: " + indexExpected);
                    Assert.Equal(indexExpected, response.StatusCode);
                }

                using (var response = await client.GetAsync(Path("results-list")))
                {
                    Output.WriteLine("Hitting: " + response.RequestMessage.RequestUri);
                    Output.WriteLine("  Response: " + await response.Content.ReadAsStringAsync());
                    Output.WriteLine("  Code: " + response.StatusCode);
                    Output.WriteLine("  Expected Code: " + listExpected);
                    Assert.Equal(listExpected, response.StatusCode);
                }

                using (var response = await client.GetAsync(Path("results?id=" + id)))
                {
                    Output.WriteLine("Hitting: " + response.RequestMessage.RequestUri);
                    Output.WriteLine("  Response: " + await response.Content.ReadAsStringAsync());
                    Output.WriteLine("  Code: " + response.StatusCode);
                    Output.WriteLine("  Expected Code: " + singleExpected);
                    Assert.Equal(singleExpected, response.StatusCode);
                }
            }
        }
    }
}
