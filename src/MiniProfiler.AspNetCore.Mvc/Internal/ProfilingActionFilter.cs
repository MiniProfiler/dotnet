﻿using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;

namespace StackExchange.Profiling.Internal
{
    /// <summary>
    /// Internal MiniProfiler architecture, not meant for consumption.
    /// This can and probably will break without warning. Don't use the .Internal namespace directly.
    /// This filter can be applied globally to hook up automatic action profiling.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class ProfilingActionFilter : ActionFilterAttribute
    {
        private const string StackKey = "ProfilingActionFilterStack";

        /// <summary>
        /// Happens before the action starts running
        /// </summary>
        /// <param name="context">The filter context to handle the start of.</param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var mp = MiniProfiler.Current;
            if (mp != null)
            {
                var stack = context.HttpContext.Items[StackKey] as Stack<IDisposable>;
                if (stack == null)
                {
                    stack = new Stack<IDisposable>();
                    context.HttpContext.Items[StackKey] = stack;
                }

                var area = context.RouteData.DataTokens.TryGetValue("area", out object areaToken)
                    ? areaToken as string + "."
                    : null;

                switch (context.ActionDescriptor)
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
            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Happens after the action executes
        /// </summary>
        /// <param name="context">The filter context to handle the end of.</param>
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            if (context.HttpContext.Items[StackKey] is Stack<IDisposable> stack && stack.Count > 0)
            {
                stack.Pop().Dispose();
            }
        }
    }
}
