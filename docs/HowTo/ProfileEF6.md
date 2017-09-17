---
layout: "default"
---
### How-To Profile Entity Framework 6

Hooking up profiling to Entity Framework 6 is easy to do:

1. Install the [MiniProfiler.EF6](https://www.nuget.org/packages/MiniProfiler.EF6) NuGet package.
2. Where you configure MiniProfiler (for example in `Application_Start`), call `Initialize` once:

```c#
using StackExchange.Profiling.EntityFramework6;

protected void Application_Start()
{
    MiniProfilerEF6.Initialize();
}
```
> Note: If you are using `PreApplicationStartMethod`, you may need to move this `.Initialize();` call up to that point. See [#98](https://github.com/MiniProfiler/dotnet/issues/98) fo details.

Entity Framework 6 is now configured for profiling.