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
        /// Initialises a new instance of the <see cref="ProfilingViewEngine"/> class. 
        /// </summary>
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
            if (found != null && found.View != null)
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
        /// Find a partial
        /// </summary>
        public ViewEngineResult FindPartialView(ControllerContext ctx, string partialViewName, bool useCache)
        {
            return Find(partialViewName, () => _wrapped.FindPartialView(ctx, partialViewName, useCache), isPartial: true);
        }

        /// <summary>
        /// Find a view
        /// </summary>
        public ViewEngineResult FindView(ControllerContext ctx, string viewName, string masterName, bool useCache)
        {
            return Find(viewName, () => _wrapped.FindView(ctx, viewName, masterName, useCache), isPartial: false);
        }

        /// <summary>
        /// Find a partial
        /// </summary>
        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            _wrapped.ReleaseView(controllerContext, view);
        }
    }
}