---
title: "ASP.NET Core"
layout: "default"
---
### ASP.NET Core
If you'd rather learn by example, sample apps are available. [The ASP.NET Core sample is here](https://github.com/MiniProfiler/dotnet/tree/master/samples/Samples.AspNetCore2), with the important bits in [Startup.cs](https://github.com/MiniProfiler/dotnet/blob/master/samples/Samples.AspNetCore2/Startup.cs).

#### Installation and Configuration

* Install the NuGet Package: [MiniProfiler.AspNetCore.Mvc](https://www.nuget.org/packages/MiniProfiler.AspNetCore.Mvc/)
   * Either use the NuGet UI to install `MiniProfiler.AspNetCore.Mvc` (which has all needed dependencies)
   * Or use the Package Manager Console:

```ps
Install-Package MiniProfiler.AspNetCore.Mvc -IncludePrerelease
```

* Edit your `Startup.cs` to add the middleware and configure options:

```c#
public void ConfigureServices(IServiceCollection services)
{
    // ...existing configuration...

    // Note .AddMiniProfiler() returns a IMiniProfilerBuilder for easy intellisense
    services.AddMiniProfiler(options =>
    {
        // All of this is optional. You can simply call .AddMiniProfiler() for all defaults

        // (Optional) Path to use for profiler URLs, default is /mini-profiler-resources
        options.RouteBasePath = "/profiler";

        // (Optional) Control storage
        // (default is 30 minutes in MemoryCacheStorage)
        (options.Storage as MemoryCacheStorage).CacheDuration = TimeSpan.FromMinutes(60);

        // (Optional) Control which SQL formatter to use, InlineFormatter is the default
        options.SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter();

        // (Optional) To control authorization, you can use the Func<HttpRequest, bool> options:
        // (default is everyone can access profilers)
        options.ResultsAuthorize = request => MyGetUserFunction(request).CanSeeMiniProfiler;
        options.ResultsListAuthorize = request => MyGetUserFunction(request).CanSeeMiniProfiler;
        // Or, there are async versions available:
        options.ResultsAuthorizeAsync = async request => (await MyGetUserFunctionAsync(request)).CanSeeMiniProfiler;
        options.ResultsAuthorizeListAsync = async request => (await MyGetUserFunctionAsync(request)).CanSeeMiniProfilerLists;

        // (Optional)  To control which requests are profiled, use the Func<HttpRequest, bool> option:
        // (default is everything should be profiled)
        options.ShouldProfile = request => MyShouldThisBeProfiledFunction(request);

        // (Optional) Profiles are stored under a user ID, function to get it:
        // (default is null, since above methods don't use it by default)
        options.UserIdProvider =  request => MyGetUserIdFunction(request);

        // (Optional) Swap out the entire profiler provider, if you want
        // (default handles async and works fine for almost all applications)
        options.ProfilerProvider = new MyProfilerProvider();

        // (Optional) You can disable "Connection Open()", "Connection Close()" (and async variant) tracking.
        // (defaults to true, and connection opening/closing is tracked)
        options.TrackConnectionOpenClose = true;

        // (Optional) Use something other than the "light" color scheme.
        // (defaults to "light")
        options.ColorScheme = StackExchange.Profiling.ColorScheme.Auto;
    });
}
```

* Configure MiniProfiler with the options you want:

```c#
public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IMemoryCache cache)
{
    // ...existing configuration...
    app.UseMiniProfiler();

    // The call to app.UseMiniProfiler must come before the call to app.UseMvc
    app.UseMvc(routes =>
    {
        // ...
    });
}
```
<sub>Note: most of the above are optional. A config can be as minimal as `app.UseMiniProfiler(new MiniProfilerOptions()));`</sub>

* Add Tag Helpers in `_ViewImports.cshtml`:

```
@using StackExchange.Profiling
@addTagHelper *, MiniProfiler.AspNetCore.Mvc
```

* Add MiniProfiler to your master layout (`Shared/_Layout.cshtml` by default):

```html
<mini-profiler />
```
<sub>Note: `<mini-profiler>` has many options like `max-traces`, `position`, `color-scheme`, `nonce`, etc. [You can find them in code here](https://github.com/MiniProfiler/dotnet/blob/master/src/MiniProfiler.AspNetCore.Mvc/MiniProfilerScriptTagHelper.cs).</sub>
<sub>Note #2: The above tag helper registration may go away in future versions of ASP.NET Core, they're working on smoother alternatives here.</sub>



#### Profiling
Now you're ready to profile. In addition to [the usual `using` wrap method]({{ site.baseurl }}/HowTo/ProfileCode) for profiling sections of code, ASP.NET Core includes a tag helper you can use in views like this:

```html
<profile name="My Profiling Step via a <profile> Tag">
    @{ SomethingExpensive(); }
    <span>Hello Mars!</span>
</profile>
```
