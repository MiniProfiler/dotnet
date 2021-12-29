using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Profiling.Storage;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Builder;
using System.Collections.Generic;

namespace StackExchange.Profiling.Tests
{
    public abstract class AspNetCoreTest : BaseTest
    {
        protected MiniProfilerOptions CurrentOptions { get; set; }

        protected AspNetCoreTest(ITestOutputHelper output) : base(output)
        {
            // Instance per class, so multiple tests swapping the provider don't cause issues here
            // It's not a threading issue of the profiler, but rather tests swapping providers
            Options = new MiniProfilerOptions()
            {
                StopwatchProvider = () => new UnitTestStopwatch(),
                Storage = new MemoryCacheStorage(GetMemoryCache(), TimeSpan.FromDays(1))
            };
        }

        protected static MemoryCache GetMemoryCache() => new MemoryCache(new MemoryCacheOptions());

        protected static string UserName([CallerMemberName]string name = null) => name;

        protected List<Guid> GetProfilerIds([CallerMemberName]string name = null) =>
            CurrentOptions?.Storage.GetUnviewedIds(name);

        protected TestServer GetServer(RequestDelegate requestDelegate, [CallerMemberName]string name = null) =>
            new TestServer(BasicBuilder(requestDelegate, name));

        protected IWebHostBuilder BasicBuilder(RequestDelegate requestDelegate, [CallerMemberName]string name = null) =>
            new WebHostBuilder()
               .ConfigureServices(services => services.AddMiniProfiler(o =>
               {
                   o.Storage = new MemoryCacheStorage(GetMemoryCache(), TimeSpan.FromDays(1));
                   o.StopwatchProvider = () => new UnitTestStopwatch();
                   o.UserIdProvider = _ => name;
                   CurrentOptions = o;
               }))
               .Configure(app =>
               {
                   app.UseMiniProfiler();
                   app.Run(requestDelegate);
               });
    }
}
