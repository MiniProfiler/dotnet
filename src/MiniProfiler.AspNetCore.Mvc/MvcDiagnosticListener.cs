#if NETCOREAPP3_0
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using StackExchange.Profiling.Internal;
using System;
using System.Collections.Concurrent;
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

        // So we don't keep allocating strings for the same actions over and over
        private readonly ConcurrentDictionary<(string, string), string> _descriptorNameCache = new ConcurrentDictionary<(string, string), string>();

        private string GetName(string label, ActionDescriptor descriptor)
        {
            var key = (label, descriptor.DisplayName);
            if (!_descriptorNameCache.TryGetValue(key, out var result))
            {
                // For the "Samples.AspNetCore.Controllers.HomeController.Index (Samples.AspNetCore3)" format,
                // ...trim off the assembly on the end.
                var assemblyNamePos = descriptor.DisplayName.IndexOf(" (");
                if (assemblyNamePos > 0)
                {
                    result = string.Concat(label, ": ", descriptor.DisplayName.AsSpan().Slice(0, assemblyNamePos));
                }
                else
                {
                    result = label + ": " + descriptor.DisplayName;
                }
                _descriptorNameCache[key] = result;
            }
            return result;
        }

        private static string GetName(IActionResult result) => result switch
        {
            ViewResult vr => vr.ViewName.HasValue() ? "View: " + vr.ViewName : nameof(ViewResult),
            ContentResult cr => cr.ContentType.HasValue() ? "Content: " + cr.ContentType : nameof(ContentResult),
            ObjectResult or => or.DeclaredType != null ? "Object: " + or.DeclaredType.Name : nameof(ObjectResult),
            StatusCodeResult scr => scr.StatusCode > 0 ? "Status Code: " + scr.StatusCode.ToString() : nameof(StatusCodeResult),
            JsonResult jr => jr.ContentType.HasValue() ? "JSON: " + jr.ContentType : nameof(JsonResult),
            _ => "Result: " + result.GetType().Name
        };

        private static string GetName(IFilterMetadata filter) => filter.GetType().Name;

        private class StackTiming
        {
            public object State { get; set; }
            public Timing Timing { get; set; }
            public StackTiming Previous { get; set; }

            public StackTiming(object state, Timing timing, StackTiming previous) =>
                (State, Timing, Previous) = (state, timing, previous);
        }

        /// <summary>
        /// Stores the current timing in the tree, on each request.
        /// </summary>
        private readonly AsyncLocal<StackTiming> CurrentTiming = new AsyncLocal<StackTiming>();

        private object StartFilter<T>(T state, string stepName) where T : class
        {
            var profiler = MiniProfiler.Current;
            if (profiler?.Options is MiniProfilerOptions opts && opts.EnableMvcFilterProfiling)
            {
                CurrentTiming.Value = new StackTiming(state, new Timing(profiler, profiler.Head, stepName, opts.MvcFilterMinimumSaveMs, true, debugStackShave: 4), CurrentTiming.Value);
            }
            return null;
        }

        private object StartView<T>(T state, string stepName) where T : class
        {
            var profiler = MiniProfiler.Current;
            if (profiler?.Options is MiniProfilerOptions opts && opts.EnableMvcViewProfiling)
            {
                CurrentTiming.Value = new StackTiming(state, new Timing(profiler, profiler.Head, stepName, opts.MvcViewMinimumSaveMs, true, debugStackShave: 4), CurrentTiming.Value);
            }
            return null;
        }

        private object Start<T>(T state, string stepName) where T : class
        {
            var profiler = MiniProfiler.Current;
            CurrentTiming.Value = new StackTiming(state, profiler != null ? new Timing(profiler, profiler.Head, stepName, null, null, debugStackShave: 4) : null, CurrentTiming.Value);
            return null;
        }

        private object Complete<T>(T state) where T : class
        {
            var top = CurrentTiming.Value;
            while (top?.Timing?.DurationMilliseconds.HasValue == true)
            {
                top = top.Previous;
            }
            if (top?.State is T currentState && currentState == state)
            {
                using (top.Timing) { }
                CurrentTiming.Value = top.Previous;
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
            BeforeActionEventData data => Start(data.ActionDescriptor, GetName("Action", data.ActionDescriptor)),
            AfterActionEventData data => Complete(data.ActionDescriptor),
            // ControllerActionMethod
            BeforeControllerActionMethodEventData data => Start(data.ActionContext.ActionDescriptor, GetName("Controller Action", data.ActionContext.ActionDescriptor)),
            AfterControllerActionMethodEventData data => Complete(data.ActionContext.ActionDescriptor),
            // ActionResultEvent
            BeforeActionResultEventData data => Start(data.Result, GetName(data.Result)),
            AfterActionResultEventData data => Complete(data.Result),

            // AuthorizationFilterOnAuthorization
            BeforeAuthorizationFilterOnAuthorizationEventData data => StartFilter(data.Filter, "Auth Filter: " + GetName(data.Filter)),
            AfterAuthorizationFilterOnAuthorizationEventData data => Complete(data.Filter),

            // ResourceFilterOnResourceExecution
            BeforeResourceFilterOnResourceExecutionEventData data => StartFilter(data.Filter, "Resource Filter (Exec): " + GetName(data.Filter)),
            AfterResourceFilterOnResourceExecutionEventData data => Complete(data.Filter),
            // ResourceFilterOnResourceExecuting
            BeforeResourceFilterOnResourceExecutingEventData data => StartFilter(data.Filter, "Resource Filter (Execing): " + GetName(data.Filter)),
            AfterResourceFilterOnResourceExecutingEventData data => Complete(data.Filter),
            // ResourceFilterOnResourceExecuted
            BeforeResourceFilterOnResourceExecutedEventData data => StartFilter(data.Filter, "Resource Filter (Execed): " + GetName(data.Filter)),
            AfterResourceFilterOnResourceExecutedEventData data => Complete(data.Filter),

            // ExceptionFilterOnException
            BeforeExceptionFilterOnException data => StartFilter(data.Filter, "Exception Filter: " + GetName(data.Filter)),
            AfterExceptionFilterOnExceptionEventData data => Complete(data.Filter),

            // ActionFilterOnActionExecution
            BeforeActionFilterOnActionExecutionEventData data => StartFilter(data.Filter, "Action Filter (Exec): " + GetName(data.Filter)),
            AfterActionFilterOnActionExecutionEventData data => Complete(data.Filter),
            // ActionFilterOnActionExecuting
            BeforeActionFilterOnActionExecutingEventData data => StartFilter(data.Filter, "Action Filter (Execing): " + GetName(data.Filter)),
            AfterActionFilterOnActionExecutingEventData data => Complete(data.Filter),
            // ActionFilterOnActionExecuted
            BeforeActionFilterOnActionExecutedEventData data => StartFilter(data.Filter, "Action Filter (Execed): " + GetName(data.Filter)),
            AfterActionFilterOnActionExecutedEventData data => Complete(data.Filter),

            // ResultFilterOnResultExecution
            BeforeResultFilterOnResultExecutionEventData data => StartFilter(data.Filter, "Result Filter (Exec): " + GetName(data.Filter)),
            AfterResultFilterOnResultExecutionEventData data => Complete(data.Filter),
            // ResultFilterOnResultExecuting
            BeforeResultFilterOnResultExecutingEventData data => StartFilter(data.Filter, "Result Filter (Execing): " + GetName(data.Filter)),
            AfterResultFilterOnResultExecutingEventData data => Complete(data.Filter),
            // ResultFilterOnResultExecuted
            BeforeResultFilterOnResultExecutedEventData data => StartFilter(data.Filter, "Result Filter (Execed): " + GetName(data.Filter)),
            AfterResultFilterOnResultExecutedEventData data => Complete(data.Filter),

            // Razor Bits: https://github.com/dotnet/aspnetcore/blob/v3.0.0/src/Mvc/Mvc.Razor/src/Diagnostics/MvcDiagnostics.cs
            // ViewPage
            BeforeViewPageEventData data => StartView(data.Page, "View: " + data.Page.Path),
            AfterViewPageEventData data => Complete(data.Page),

            // RazorPage Bits: https://github.com/dotnet/aspnetcore/blob/v3.0.0/src/Mvc/Mvc.RazorPages/src/Diagnostics/MvcDiagnostics.cs
            // HandlerMethod
            BeforeHandlerMethodEventData data => Start(data.Instance, "Handler: " + data.HandlerMethodDescriptor.Name),
            AfterHandlerMethodEventData data => Complete(data.Instance),

            // PageFilterOnPageHandlerExecution
            BeforePageFilterOnPageHandlerExecutionEventData data => StartFilter(data.Filter, "Filter (Exec): " + GetName(data.Filter)),
            AfterPageFilterOnPageHandlerExecutionEventData data => Complete(data.Filter),
            // PageFilterOnPageHandlerExecuting
            BeforePageFilterOnPageHandlerExecutingEventData data => StartFilter(data.Filter, "Filter (Execing): " + GetName(data.Filter)),
            AfterPageFilterOnPageHandlerExecutingEventData data => Complete(data.Filter),
            // PageFilterOnPageHandlerExecuted
            BeforePageFilterOnPageHandlerExecutedEventData data => StartFilter(data.Filter, "Filter (Execed): " + GetName(data.Filter)),
            AfterPageFilterOnPageHandlerExecutedEventData data => Complete(data.Filter),

            // PageFilterOnPageHandlerSelection
            BeforePageFilterOnPageHandlerSelectionEventData data => StartFilter(data.Filter, "Filter (Selection): " + GetName(data.Filter)),
            AfterPageFilterOnPageHandlerSelectionEventData data => Complete(data.Filter),
            // PageFilterOnPageHandlerSelected
            BeforePageFilterOnPageHandlerSelectedEventData data => StartFilter(data.Filter, "Filter (Selected): " + GetName(data.Filter)),
            AfterPageFilterOnPageHandlerSelectedEventData data => Complete(data.Filter),
            _ => null
        };
    }
}
#endif
