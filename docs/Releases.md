---
title: "Release Notes"
layout: "default"
---
### Release Notes
This page tracks major changes included in any update starting with version 4.0.0.3

#### Version 4.2.0 (In preview)
- Added `<script nonce="..." />` to rendering for CSP support ([#465](https://github.com/MiniProfiler/dotnet/pull/465))
- Added dark and "auto" (system preference decides) color themes, total is "Light", "Dark", and "Auto" ([#451](https://github.com/MiniProfiler/dotnet/pull/451))
  - Generally moves to CSS 3 variables, for easier custom themes as well.
- Added `SqlServerFormatter.IncludeParameterValues` for excluding actual values in output if desired ([#463](https://github.com/MiniProfiler/dotnet/pull/463))
- Fix for ['i.Started.toUTCString is not a function'](https://github.com/MiniProfiler/dotnet/pull/462) when global serializer options are changed.
- Removed jQuery (built-in) dependency ([#442](https://github.com/MiniProfiler/dotnet/pull/442))
  - Drops IE 11 support
- Fix for missing `IMemoryCache` depending on config ([#440](https://github.com/MiniProfiler/dotnet/pull/440))
- Updates `MySqlConnector` to 0.60.1 for misc fixes ([#432](https://github.com/MiniProfiler/dotnet/pull/432)) (thanks [@bgrainger](https://github.com/bgrainger)!)
- **.NET Core only**
  - Added `MiniProfilerOptions.ResultsAuthorizeAsync` and `MiniProfiler.ResultsAuthorizeListAsync` ([#472](https://github.com/MiniProfiler/dotnet/pull/472))


#### Version 4.1.0
- ASP.NET Core 3.0 support ([MiniProfiler.AspNetCore](https://www.nuget.org/packages/MiniProfiler.AspNetCore/) and [MiniProfiler.AspNetCore.Mvc](https://www.nuget.org/packages/MiniProfiler.AspNetCore.Mvc/) packages, now with a `netcoreapp3.0` build)
- Error support via `CustomTiming.Errored = true`, this will turn the UI red to raise error awareness ([#418](https://github.com/MiniProfiler/dotnet/pull/418) & [#420](https://github.com/MiniProfiler/dotnet/pull/420))
- Adds a `MiniProfiler.EFC7` (Entity Framework Classic 7) provider ([#397](https://github.com/MiniProfiler/dotnet/pull/397))
- Fix for [`.Close()`tracking](https://github.com/MiniProfiler/dotnet/commit/a7322be1d97be0720832ea9667105c0729d9343d)
- Drops `netstandard1.x` support ([#422](https://github.com/MiniProfiler/dotnet/pull/422))

#### Version 4.0.0
- ASP.NET Core 2.0+ support ([MiniProfiler.AspNetCore](https://www.nuget.org/packages/MiniProfiler.AspNetCore/) and [MiniProfiler.AspNetCore.Mvc](https://www.nuget.org/packages/MiniProfiler.AspNetCore.Mvc/) packages)
  - [Getting started docs](https://miniprofiler.com/dotnet/AspDotNetCore)
  - `<mini-profiler>` tag helper
  - `<profile name="My Step">` tag helper
- ASP.NET (non-Core) support ([MiniProfiler](https://www.nuget.org/packages/MiniProfiler/) and [MiniProfiler.Mvc5](https://www.nuget.org/packages/MiniProfiler.Mvc5/) packages)
  - [Getting Started docs](https://miniprofiler.com/dotnet/AspDotNet)
- Entity Framework Core (EFCore) support: [MiniProfiler.EntityFrameworkCore](https://www.nuget.org/packages/MiniProfiler.EntityFrameworkCore/)
- Full `async` support (correct timings)
- Multi-threaded access support (goes with `async`)
- `netstandard1.5` and `netstandard2.0` support
- [All libraries](https://www.nuget.org/packages?q=MiniProfiler+owner%3AStackExchange) are strongly named
- Client timings added to main UI for visual breakdown of requests
- Storage providers (optional - for persistent storage of MiniProfilers) added and updated:
  - MySQL: [MiniProfiler.Providers.MySql](https://www.nuget.org/packages/MiniProfiler.Providers.MySql/)
  - Redis: [MiniProfiler.Providers.Redis](https://www.nuget.org/packages/MiniProfiler.Providers.Redis/)
  - SQL Server: [MiniProfiler.Providers.SqlServer](https://www.nuget.org/packages/MiniProfiler.Providers.SqlServer/)
  - SQL Server CE: [MiniProfiler.Providers.SqlServerCe](https://www.nuget.org/packages/MiniProfiler.Providers.SqlServerCe/)
  - Sqlite: [MiniProfiler.Providers.Sqlite](https://www.nuget.org/packages/MiniProfiler.Providers.Sqlite/)

- **Major version breaking changes**
  - UI templating has been removed. The `share` and `includes` templates are now much more optimized code. Given the very few people customizing these, they certainly weren't worth the performance tradeoffs. The includes are now much smaller.
  - CSS class prefixes are now `mp-` instead of `profiler-` for fewer conflicts in styling.
  - A `Name` field has been added to all SQL storage providers.
  - Dropped .NET 4.5 support, due to lack of the framework bits needed to really make async profiling work correctly.
  - `MiniProfiler.Step()` and `MiniProfiler.StepIf()` methods now return `Timing` (the same previous underlying type) instead of `IDisposable`.
  - `IProfilerProvider` replaced with `IAsyncProfilerProvider` (which adds `StopAsync(bool discardResults)`).
  - `IStorage` replaced with `IAsyncStorage` (which adds `ListAsync`, `SaveAsync`, `LoadAsync`, `SetUnviewedAsync`, `SetViewedAsync`, and `GetUnviewedIdsAsync`).
  - `ProfiledDbCommand`, `ProfiledDbConnection`, and `SimpleProfiledCommand` no longer implement `ICloneable`.
  - `MiniProfiler.Settings.(AssembliesToExclude|TypesToExclude|MethodsToExclude)` changed from `IEnumerable<string>` to `HashSet<string>` (for performance).
  - `MiniProfiler.ToJson(MiniProfiler profiler)` is now `profiler.ToJson()` (instance method)
  - `[Obsolete]` methods removed:
    - `IProfilerProvider.Start(ProfileLevel level, string sessionName = null)`
    - `MiniProfiler.Settings.ExcludeStackTraceSnippetFromSqlTimings`
    - `MiniProfiler.Settings.UseExistingjQuery`
    - `MiniProfilerExtensions.Inline<T>(this MiniProfiler profiler, Func<T> selector, string name, ProfileLevel level)`
    - `MiniProfilerExtensions.Step(this MiniProfiler profiler, string name, ProfileLevel level)`
  - More information about v4.0 decisions can be found in [Issue #144](https://github.com/MiniProfiler/dotnet/issues/144).