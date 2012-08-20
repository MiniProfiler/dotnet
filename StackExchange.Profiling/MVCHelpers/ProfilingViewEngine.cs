#if ASP_NET_MVC3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web;

namespace StackExchange.Profiling.MVCHelpers
{
    /// <summary>
    /// Wrapped MVC View that ProfilingViewEngine uses to log profiling data
    /// </summary>
    public class WrappedView : IView
    {
        /// <summary>
        /// MVC IView that is wrapped by the ProfilingViewEngine
        /// </summary>
        public IView wrapped;
        /// <summary>
        /// ViewName of wrapped View
        /// </summary>
        public string name;
        /// <summary>
        /// Flag as to whether wrapped is a PartialView
        /// </summary>
        public bool isPartial;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="wrapped">IView to wrap</param>
        /// <param name="name">Name/Path to view</param>
        /// <param name="isPartial">Whether view is Partial</param>
        public WrappedView(IView wrapped, string name, bool isPartial)
        {
            this.wrapped = wrapped;
            this.name = name;
            this.isPartial = isPartial;
        }

        /// <summary>
        /// Renders the WrappedView and logs profiling data
        /// </summary>
        /// <param name="viewContext"></param>
        /// <param name="writer"></param>
        public void Render(ViewContext viewContext, System.IO.TextWriter writer)
        {
            using (MiniProfiler.Current.Step("Render " + (isPartial ? "partial" : "") + ": " + name))
            {
                wrapped.Render(viewContext, writer);
            }
        }
    }
    
    /// <summary>
    /// You can wrap your view engines with this view to enable profiling on views and partial
    /// </summary>
    public class ProfilingViewEngine : IViewEngine
    {
        IViewEngine wrapped;

        /// <summary>
        /// Wrap your view engines with this to allow profiling
        /// </summary>
        /// <param name="wrapped"></param>
        public ProfilingViewEngine(IViewEngine wrapped)
        {
            this.wrapped = wrapped;
        }


        private ViewEngineResult Find(ControllerContext controllerContext, string name, Func<ViewEngineResult> finder, bool isPartial)
        {
            var profiler = MiniProfiler.Current;
            IDisposable block = null;
            var key = "find-view-or-partial";

            if (profiler != null)
            {
                block = HttpContext.Current.Items[key] as IDisposable;
                if (block == null)
                {
                    HttpContext.Current.Items[key] = block = profiler.Step("Find: " + name);
                }
            }

            var found = finder();
            if (found != null && found.View != null)
            {
                found = new ViewEngineResult(new WrappedView(found.View, name, isPartial: isPartial), this);

                if (found != null && block != null)
                {
                    block.Dispose();
                    HttpContext.Current.Items[key] = null;
                }
            }

            if (found == null && block != null && this == ViewEngines.Engines.Last())
            {
                block.Dispose();
                HttpContext.Current.Items[key] = null;
            }

            return found;
        }


        /// <summary>
        /// Find a partial
        /// </summary>
        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            return Find(controllerContext, partialViewName, () => wrapped.FindPartialView(controllerContext, partialViewName, useCache), isPartial: true);
        }

        /// <summary>
        /// Find a view
        /// </summary>
        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            return Find(controllerContext, viewName, () => wrapped.FindView(controllerContext, viewName, masterName, useCache), isPartial: false);
        }

        /// <summary>
        /// Find a partial
        /// </summary>
        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            wrapped.ReleaseView(controllerContext, view);
        }
    }
}

#endif