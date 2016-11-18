## MiniProfiler for Entity Framework 6

This package allows you to profile your Entity Framework 6 requests using MiniProfiler.

You can install a [nuget package](https://www.nuget.org/packages/MiniProfiler.EF6/) using `Install-Package MiniProfiler.EF6 -Pre`. 

To initialize, simply call the following in your application startup logic:

    using StackExchange.Profiling.EntityFramework6;

	...

    protected void Application_Start()
    {
        MiniProfilerEF6.Initialize();
    }

Be sure to call this before using EF in any way.

Then it should just work.