## MiniProfiler for Entity Framework Classic 7 (https://entityframework-classic.net/)

This package allows you to profile your Entity Framework Classic 7 requests using MiniProfiler.

You can install a [nuget package](https://www.nuget.org/packages/MiniProfiler.EFC7/) using `Install-Package MiniProfiler.EFC76 -Pre`. 

To initialize, simply call the following in your application startup logic:

    using StackExchange.Profiling.EntityFrameworkClassic7;

	...

    protected void Application_Start()
    {
        MiniProfilerEFC7.Initialize();
    }

Be sure to call this before using EF in any way.

Then it should just work.