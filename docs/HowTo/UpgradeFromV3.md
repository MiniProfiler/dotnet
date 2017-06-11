---
layout: "default"
---
### How-To Upgrade From MiniProfiler V3

MiniProfiler V4 has major breaking changes in layout compared to V3 due to needing to support both ASP.NET and ASP.NET Core.

#### Breaking Changes
* `RenderIncludes()` is now an instance method
  * Fix: change `MiniProfiler.RenderIncludes()` to `MiniProfiler.Current.RenderIncludes()`
* A `Name` field has been added to the SQL Server Profiler storage
  * Fix: Add a `nvarchar(200) null` field to your `MiniProfilers` table.
* .NET 4.6+ (or `netstandard1.5`) are required (due to lack of the framework bits needed to really make async profiling work correctly).
  * Fix: If you need .NET 4.5 and below support, continue to use MiniProfiler V3.
* `MiniProfiler.Step()` and `MiniProfiler.StepIf()` methods now return `Timing` (the same previous underlying type) instead of `IDisposable`.
  * Fix: this shouldn't require changes beyond a recompile, but adds functionality.
* `IProfilerProvider` replaced with [`IAsyncProfilerProvider`](https://github.com/MiniProfiler/dotnet/blob/master/src/MiniProfiler.Shared/ProfileProviders/IAsyncProfilerProvider.cs) (which adds `StopAsync(bool discardResults)`)
  * Fix: if you implemented your own provider, you'll need to change the interface and implement all [the new bits](https://github.com/MiniProfiler/dotnet/blob/master/src/MiniProfiler.Shared/ProfileProviders/IAsyncProfilerProvider.cs).
* `IStorage` replaced with `IAsyncStorage` (which adds `ListAsync`, `SaveAsync`, `LoadAsync`, `SetUnviewedAsync`, `SetViewedAsync`, and `GetUnviewedIdsAsync`)
  * Fix: if you implemented your own storage, you'll need to change the interface and implement all [the new bits](https://github.com/MiniProfiler/dotnet/blob/master/src/MiniProfiler.Shared/Storage/IAsyncStorage.cs).
* `ProfiledDbCommand`, `ProfiledDbConnection`, and `SimpleProfiledCommand` no longer implement `ICloneable` in `netstandard` (it doesn't exist there)
  * Fix: if you need this, please file an issue. The `ICloneable` interface is gone...so, yeah.
* `MiniProfiler.Settings.(AssembliesToExclude|TypesToExclude|MethodsToExclude)` changed from `IEnumerable<string>` to `HashSet<string>` (for performance)
  * Fix: if you're using these the access may need minor tweaks. If you were using `.Add()`, no change is necessary.
* `MiniProfiler.ToJson(MiniProfiler profiler)` is now `profiler.ToJson()` (instance method)
  * Fix: change `MiniProfiler.ToJson(myProfiler)` to `myProfiler.ToJson()`
* `IUserProvider` has been removed, it's now just a function on the settings.
  * Fix (ASP.NET): Use `MiniProfilerWebSettings.UserIdProvider = myFunc`
  * Fix (ASP.NET Core): Set `UserIdProvider` in `MiniProfilerOptions` in `Startup.cs`

#### Obsolete Things Removed in V4
* `IProfilerProvider.Start(ProfileLevel level, string sessionName = null)`
* `MiniProfiler.Settings.ExcludeStackTraceSnippetFromSqlTimings`
* `MiniProfiler.Settings.UseExistingjQuery`
* `MiniProfilerExtensions.Inline<T>(this MiniProfiler profiler, Func<T> selector, string name, ProfileLevel level)`
* `MiniProfilerExtensions.Step(this MiniProfiler profiler, string name, ProfileLevel level)`
* UI templating has been removed. While Stylesheets and the `.tmpl` are still replaceable, the `share` and `includes` templates are now much more optimized code. Given the very few people customizing these (was anyone using those pieces?), they certainly weren't worth the performance tradeoffs. If there's a loud demand for them to come back, we'll find a more efficient way to do it.