## MiniProfiler for .NET (and .NET Standard)

[![Build status](https://ci.appveyor.com/api/projects/status/sieyhfuhjww5ur5i/branch/master?svg=true)](https://ci.appveyor.com/project/StackExchange/dotnet/branch/master)

* See the home page at: [miniprofiler.com](http://miniprofiler.com) for more info on how to set things up.
* We accept [pull requests](https://github.com/MiniProfiler/dotnet/pulls) here.
* Any issues can be reported in [GitHub Issues](https://github.com/MiniProfiler/dotnet/issues) or on the [Community Site](http://community.miniprofiler.com/).
* Questions on Stack Overflow are welcome using the [Mvc-Mini-Profiler tag](https://stackoverflow.com/questions/tagged/mvc-mini-profiler).

#### Minimum requirements
MiniProfiler v4 runs on .NET 4.6 and above or .NET Standard 1.5 and above. .NET 4.6+ is required due to all of the `async` support added in v4. If you are a version earlier than .NET 4.6, MiniProfiler v3.x is for you.

### Nuget Packages Available  
  * ASP.NET (Full .NET: `net46`+)
      * **[MiniProfiler](https://www.nuget.org/packages/MiniProfiler/)** - The core functionality (for full framework .NET applications)
      * [MiniProfiler.Mvc5](https://www.nuget.org/packages/MiniProfiler.Mvc5/) - ASP.NET MVC 5 Integration 
  * ASP.NET (.NET Standard: `netstandard1.5`+)
      * **[MiniProfiler.AspNetCore](https://www.nuget.org/packages/MiniProfiler.AspNetCore/)** - The core functionality (for .NET Standard applications)
      * [MiniProfiler.AspNetCore.Mvc](https://www.nuget.org/packages/MiniProfiler.AspNetCore.Mvc/) - ASP.NET Core MVC Integration 
  * [MiniProfiler.Shared](https://www.nuget.org/packages/MiniProfiler.Shared/) - Core, shared functionality for all platform-specific packages above
  * [MiniProfiler.EF6](https://www.nuget.org/packages/MiniProfiler.EF6/) - Entity Framework 6+ Integration
  *  Storage and Profiling Providers
      * [MiniProfiler.Providers.SqlServer](https://www.nuget.org/packages/MiniProfiler.Providers.SqlServer/) - SQL Server MiniProfiler Storage
      * [MiniProfiler.Providers.SqlServerCe](https://www.nuget.org/packages/MiniProfiler.Providers.SqlServerCe/) - SQL Server CS MiniProfiler Storage

In-progress (not yet available for v4):  
  * [MiniProfiler.Providers.EntityFrameworkCore](https://www.nuget.org/packages/MiniProfiler.Providers.EntityFrameworkCore/) - Entity Framework Core Integration
  * [MiniProfiler.Raven](https://www.nuget.org/packages/MiniProfiler.Raven/) - [RavenDb](https://ravendb.net) Integration

Alpha and Beta builds are available earlier via MyGet
  * NuGet v3 MyGet Feed: `https://www.myget.org/F/miniprofiler/api/v3/index.json` (Visual Studio 2015+)
  * NuGet v2 MyGet Feed: `https://www.myget.org/F/miniprofiler/api/v2` (Visual Studio 2012+)

### Providers
MiniProfiler is made of of several libraries, but you likely only need to reference 1 or 2 packages. For example:  
* If you're making a full framework ASP.NET MVC 5 application: you likely just want `MiniProfiler.Mvc5` (which references `MiniProfiler` and `MiniProfiler.Shared` beneath).
* If you're working on an ASP.NET Core application, you likely want `MiniProfiler.AspNetCore.Mvc` (which references `MiniProfiler.AspNetCore` and `MiniProfiler.Shared` beneath).
* If you want to store profilers *not* in memory, you can grab that provider package and set the provider in options. For example, to store MiniProfiler results in SQL Server:
   * 1. Reference the `MiniProfiler.Providers.SqlServer` NuGet package.
   * 2. Use it via `MiniProfiler.Settings.Storage = new SqlServerStorage(ConnectionString);`

### The following packages are no longer being actively worked on)
  *  v3 Only Packages
    * [MiniProfiler.EF5](https://www.nuget.org/packages/MiniProfiler.EF5/) - Entity Framework 4 and 5 Integration
    * [MiniProfiler.MongoDb](https://www.nuget.org/packages/MiniProfiler.MongoDb/) - MongoDB Integration
    * [MiniProfiler.MVC4](https://www.nuget.org/packages/MiniProfiler.Mvc4/) - ASP.NET MVC 4 Integration
    * [MiniProfiler.WCF](https://www.nuget.org/packages/MiniProfiler.WCF/) - WCF Integration
  * v2 Only Packages
    * [MiniProfiler.MVC3](https://www.nuget.org/packages/MiniProfiler.MVC3/) - ASP.NET MVC 3 Integration
	    * May [have issues](https://github.com/MiniProfiler/dotnet/issues/81) working with the EF6 nuget or other nugets requiring MiniProfiler v3 (like Raven and Mongo).

Licensed under [MIT license](LICENSE.txt)