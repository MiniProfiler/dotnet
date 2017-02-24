using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using StackExchange.Profiling.Helpers;

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
                var stack = filterContext.HttpContext.Items[StackKey] as Stack<IDisposable>;
                if (stack == null)
                {
                    stack = new Stack<IDisposable>();
                    filterContext.HttpContext.Items[StackKey] = stack;
                }

                var area = filterContext.RouteData.DataTokens.TryGetValue("area", out object areaToken)
                    ? areaToken as string + "."
                    : null;

                switch (filterContext.ActionDescriptor)
                {
                    case ControllerActionDescriptor cd:
                        if (mp.Name.IsNullOrWhiteSpace())
                        {
                            mp.Name = $"{cd.ControllerName}/{cd.MethodInfo.Name}";
                        }
                        stack.Push(mp.Step($"Controller: {area}{cd.ControllerName}.{cd.MethodInfo.Name}"));
                        break;
                    case ActionDescriptor ad:
                        if (mp.Name.IsNullOrWhiteSpace())
                        {
                            mp.Name = ad.DisplayName;
                        }
                        stack.Push(mp.Step($"Controller: {area}{ad.DisplayName}"));
                        break;
                }
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
            if (filterContext.HttpContext.Items[StackKey] is Stack<IDisposable> stack && stack.Count > 0)
            {
                stack.Pop().Dispose();
            }
        }
    }
}