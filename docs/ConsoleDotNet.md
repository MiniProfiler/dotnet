---
title: "Console .NET"
layout: "default"
---
### .NET (Console Applications)
If you'd rather learn by example, sample apps are available. [The .NET Console sample is here](https://github.com/MiniProfiler/dotnet/tree/master/samples/Samples.Console), with the important bits in [Program.cs](https://github.com/MiniProfiler/dotnet/blob/master/samples/Samples.Console/Program.cs).

#### Installation and Configuration

* Install the NuGet Package: [MiniProfiler](https://www.nuget.org/packages/MiniProfiler/)
   * Either use the NuGet UI to install `MiniProfiler` (which has all needed dependencies)
   * Or use the Package Manager Console:

```ps
Install-Package MiniProfiler -IncludePrerelease
```

* Edit your `Program.cs` to configure MiniProfiler and start profiling:

```c#
public static void Main()
{
    // Default configuration usually works for most, but overrde, you can call:
    // MiniProfiler.Configure(new MiniProfilerOptions { ... });

    var profiler = MiniProfiler.StartNew("My Pofiler Name");
    using (profiler.Step("Main Work"))
    {
        // Do some work...
    }
}
```
> Note that we're using `DefaultProfilerProvider` here because we're not in a web context, e.g. we don't want pofiles accessed via `HttpContext.Items` in a console application.

#### Viewing the results

To output the results you can do so from shared storage anywhere or in the simple console case you may just want some plain text output. To see the profiler tree rendered as simple text you can use:
```c#
Console.WriteLine(profiler.RenderPlainText());
// or for the active profiler:
Console.WriteLine(MiniProfiler.Current.RenderPlainText());
```