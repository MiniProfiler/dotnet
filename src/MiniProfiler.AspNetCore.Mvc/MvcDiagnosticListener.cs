#if NETCOREAPP3_0
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Razor;
using StackExchange.Profiling.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// Diagnostic listener for Microsoft.AspNetCore.Mvc.* events
    /// </summary>
    /// <remarks>
    /// See:
    /// - https://github.com/dotnet/aspnetcore/blob/v3.0.0/src/Mvc/Mvc.Core/src/MvcCoreDiagnosticListenerExtensions.cs
    /// - https://github.com/dotnet/aspnetcore/blob/v3.0.0/src/Mvc/Mvc.Core/src/Diagnostics/MvcDiagnostics.cs
    /// - https://github.com/dotnet/aspnetcore/blob/v3.0.0/src/Mvc/Mvc.Razor/src/Diagnostics/MvcDiagnostics.cs
    /// </remarks>
    public class MvcDiagnosticListener : IMiniProfilerDiagnosticListener
    {
        /// <summary>
        /// Diagnostic Listener name to handle.
        /// </summary>
        public string ListenerName => "Microsoft.AspNetCore";

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public void OnCompleted() { }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error) => Trace.WriteLine(error);

        private static string GetName(string label, ActionDescriptor descriptor)
        {
            var controller = descriptor.RouteValues.TryGetValue("controller", out var c) ? c : "UnknownController";
            var action = descriptor.RouteValues.TryGetValue("action", out var a) ? a : "UnknownAction";

            // TODO: Don't allocate this string more than once
            return label + ": " + controller + "/" + action;
        }

        private static string GetName(IActionResult result) => result switch
        {
            ViewResult vr => vr.ViewName.HasValue() ? "View: " + vr.ViewName : "ViewResult",
            ContentResult cr => cr.ContentType.HasValue() ? "Content: " + cr.ContentType : "ContentResult",
            // TODO: Other main ones?
            _ => "Result: " + result.GetType().Name
        };

        private static string GetName(IFilterMetadata filter) => filter.GetType().Name;

        /// <summary>
        /// Stores the current timing in the tree, on each request.
        /// </summary>
        private readonly AsyncLocal<(object State, Timing Timing)> CurrentTiming = new AsyncLocal<(object, Timing)>();

        private object Start<T>(T state, string stepName) where T : class
        {
            CurrentTiming.Value = (state, MiniProfiler.Current.Step(stepName));
            return null;
        }

        private object Complete<T>(T state) where T : class
        {
            if (CurrentTiming.Value.State is T currentState && currentState == state)
            {
                using (CurrentTiming.Value.Timing) { }
            }
            return null;
        }

        /// <summary>
        /// Provides the observer with new data.
        /// </summary>
        /// <param name="kv">The current notification information.</param>
        public void OnNext(KeyValuePair<string, object> kv) => _ = kv.Value switch
        {
            // MVC Bits: https://github.com/dotnet/aspnetcore/blob/v3.0.0/src/Mvc/Mvc.Core/src/Diagnostics/MvcDiagnostics.cs
            // ActionEvent
            BeforeActionEventData data => Start(data.ActionDescriptor, GetName("Overall Action", data.ActionDescriptor)),
            AfterActionEventData data => Complete(data.ActionDescriptor),
            // ControllerActionMethod
            BeforeControllerActionMethodEventData data => Start(data.ActionContext.ActionDescriptor, GetName("Controller Action", data.ActionContext.ActionDescriptor)),
            AfterControllerActionMethodEventData data => Complete(data.ActionContext.ActionDescriptor),
            // ActionResultEvent
            BeforeActionResultEventData data => Start(data.Result, GetName(data.Result)),
            AfterActionResultEventData data => Complete(data.Result),

            // AuthorizationFilterOnAuthorization
            BeforeAuthorizationFilterOnAuthorizationEventData data => Start(data.Filter, "Auth Filter: " + GetName(data.Filter)),
            AfterAuthorizationFilterOnAuthorizationEventData data => Complete(data.Filter),

            // ResourceFilterOnResourceExecution
            BeforeResourceFilterOnResourceExecutionEventData data => Start(data.Filter, "Resource Filter (Exec): " + GetName(data.Filter)),
            AfterResourceFilterOnResourceExecutionEventData data => Complete(data.Filter),
            // ResourceFilterOnResourceExecuting
            BeforeResourceFilterOnResourceExecutingEventData data => Start(data.Filter, "Resource Filter (Execing): " + GetName(data.Filter)),
            AfterResourceFilterOnResourceExecutingEventData data => Complete(data.Filter),
            // ResourceFilterOnResourceExecuted
            BeforeResourceFilterOnResourceExecutedEventData data => Start(data.Filter, "Resource Filter (Execed): " + GetName(data.Filter)),
            AfterResourceFilterOnResourceExecutedEventData data => Complete(data.Filter),

            // ExceptionFilterOnException
            BeforeExceptionFilterOnException data => Start(data.Filter, "Exception Filter: " + GetName(data.Filter)),
            AfterExceptionFilterOnExceptionEventData data => Complete(data.Filter),

            // ActionFilterOnActionExecution
            BeforeActionFilterOnActionExecutionEventData data => Start(data.Filter, "Action Filter (Exec): " + GetName(data.Filter)),
            AfterActionFilterOnActionExecutionEventData data => Complete(data.Filter),
            // ActionFilterOnActionExecuting
            BeforeActionFilterOnActionExecutingEventData data => Start(data.Filter, "Action Filter (Execing): " + GetName(data.Filter)),
            AfterActionFilterOnActionExecutingEventData data => Complete(data.Filter),
            // ActionFilterOnActionExecuted
            BeforeActionFilterOnActionExecutedEventData data => Start(data.Filter, "Action Filter (Execed): " + GetName(data.Filter)),
            AfterActionFilterOnActionExecutedEventData data => Complete(data.Filter),

            // ResultFilterOnResultExecution
            BeforeResultFilterOnResultExecutionEventData data => Start(data.Filter, "Result Filter (Exec): " + GetName(data.Filter)),
            AfterResultFilterOnResultExecutionEventData data => Complete(data.Filter),
            // ResultFilterOnResultExecuting
            BeforeResultFilterOnResultExecutingEventData data => Start(data.Filter, "Result Filter (Execing): " + GetName(data.Filter)),
            AfterResultFilterOnResultExecutingEventData data => Complete(data.Filter),
            // ResultFilterOnResultExecuted
            BeforeResultFilterOnResultExecutedEventData data => Start(data.Filter, "Result Filter (Execed): " + GetName(data.Filter)),
            AfterResultFilterOnResultExecutedEventData data => Complete(data.Filter),

            // Razor Bits: https://github.com/dotnet/aspnetcore/blob/v3.0.0/src/Mvc/Mvc.Razor/src/Diagnostics/MvcDiagnostics.cs
            // ViewPage
            BeforeViewPageEventData data => Start(data.Page, "View: " + data.Page.Path),
            AfterViewPageEventData data => Complete(data.Page),

            // RazorPage Bits: https://github.com/dotnet/aspnetcore/blob/v3.0.0/src/Mvc/Mvc.RazorPages/src/Diagnostics/MvcDiagnostics.cs
            // HandlerMethod
            BeforeHandlerMethodEventData data => Start(data.Instance, "Handler: " + data.HandlerMethodDescriptor.Name),
            AfterHandlerMethodEventData data => Complete(data.Instance),

            // PageFilterOnPageHandlerExecution
            BeforePageFilterOnPageHandlerExecutionEventData data => Start(data.Filter, "Filter (Exec): " + GetName(data.Filter)),
            AfterPageFilterOnPageHandlerExecutionEventData data => Complete(data.Filter),
            // PageFilterOnPageHandlerExecuting
            BeforePageFilterOnPageHandlerExecutingEventData data => Start(data.Filter, "Filter (Execing): " + GetName(data.Filter)),
            AfterPageFilterOnPageHandlerExecutingEventData data => Complete(data.Filter),
            // PageFilterOnPageHandlerExecuted
            BeforePageFilterOnPageHandlerExecutedEventData data => Start(data.Filter, "Filter (Execed): " + GetName(data.Filter)),
            AfterPageFilterOnPageHandlerExecutedEventData data => Complete(data.Filter),

            // PageFilterOnPageHandlerSelection
            BeforePageFilterOnPageHandlerSelectionEventData data => Start(data.Filter, "Filter (Selection): " + GetName(data.Filter)),
            AfterPageFilterOnPageHandlerSelectionEventData data => Complete(data.Filter),
            // PageFilterOnPageHandlerSelected
            BeforePageFilterOnPageHandlerSelectedEventData data => Start(data.Filter, "Filter (Selected): " + GetName(data.Filter)),
            AfterPageFilterOnPageHandlerSelectedEventData data => Complete(data.Filter),
            _ => null
        };
    }
}
#endif
