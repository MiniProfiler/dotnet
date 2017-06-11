---
layout: "default"
---
### How-To Profile Entity Framework Core

Hooking up profiling to Entity Framework Core is easy to do:

1. Install the [MiniProfiler.EntityFrameworkCore](https://www.nuget.org/packages/MiniProfiler.EntityFrameworkCore) NuGet package.
2. In your `Startup.cs`, call `AddEntityFramework()`:

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddMiniProfiler()
            .AddEntityFramework();
}
```

Entity Framework Core is now configured for profiling.