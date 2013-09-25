using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Happens before the action starts running
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext ctx)
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

                var tokens = ctx.RouteData.DataTokens;
                string area = tokens.ContainsKey("area") && !string.IsNullOrWhiteSpace(((string)tokens["area"]))
                    ? tokens["area"] + "."
                    : "";
                string controller = ctx.Controller.ToString().Split('.').Last() + ".";
                string action = ctx.ActionDescriptor.ActionName;

                stack.Push(mp.Step("Controller: " + area + controller + action));
            }
            base.OnActionExecuting(ctx);
        }

        /// <summary>
        /// Happens after the action executes
        /// </summary>
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