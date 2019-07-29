---
layout: "default"
---
### How-To Profile Code

Once you've setup MiniProfiler, there are several ways to profile code. MiniProfiler is generally setup as 1 profiler per "action" (e.g. an HTTP Request, or startup, or some job) of an application. Inside that profiler, there are steps. Inside steps, you can also have custom timings. The general structure is:

* Profiler
  * Root Timing (generally unused, but it's there)
    * Timing/Step 1
      * Child Step a
      * Child Step b
        * Custom Timing
    * Timing/Step 2
      * Custom Timing

Whatever code path you're in, you're at the same place in the MiniProfiler tree you've created. How detailed the profile needs to be is totally up to you. In general, start with a little profiling, and add more detail where it's warranted by wrapping subsections of code inside anything expensive to narrow down problems.

There are several extension methods available on a MiniProfiler. Note these are **extension** methods, and do their own null checking internally. You don't need to null check every place you profile, calling `MiniProfiler.Current.<method>()` unconditionally is okay, regardless of if a profiler is running:

* `.Step(string name)`
  * Most common simple way to time a section of code
  * `name`: Name of the step you want to appear in the profile
* `.StepIf(string name, decimal minSaveMs, bool includeChildren = false)`
  * Same as `.Step()`, but only saves when over `minSaveMs`
  * `name`: Name of the step you want to appear in the profile
  * `minSaveMs`: The minimum time to take before this step is saved (e.g. if it's fast, leave it out)
  * `includeChildren`: Whether to include child time (vs. only self-time) in the `minSaveMs` calculation
* `.CustomTiming(string category, string commandString, string executeType = null)`
  * Adds a timing to a custom category (like SQL, redis, "mycustomengine", etc.)
  * `category`: The category to add this timing to, this is the column in the profiler popup
  * `commandString`: The string to show in the custom timing, e.g. the SQL query or a URL
  * `executeType`: The execute type to show in the profile list, e.g. `GET` for a URL or `EXECUTE` for some SQL
* `.CustomTimingIf(string category, string commandString, decimal minSaveMs, string executeType = null)`
  * Same as `.CustomTiming()`, but only saves when over `minSaveMs`
  * `category`: The category to add this timing to, this is the column in the profiler popup
  * `commandString`: The string to show in the custom timing, e.g. the SQL query or a URL
  * `minSaveMs`: The minimum time to take before this step is saved (e.g. if it's fast, leave it out)
  * `executeType`: The execute type to show in the profile list, e.g. `GET` for a URL or `EXECUTE` for some SQL
* `.Ignore()`
  * Silences a MiniProfiler for the duration, use in a `using` to silence for the duration
* `.AddProfilerResults(MiniProfiler externalProfiler)`
  * Appends another MiniProfiler tree to the current place in this tree, useful for returning a MiniProfiler from a background service as part of its result and showing the entire tree to the user in one view
  * `externalProfiler`: The child profiler to append
* `.AddCustomLink(string text, string url)`
  * Adds a custom link to the profiler a user sees
  * `text`: The text of the `<a>` to show in the profiler popup
  * `url`: The `href` of the `<a>` to show in the profiler popup


The primary way to use MiniProfiler is via [`using` statements](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-statement) ([VB.NET Link](https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/statements/using-statement)). The timing begins when created, and stops when disposed (at the end of the `using`). It generally looks like this:

```c#
using (MiniProfiler.Current.Step("InitUser"))
{
    var user = User.Get();
    user.Init();
}
```

In addition to steps, you can have custom timings that you want to attribute to custom services like SQL, Redis, or anything custom you have. These timings will show up in their own column and be summarized separately in the profiler popup. Like this:

```c#
var url = "https://google.com";
using (profiler.CustomTiming("http", "GET " + url))
{
    var client = new WebClient();
    var reply = client.DownloadString(url);
}
```

#### Helpers

There are also some more rarely used but handy helper extensions in MiniProfiler.

`.Inline<T>()` can be used for inline profiling of simple code without a `using`. It takes a `Func<T>` that returns a value, like this:

```c#
var url = "https://stackoverflow.com";
var html = MiniProfiler.Current.Inline(() => new WebClient().DownloadString(url), "Fetch Stack Overflow");
```

`.Ignore()` can be used to disable profiling for something we just don't want profiling on for whatever reason, like this:

```c#
using (MiniProfiler.Current.Ignore())
{
    // stuff we really don't care about - maybe a library that does a lot of profiling, etc.
    // in here, no timings would be recorded
}
```
