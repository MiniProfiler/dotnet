---
title: "Console .NET Core"
layout: "default"
---
### .NET Core (Console Applications)
If you'd rather learn by example, sample apps are available. [The .NET Core Console sample is here](https://github.com/MiniProfiler/dotnet/tree/master/samples/Samples.ConsoleCore), with the important bits in [Program.cs](https://github.com/MiniProfiler/dotnet/blob/master/samples/Samples.ConsoleCore/Program.cs).

#### Installation and Configuration

* Install the NuGet Package: [MiniProfiler.AspNetCore](https://www.nuget.org/packages/MiniProfiler.AspNetCore/) (There is no package for just .NET Core Console applications, see [this GitHub issue](https://github.com/MiniProfiler/dotnet/issues/363))
   * Either use the NuGet UI to install `MiniProfiler.AspNetCore` (which has all needed dependencies)
   * Or use the Package Manager Console:

```ps
Install-Package MiniProfiler.AspNetCore -IncludePrerelease
```

* Edit your `Program.cs` to configure MiniProfiler and start profiling:

```c#
public static void Main()
{
    // Default configuration usually works for most, but override, you can call:
    // MiniProfiler.Configure(new MiniProfilerOptions { ... });

    var profiler = MiniProfiler.StartNew("My Profiler Name");
    using (profiler.Step("Main Work"))
    {
        // Do some work...
    }
}
```

#### Viewing the results

To output the results you can do so from shared storage anywhere or in the simple console case you may just want some plain text output. To see the profiler tree rendered as simple text you can use:
```c#
Console.WriteLine(profiler.RenderPlainText());
// or for the active profiler:
Console.WriteLine(MiniProfiler.Current.RenderPlainText());
```
