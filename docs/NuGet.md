---
title: "NuGet Packages"
layout: "default"
---
### NuGet Packages
MiniProfiler is split into the NuGet packages so you can easily select the bits you need for profiling the things you use and want to profile. It's likely you just want 1 or 2 packages.

#### Examples
* If you're making a full framework ASP.NET MVC 5 application: you likely just want [`MiniProfiler.Mvc5`](https://www.nuget.org/packages/MiniProfiler.Mvc5/) (which references [`MiniProfiler`](https://www.nuget.org/packages/MiniProfiler/) and [`MiniProfiler.Shared`](https://www.nuget.org/packages/MiniProfiler.Shared/) beneath).
* If you're working on an ASP.NET Core application, you likely want [`MiniProfiler.AspNetCore.Mvc`](https://www.nuget.org/packages/MiniProfiler.AspNetCore.Mvc/) (which references [`MiniProfiler.AspNetCore`](https://www.nuget.org/packages/MiniProfiler.AspNetCore/) and [`MiniProfiler.Shared`](https://www.nuget.org/packages/MiniProfiler.Shared/) beneath).
* If you want to store profilers *not* in memory, you can grab that provider package and set the provider in options. [See below](#shared-packages) for available providers. For example, to store MiniProfiler results in SQL Server:
  1. Reference the [`MiniProfiler.Providers.SqlServer`](https://www.nuget.org/packages/MiniProfiler.Providers.SqlServer) NuGet package.
  2. Use it via `MiniProfiler.Settings.Storage = new SqlServerStorage(ConnectionString);`

#### NuGet Package list for ASP.NET Core (.NET Standard: `netstandard1.5`+)
* [MiniProfiler.AspNetCore](https://www.nuget.org/packages/MiniProfiler.AspNetCore/) - The core functionality (for .NET Standard applications)
* [MiniProfiler.AspNetCore.Mvc](https://www.nuget.org/packages/MiniProfiler.AspNetCore.Mvc/) - ASP.NET Core MVC Integration 

#### NuGet Package list for ASP.NET (Full Framework)
* [MiniProfiler](https://www.nuget.org/packages/MiniProfiler/) - The core functionality (for full framework .NET applications)
* [MiniProfiler.Mvc5](https://www.nuget.org/packages/MiniProfiler.Mvc5/) - ASP.NET MVC 5 Integration 


#### Shared Packages
* [MiniProfiler.Shared](https://www.nuget.org/packages/MiniProfiler.Shared/) - Core, shared functionality for all platform-specific packages above
* [MiniProfiler.EntityFrameworkCore](https://www.nuget.org/packages/MiniProfiler.EntityFrameworkCore) - Entity Framework Core Integration
* [MiniProfiler.EF6](https://www.nuget.org/packages/MiniProfiler.EF6/) - Entity Framework 6+ Integration
*  Storage and Profiling Providers
   * [MiniProfiler.Providers.MongoDB](https://www.nuget.org/packages/MiniProfiler.Providers.MongoDB/) - MongoDB MiniProfiler Storage
   * [MiniProfiler.Providers.MySql](https://www.nuget.org/packages/MiniProfiler.Providers.MySql/) - MySQL MiniProfiler Storage
   * [MiniProfiler.Providers.Redis](https://www.nuget.org/packages/MiniProfiler.Providers.Redis/) - Redis MiniProfiler Storage
   * [MiniProfiler.Providers.Sqlite](https://www.nuget.org/packages/MiniProfiler.Providers.Sqlite/) - SQLite MiniProfiler Storage
   * [MiniProfiler.Providers.SqlServer](https://www.nuget.org/packages/MiniProfiler.Providers.SqlServer/) - SQL Server MiniProfiler Storage
   * [MiniProfiler.Providers.SqlServerCe](https://www.nuget.org/packages/MiniProfiler.Providers.SqlServerCe/) - SQL Server CS MiniProfiler Storage

#### Early Access
Alpha and Beta builds are available earlier via MyGet. These are built on every commit.

* NuGet v3 MyGet Feed: [`https://www.myget.org/F/miniprofiler/api/v3/index.json`](https://www.myget.org/F/miniprofiler/api/v3/index.json) (Visual Studio 2015+)
* NuGet v2 MyGet Feed: [`https://www.myget.org/F/miniprofiler/api/v2`](https://www.myget.org/F/miniprofiler/api/v2) (Visual Studio 2012+)

#### Older MiniProfiler Packages
The following packages are no longer being actively worked on):

* MiniProfiler v3 Packages
   * [MiniProfiler.EF5](https://www.nuget.org/packages/MiniProfiler.EF5/) - Entity Framework 4 and 5 Integration
   * [MiniProfiler.MongoDb](https://www.nuget.org/packages/MiniProfiler.MongoDb/) - MongoDB Integration
   * [MiniProfiler.MVC4](https://www.nuget.org/packages/MiniProfiler.Mvc4/) - ASP.NET MVC 4 Integration
   * [MiniProfiler.WCF](https://www.nuget.org/packages/MiniProfiler.WCF/) - WCF Integration
* MiniProfiler v2 Packages
   * [MiniProfiler.MVC3](https://www.nuget.org/packages/MiniProfiler.MVC3/) - ASP.NET MVC 3 Integration
     * May [have issues](https://github.com/MiniProfiler/dotnet/issues/81) working with the EF6 nuget or other nugets requiring MiniProfiler v3 (like Raven and Mongo).