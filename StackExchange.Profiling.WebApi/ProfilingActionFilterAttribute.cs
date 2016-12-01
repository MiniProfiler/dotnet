using System;
using System.Collections.Generic;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace StackExchange.Profiling.WebApi
{
    /// <summary>
    /// This filter can be applied globally to hook up automatic action profiling by adding it to HttpConfiguration.Filters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ProfilingActionFilterAttribute : ActionFilterAttribute
    {
        private const string StackKey = "ProfilingActionFilterStack";

        /// <summary>
        /// Called by the ASP.NET WebApi framework after the action method executes.
        /// </summary>
        /// <param name="actionExecutedContext">The action executed context.</param>
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            Stack<IDisposable> stack = null;

            object storedStack;

            if (actionExecutedContext.Request.Properties.TryGetValue(StackKey, out storedStack))
            {
                stack = storedStack as Stack<IDisposable>;
            }

            if (stack != null && stack.Count > 0)
            {
                stack.Pop().Dispose();
            }

            base.OnActionExecuted(actionExecutedContext);
        }

        /// <summary>
        /// Called by the ASP.NET WebApi framework before the action method executes.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var profiler = MiniProfiler.Current;

            if (profiler != null)
            {
                Stack<IDisposable> stack = null;

                object storedStack;

                if (actionContext.Request.Properties.TryGetValue(StackKey, out storedStack))
                {
                    stack = storedStack as Stack<IDisposable>;
                }

                stack = stack ?? new Stack<IDisposable>();

                actionContext.Request.Properties[StackKey] = stack;

                var stepName = actionContext.ControllerContext.ControllerDescriptor.ControllerName
                    + "."
                    + actionContext.ActionDescriptor.ActionName;

                stack.Push(profiler.Step(stepName));
            }

            base.OnActionExecuting(actionContext);
        }
    }
}