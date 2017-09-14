---
title: "ASP.NET"
layout: "default"
---
### ASP.NET
If you'd rather learn by example, sample apps are available. [The ASP.NET MVC 5 sample is here](https://github.com/MiniProfiler/dotnet/tree/master/samples/Samples.Mvc5), with the important bits in [Global.asax.cs](https://github.com/MiniProfiler/dotnet/blob/master/samples/Samples.Mvc5/Global.asax.cs).

#### Installation and Configuration

* Install the NuGet Package: [MiniProfiler.Mvc5](https://www.nuget.org/packages/MiniProfiler.Mvc5/)
   * Either use the NuGet UI to install `MiniProfiler.Mvc5` (which has all needed dependencies)
   * Or use the Package Manager Console:

```ps
Install-Package MiniProfiler.Mvc5 -IncludePrerelease
```

* Edit your `Global.asax` to configure MiniProfiler in `Application_Start` and profile requests:

```c#
protected void Application_Start()
{
    // This is in another method just for example purposes
    InitProfilerSettings();

    // If you want to profile views, wrap the view engine like this:
    var copy = ViewEngines.Engines.ToList();
    ViewEngines.Engines.Clear();
    foreach (var item in copy)
    {
        ViewEngines.Engines.Add(new ProfilingViewEngine(item));
    }
}

protected void Application_BeginRequest()
{
    // You can decide whether to profile here, or it can be done in ActionFilters, etc.
    // We're doing it here so profiling happens ASAP to account for as much time as possible.
    if (Request.IsLocal) // Example of conditional profiling, you could just call MiniProfiler.Start();
    {
        MiniProfiler.Start();
    }
}

protected void Application_EndRequest()
{
    MiniProfiler.Stop(); // Be sure to stop the profiler!
}

private void InitProfilerSettings()
{
    // Stray from the in-memory storage provider and use SQL Server instead
    MiniProfiler.Settings.Storage = new SqliteMiniProfilerStorage(ConnectionString);

    // Sets up the WebRequestProfilerProvider with
    // ~/profiler as the route path to use (e.g. /profiler/mini-profiler-includes.js)
    WebRequestProfilerProvider.Setup("~/profiler",
        // ResultsAuthorize (optional - open to all by default):
        // because profiler results can contain sensitive data (e.g. sql queries with parameter values displayed), we
        // can define a function that will authorize clients to see the json or full page results.
        // we use it on https://stackoverflow.com to check that the request cookies belong to a valid developer.
        request => request.IsLocal,
        // ResultsListAuthorize (optional - open to all by default)
        // the list of all sessions in the store is restricted by default, you must return true to allow it
        request => request.IsLocal
    );
}
```
