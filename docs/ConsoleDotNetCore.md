---
title: "Console .NET Core"
layout: "default"
---
### .NET Core (Console Applications)
If you'd rather learn by example, sample apps are available. [The .NET Core Console sample is here](https://github.com/MiniProfiler/dotnet/tree/master/samples/Samples.ConsoleCore), with the important bits in [Program.cs](https://github.com/MiniProfiler/dotnet/blob/master/samples/Samples.ConsoleCore/Program.cs).

#### Installation and Configuration

* Install the NuGet Package: [MiniProfiler.AspNetCore](https://www.nuget.org/packages/MiniProfiler.AspNetCore/)
   * Either use the NuGet UI to install `MiniProfiler.AspNetCore` (which has all needed dependencies)
   * Or use the Package Manager Console:

```ps
Install-Package MiniProfiler.AspNetCore -IncludePrerelease
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