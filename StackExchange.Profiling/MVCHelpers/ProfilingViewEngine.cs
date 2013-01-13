#if ASP_NET_MVC3
namespace StackExchange.Profiling.MVCHelpers
{
    using System;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;

    /// <summary>
    /// You can wrap your view engines with this view to enable profiling on views and partial
    /// </summary>
    public class ProfilingViewEngine : IViewEngine
    {
        /// <summary>
        /// The wrapped.
        /// </summary>
        private readonly IViewEngine _wrapped;

        /// <summary>
        /// Initialises a new instance of the <see cref="ProfilingViewEngine"/> class. 
        /// Wrap your view engines with this to allow profiling
        /// </summary>
        /// <param name="wrapped">the wrapped view engine.</param>
        public ProfilingViewEngine(IViewEngine wrapped)
        {
            this._wrapped = wrapped;
        }

        /// <summary>
        /// find the view engine.
        /// </summary>
        /// <param name="controllerContext">The controller context.</param>
        /// <param name="name">The name.</param>
        /// <param name="finder">The finder.</param>
        /// <param name="isPartial">The is partial.</param>
        /// <returns>the view engine result.</returns>
        private ViewEngineResult Find(ControllerContext controllerContext, string name, Func<ViewEngineResult> finder, bool isPartial)
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

                if (found != null && block != null)
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
        /// <param name="controllerContext">The controller Context.</param>
        /// <param name="partialViewName">The partial View Name.</param>
        /// <param name="useCache">The use Cache.</param>
        /// <returns>the view engine result.</returns>
        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            return Find(controllerContext, partialViewName, () => this._wrapped.FindPartialView(controllerContext, partialViewName, useCache), isPartial: true);
        }

        /// <summary>
        /// Find a view
        /// </summary>
        /// <param name="controllerContext">The controller Context.</param>
        /// <param name="viewName">The view Name.</param>
        /// <param name="masterName">The master Name.</param>
        /// <param name="useCache">The use Cache.</param>
        /// <returns>the view engine result.</returns>
        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            return Find(controllerContext, viewName, () => this._wrapped.FindView(controllerContext, viewName, masterName, useCache), isPartial: false);
        }

        /// <summary>
        /// Find a partial
        /// </summary>
        /// <param name="controllerContext">The controller Context.</param>
        /// <param name="view">The view.</param>
        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            this._wrapped.ReleaseView(controllerContext, view);
        }
    }
}
#endif