using System;
using System.Web.Mvc;
using StackExchange.Profiling;

namespace Samples.Mvc5.Controllers
{
    /// <summary>
    /// The base controller.
    /// </summary>
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// keep track of the profiler to dispose it.
        /// </summary>
        private IDisposable _resultExecutingToExecuted;

        /// <summary>
        /// on action executing.
        /// demonstrate using a base controller to intercept actions as they are executed.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var profiler = MiniProfiler.Current;

            using (profiler.Step("OnActionExecuting"))
            {
                //UpsertRouteHit(filterContext.ActionDescriptor, profiler);
                base.OnActionExecuting(filterContext);
            }
        }

        /// <summary>
        /// on result executing.
        /// demonstrate using a base controller to intercept actions as they are executed.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            _resultExecutingToExecuted = MiniProfiler.Current.Step("OnResultExecuting");

            base.OnResultExecuting(filterContext);
        }

        /// <summary>
        /// on result executed.
        /// demonstrate using a base controller to intercept actions as they are executed.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        protected override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            _resultExecutingToExecuted?.Dispose();

            base.OnResultExecuted(filterContext);
        }
    }
}
