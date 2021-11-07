---
title: "ASP.NET"
layout: "default"
---
### ASP.NET
If you'd rather learn by example, sample apps are available. [The ASP.NET MVC 5 sample is here](https://github.com/MiniProfiler/dotnet/tree/main/samples/Samples.Mvc5), with the important bits in [Global.asax.cs](https://github.com/MiniProfiler/dotnet/blob/main/samples/Samples.Mvc5/Global.asax.cs).

#### Installation and Configuration

* Install the NuGet Package: [MiniProfiler.Mvc5](https://www.nuget.org/packages/MiniProfiler.Mvc5/)
   * Either use the NuGet UI to install `MiniProfiler.Mvc5` (which has all needed dependencies)
   * Or use the Package Manager Console:

```ps
Install-Package MiniProfiler.Mvc5 -IncludePrerelease
```

* Edit your `Global.asax` to configure MiniProfiler in `Application_Start` and profile requests:

```c#
protected void Application_Start()
{
    MiniProfiler.Configure(new MiniProfilerOptions
    {
        // Sets up the route to use for MiniProfiler resources:
        // Here, ~/profiler is used for things like /profiler/mini-profiler-includes.js
        RouteBasePath = "~/profiler",

        // Example of using SQLite storage instead
        Storage = new SqliteMiniProfilerStorage(ConnectionString),

        // Different RDBMS have different ways of declaring sql parameters - SQLite can understand inline sql parameters just fine.
        // By default, sql parameters will be displayed.
        //SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter(),

        // These settings are optional and all have defaults, any matching setting specified in .RenderIncludes() will
        // override the application-wide defaults specified here, for example if you had both:
        //    PopupRenderPosition = RenderPosition.Right;
        //    and in the page:
        //    @MiniProfiler.Current.RenderIncludes(position: RenderPosition.Left)
        // ...then the position would be on the left on that page, and on the right (the application default) for anywhere that doesn't
        // specified position in the .RenderIncludes() call.
        PopupRenderPosition = RenderPosition.Right,  // defaults to left
        PopupMaxTracesToShow = 10,                   // defaults to 15
        PopupDecimalPlaces = 1,                      // defaults to 2
        ColorScheme = ColorScheme.Auto,              // defaults to light

        // ResultsAuthorize (optional - open to all by default):
        // because profiler results can contain sensitive data (e.g. sql queries with parameter values displayed), we
        // can define a function that will authorize clients to see the JSON or full page results.
        // we use it on http://stackoverflow.com to check that the request cookies belong to a valid developer.
        ResultsAuthorize = request => request.IsLocal,

        // ResultsListAuthorize (optional - open to all by default)
        // the list of all sessions in the store is restricted by default, you must return true to allow it
        ResultsListAuthorize = request =>
        {
            // you may implement this if you need to restrict visibility of profiling lists on a per request basis
            return true; // all requests are legit in this example
        },

        // Stack trace settings
        StackMaxLength = 256, // default is 120 characters
        
        // (Optional) You can disable "Connection Open()", "Connection Close()" (and async variant) tracking.
        // (defaults to true, and connection opening/closing is tracked)
        TrackConnectionOpenClose = true
    }
    // Optional settings to control the stack trace output in the details pane, examples:
    .ExcludeType("SessionFactory")  // Ignore any class with the name of SessionFactory)
    .ExcludeAssembly("NHibernate")  // Ignore any assembly named NHibernate
    .ExcludeMethod("Flush")         // Ignore any method with the name of Flush
    .AddViewProfiling()              // Add MVC view profiling (you want this)
    // If using EntityFrameworkCore, here's where it'd go.
    // .AddEntityFramework()        // Extension method in the MiniProfiler.EntityFrameworkCore package
    );

    // If we're using EntityFramework 6, here's where it'd go.
    // This is in the MiniProfiler.EF6 NuGet package.
    // MiniProfilerEF6.Initialize();
}

protected void Application_BeginRequest()
{
    // You can decide whether to profile here, or it can be done in ActionFilters, etc.
    // We're doing it here so profiling happens ASAP to account for as much time as possible.
    if (Request.IsLocal) // Example of conditional profiling, you could just call MiniProfiler.StartNew();
    {
        MiniProfiler.StartNew();
    }
}

protected void Application_EndRequest()
{
    MiniProfiler.Current?.Stop(); // Be sure to stop the profiler!
}
```

* Edit your `Views\Shared\_Layout.cshtml` to render the MiniProfiler:
  ```
  @using StackExchange.Profiling
  ...
  @(MiniProfiler.Current?.RenderIncludes())
  </body>
  </html>
  ```

* Depending on existing config, you may need to edit your `Web.config` to serve the resources, the `path` attribute should match `RouteBasePath`:
  ```xml
  <configuration>
    <system.webServer>
      <handlers>
        <add name="MiniProfiler" path="profiler/*" verb="*" type="System.Web.Routing.UrlRoutingModule" resourceType="Unspecified" preCondition="integratedMode" />
      </handlers>
    </system.webServer>
  </configuration>
  ```

#### Routes

There are 2 user endpoints for MiniProfiler. The root is determined by `MiniProfilerOptions.RouteBasePath` (defaults to `/mini-profiler-resources`, but can be changed):
- `/<base>/results-index`: A list of recent profilers, authorization required via `.ResultsAuthorize` and `.ResultsListAuthorize`
- `/<base>/results`: Views either the very last profiler for the current user or a specific profiler via `?id={guid}`, authorization required via `.ResultsAuthorize`
