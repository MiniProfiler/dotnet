using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

#if ASP_NET_MVC3
namespace StackExchange.Profiling.MVCHelpers
{
    /// <summary>
    /// This filter can be applied globally to hook up automatic action profiling
    /// </summary>
    public class ProfilingActionFilter : ActionFilterAttribute
    {
        /// <summary>
        /// The stack key.
        /// </summary>
        private const string StackKey = "ProfilingActionFilterStack";

        /// <summary>
        /// Happens before the action starts running
        /// </summary>
        /// <param name="filterContext">The filter Context.</param>
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

                var profiler = MiniProfiler.Current;
                if (profiler != null)
                {
                    var tokens = filterContext.RouteData.DataTokens;
                    string area = tokens.ContainsKey("area") && !string.IsNullOrEmpty((string)tokens["area"]) ?
                        tokens["area"] + "." : string.Empty;
                    string controller = filterContext.Controller.ToString().Split('.').Last() + ".";
                    string action = filterContext.ActionDescriptor.ActionName;

                    stack.Push(profiler.Step("Controller: " + area + controller + action));
                }
            
            }
            base.OnActionExecuting(filterContext);
        }

        /// <summary>
        /// Happens after the action executes
        /// </summary>
        /// <param name="filterContext">The filter Context.</param>
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
            var stack = HttpContext.Current.Items[StackKey] as Stack<IDisposable>;
            if (stack != null && stack.Count > 0)
            {
                stack.Pop().Dispose();
            }
        }
    }
}
#endif