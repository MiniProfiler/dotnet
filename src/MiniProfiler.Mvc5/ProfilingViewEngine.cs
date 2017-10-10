using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace StackExchange.Profiling.Mvc
{
    /// <summary>
    /// You can wrap your view engines with this view to enable profiling on views and partial
    /// </summary>
    public class ProfilingViewEngine : IViewEngine
    {
        private readonly IViewEngine _wrapped;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilingViewEngine"/> class. 
        /// </summary>
        /// <param name="wrapped">The view engine to wrap in profiling.</param>
        public ProfilingViewEngine(IViewEngine wrapped)
        {
            _wrapped = wrapped;
        }

        private ViewEngineResult Find(string name, Func<ViewEngineResult> finder, bool isPartial)
        {
            var profiler = MiniProfiler.Current;
            IDisposable block = null;
            const string Key = "find-view-or-partial";

            if (profiler != null)
            {
                block = HttpContext.Current.Items[Key] as IDisposable;
                if (block == null)
                {
                    HttpContext.Current.Items[Key] = block = profiler.Step("Find: " + name);
                }
            }

            var found = finder();
            if (found?.View != null)
            {
                found = new ViewEngineResult(new WrappedView(found.View, name, isPartial), this);

                if (block != null)
                {
                    block.Dispose();
                    HttpContext.Current.Items[Key] = null;
                }
            }

            if (found == null && block != null && this == ViewEngines.Engines.Last())
            {
                block.Dispose();
                HttpContext.Current.Items[Key] = null;
            }

            return found;
        }

        /// <summary>
        /// Find a partial view
        /// </summary>
        /// <param name="controllerContext">The context to search for this partial with.</param>
        /// <param name="partialViewName">The view name to search for.</param>
        /// <param name="useCache">Whether to use cached lookups.</param>
        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache) =>
            Find(partialViewName, () => _wrapped.FindPartialView(controllerContext, partialViewName, useCache), isPartial: true);

        /// <summary>
        /// Find a full view
        /// </summary>
        /// <param name="controllerContext">The context to search for this view with.</param>
        /// <param name="viewName">The view name to search for.</param>
        /// <param name="masterName">The master view name.</param>
        /// <param name="useCache">Whether to use cached lookups.</param>
        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache) =>
            Find(viewName, () => _wrapped.FindView(controllerContext, viewName, masterName, useCache), isPartial: false);

        /// <summary>
        /// Release the rendered view
        /// </summary>
        /// <param name="controllerContext">The controller context the view is in.</param>
        /// <param name="view">The view to release.</param>
        public void ReleaseView(ControllerContext controllerContext, IView view) =>
            _wrapped.ReleaseView(controllerContext, view);
    }

    /// <summary>
    /// Extension methods for <see cref="ProfilingViewEngine"/>.
    /// </summary>
    public static class ProfilingViewEngineExtensions
    {
        /// <summary>
        /// Adds view profiling to MiniProfiler, so that view times show up in profilers.
        /// </summary>
        /// <param name="options">The options object to configure, only used for chaining.</param>
        public static MiniProfilerOptions AddViewPofiling(this MiniProfilerOptions options)
        {
            var copy = ViewEngines.Engines.ToList();
            ViewEngines.Engines.Clear();
            foreach (var item in copy)
            {
                ViewEngines.Engines.Add(new ProfilingViewEngine(item));
            }
            return options;
        }
    }
}
