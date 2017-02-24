using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace StackExchange.Profiling.Mvc
{
    /// <summary>
    /// This filter can be applied globally to hook up automatic action profiling
    /// </summary>
    public class ProfilingActionFilter : ActionFilterAttribute
    {
        private const string StackKey = "ProfilingActionFilterStack";
        private static readonly char[] dotSplit = new[] { '.' };

        /// <summary>
        /// Happens before the action starts running
        /// </summary>
        /// <param name="filterContext">The filter context to handle the start of.</param>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var mp = MiniProfiler.Current;
            if (mp != null)
            {
                var stack = HttpContext.Current.Items[StackKey] as Stack<IDisposable>;
                if (stack == null)
                {
                    stack = new Stack<IDisposable>();
                    HttpContext.Current.Items[StackKey] = stack;
                }

                var ad = filterContext.ActionDescriptor;
                var area = filterContext.RouteData.DataTokens.TryGetValue("area", out object areaToken)
                    ? areaToken as string + "."
                    : null;

                stack.Push(mp.Step($"Controller: {area}{ad.ControllerDescriptor.ControllerName}.{ad.ActionName}"));
            }
            base.OnActionExecuting(filterContext);
        }

        /// <summary>
        /// Happens after the action executes
        /// </summary>
        /// <param name="filterContext">The filter context to handle the end of.</param>
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
            if (HttpContext.Current.Items[StackKey] is Stack<IDisposable> stack && stack.Count > 0)
            {
                stack.Pop().Dispose();
            }
        }
    }
}