---
layout: "default"
---
### MiniProfiler for .NET (including ASP.NET & ASP.NET Core)

MiniProfiler for .NET now has a few flavors since .NET Core differs quite a bit from .NET Full Framework on the ASP.NET front. The pipelines have changed drastically (for the better), but that means how to configure MiniProfiler has also changed quite a bit.

- If you're using ASP.NET (*not* .NET Core), [**start here**]({{ site.baseurl }}/AspDotNet).
- If you're using ASP.NET Core, [**start here**]({{ site.baseurl }}/AspDotNetCORE).

Once you're setup, see [**how to profile code**]({{ site.baseurl }}/HowTo/ProfileCode) to get some timings up and running.

#### Minimum requirements
MiniProfiler v4 runs on .NET 4.6 and above or .NET Standard 1.5 and above. .NET 4.6+ is required due to all of the `async` support added in v4. If you need to use a version earlier than .NET 4.6, MiniProfiler v3.x is for you.

#### Links
* The MiniProfiler for .NET GitHub repo [is located at MiniProfiler/dotnet](https://github.com/MiniProfiler/dotnet).
* We accept [pull requests](https://github.com/MiniProfiler/dotnet/pulls) here.
* Any issues can be reported in [GitHub Issues](https://github.com/MiniProfiler/dotnet/issues) or on the [Community Site](http://community.miniprofiler.com/).
* Questions on Stack Overflow are welcome using the [mvc-mini-profiler tag](https://stackoverflow.com/questions/tagged/mvc-mini-profiler).

#### License
MiniProfiler is licensed under the [MIT license](https://github.com/MiniProfiler/dotnet/blob/master/LICENSE.txt).