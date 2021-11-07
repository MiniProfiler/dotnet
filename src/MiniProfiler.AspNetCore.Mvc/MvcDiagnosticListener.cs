#if NETCOREAPP3_1
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
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
        public void OnCompleted()
        {
            _descriptorNameCache.Clear();
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error) => Trace.WriteLine(error);

        // So we don't keep allocating strings for the same actions over and over
        private readonly ConcurrentDictionary<(string, string), string> _descriptorNameCache = new();
        private const string _labelSeparator = ": ";

        /// <summary>
        /// Gets a cached concatenation since this is such a hot path - don't keep allocating.
        /// </summary>
        private string GetName(string label, string name, Func<string, string> trim = null)
        {
            var key = (label, name);
            if (!_descriptorNameCache.TryGetValue(key, out var result))
            {
                name = trim?.Invoke(name) ?? name;
                result = label + _labelSeparator + name;
                _descriptorNameCache[key] = result;
            }
            return result;
        }

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
        private readonly AsyncLocal<StackTiming> CurrentTiming = new();

        private object StartAction<T>(string label, T descriptor) where T : ActionDescriptor
        {
            var profiler = MiniProfiler.Current;
            var stepName = GetName(label, descriptor.DisplayName, name =>
            {
                // For the "Samples.AspNetCore.Controllers.HomeController.Index (Samples.AspNetCore3)" format,
                // ...trim off the assembly on the end.
                var assemblyNamePos = name.IndexOf(" (");
                return assemblyNamePos > 0
                        ? descriptor.DisplayName[..assemblyNamePos]
                        : descriptor.DisplayName;
            });

            CurrentTiming.Value = new StackTiming(descriptor, profiler != null ? new Timing(profiler, profiler.Head, stepName, null, null, debugStackShave: 4) : null, CurrentTiming.Value);
            return null;
        }

        private object StartActionResult<T>(T result) where T : IActionResult
        {
            var profiler = MiniProfiler.Current;
            var stepName = result switch
            {
                ViewResult vr => vr.ViewName.HasValue() ? GetName("View", vr.ViewName) : nameof(ViewResult),
                ContentResult cr => cr.ContentType.HasValue() ? GetName("Content", cr.ContentType) : nameof(ContentResult),
                ObjectResult or => or.DeclaredType != null ? GetName("Object", or.DeclaredType.Name) : nameof(ObjectResult),
                StatusCodeResult scr => scr.StatusCode > 0 ? GetName("Status Code", scr.StatusCode.ToString()) : nameof(StatusCodeResult),
                JsonResult jr => jr.ContentType.HasValue() ? GetName("JSON", jr.ContentType) : nameof(JsonResult),
                _ => GetName("Result", result.GetType().Name)
            };

            CurrentTiming.Value = new StackTiming(result, profiler != null ? new Timing(profiler, profiler.Head, stepName, null, null, debugStackShave: 4) : null, CurrentTiming.Value);
            return null;
        }

        private object StartFilter<T>(string label, T filter) where T : IFilterMetadata
        {
            var profiler = MiniProfiler.Current;
            if (profiler?.Options is MiniProfilerOptions opts && opts.EnableMvcFilterProfiling)
            {
                var stepName = GetName(label, filter.GetType().Name, name => name.EndsWith("Attribute") ? name[..^"Attribute".Length] : name);
                CurrentTiming.Value = new StackTiming(filter, new Timing(profiler, profiler.Head, stepName, opts.MvcFilterMinimumSaveMs, true, debugStackShave: 4), CurrentTiming.Value);
            }
            return null;
        }

        private object StartView<T>(T state, string label, string viewName) where T : class
        {
            var profiler = MiniProfiler.Current;
            if (profiler?.Options is MiniProfilerOptions opts && opts.EnableMvcViewProfiling)
            {
                // Trim /Views/ to / for brevity
                var stepName = GetName(label, viewName, name => name?.StartsWith("/Views/") == true ? name["/Views/".Length..] : name);
                CurrentTiming.Value = new StackTiming(state, new Timing(profiler, profiler.Head, stepName, opts.MvcViewMinimumSaveMs, true, debugStackShave: 4), CurrentTiming.Value);
            }
            return null;
        }

        private object StartHandler<T>(T state, HandlerMethodDescriptor handler) where T : class
        {
            var profiler = MiniProfiler.Current;
            var stepName = GetName("Handler", handler.Name);
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
        public void OnNext(KeyValuePair<string, object> kv)
        {
            try
            {
                _ = kv.Value switch
                {
                    // MVC Bits: https://github.com/dotnet/aspnetcore/blob/v3.0.0/src/Mvc/Mvc.Core/src/Diagnostics/MvcDiagnostics.cs
                    // ActionEvent
                    BeforeActionEventData data => StartAction("Action", data.ActionDescriptor),
                    AfterActionEventData data => Complete(data.ActionDescriptor),
                    // ControllerActionMethod
                    BeforeControllerActionMethodEventData data => StartAction("Controller Action", data.ActionContext.ActionDescriptor),
                    AfterControllerActionMethodEventData data => Complete(data.ActionContext.ActionDescriptor),
                    // ActionResultEvent
                    BeforeActionResultEventData data => StartActionResult(data.Result),
                    AfterActionResultEventData data => Complete(data.Result),

                    // AuthorizationFilterOnAuthorization
                    BeforeAuthorizationFilterOnAuthorizationEventData data => StartFilter("Auth Filter", data.Filter),
                    AfterAuthorizationFilterOnAuthorizationEventData data => Complete(data.Filter),

                    // ResourceFilterOnResourceExecution
                    BeforeResourceFilterOnResourceExecutionEventData data => StartFilter("Resource Filter (Exec)", data.Filter),
                    AfterResourceFilterOnResourceExecutionEventData data => Complete(data.Filter),
                    // ResourceFilterOnResourceExecuting
                    BeforeResourceFilterOnResourceExecutingEventData data => StartFilter("Resource Filter (Execing)", data.Filter),
                    AfterResourceFilterOnResourceExecutingEventData data => Complete(data.Filter),
                    // ResourceFilterOnResourceExecuted
                    BeforeResourceFilterOnResourceExecutedEventData data => StartFilter("Resource Filter (Execed)", data.Filter),
                    AfterResourceFilterOnResourceExecutedEventData data => Complete(data.Filter),

                    // ExceptionFilterOnException
                    BeforeExceptionFilterOnException data => StartFilter("Exception Filter", data.Filter),
                    AfterExceptionFilterOnExceptionEventData data => Complete(data.Filter),

                    // ActionFilterOnActionExecution
                    BeforeActionFilterOnActionExecutionEventData data => StartFilter("Action Filter (Exec)", data.Filter),
                    AfterActionFilterOnActionExecutionEventData data => Complete(data.Filter),
                    // ActionFilterOnActionExecuting
                    BeforeActionFilterOnActionExecutingEventData data => StartFilter("Action Filter (Execing)", data.Filter),
                    AfterActionFilterOnActionExecutingEventData data => Complete(data.Filter),
                    // ActionFilterOnActionExecuted
                    BeforeActionFilterOnActionExecutedEventData data => StartFilter("Action Filter (Execed)", data.Filter),
                    AfterActionFilterOnActionExecutedEventData data => Complete(data.Filter),

                    // ResultFilterOnResultExecution
                    BeforeResultFilterOnResultExecutionEventData data => StartFilter("Result Filter (Exec)", data.Filter),
                    AfterResultFilterOnResultExecutionEventData data => Complete(data.Filter),
                    // ResultFilterOnResultExecuting
                    BeforeResultFilterOnResultExecutingEventData data => StartFilter("Result Filter (Execing)", data.Filter),
                    AfterResultFilterOnResultExecutingEventData data => Complete(data.Filter),
                    // ResultFilterOnResultExecuted
                    BeforeResultFilterOnResultExecutedEventData data => StartFilter("Result Filter (Execed)", data.Filter),
                    AfterResultFilterOnResultExecutedEventData data => Complete(data.Filter),

                    // Razor Bits: https://github.com/dotnet/aspnetcore/blob/v3.0.0/src/Mvc/Mvc.Razor/src/Diagnostics/MvcDiagnostics.cs
                    // ViewPage
                    BeforeViewPageEventData data => StartView(data.Page, "View", data.Page.Path),
                    AfterViewPageEventData data => Complete(data.Page),

                    // ViewComponent
                    BeforeViewComponentEventData data => StartView(data.ViewComponentContext, "Component (Invoke)", data.ViewComponentContext.ViewComponentDescriptor.ShortName),
                    AfterViewComponentEventData data => Complete(data.ViewComponentContext),

                    // Intentionally not registering to slim view wrapper due to noise, accounted for in View: above.
                    //ViewComponentBeforeViewExecuteEventData data => StartView(data.ViewComponentContext, "Component (View)", data.ViewComponentContext.ViewComponentDescriptor.ShortName),
                    //ViewComponentAfterViewExecuteEventData data => Complete(data.ViewComponentContext),

                    // RazorPage Bits: https://github.com/dotnet/aspnetcore/blob/v3.0.0/src/Mvc/Mvc.RazorPages/src/Diagnostics/MvcDiagnostics.cs
                    // HandlerMethod
                    BeforeHandlerMethodEventData data => StartHandler(data.Instance, data.HandlerMethodDescriptor),
                    AfterHandlerMethodEventData data => Complete(data.Instance),

                    // PageFilterOnPageHandlerExecution
                    BeforePageFilterOnPageHandlerExecutionEventData data => StartFilter("Filter (Exec)", data.Filter),
                    AfterPageFilterOnPageHandlerExecutionEventData data => Complete(data.Filter),
                    // PageFilterOnPageHandlerExecuting
                    BeforePageFilterOnPageHandlerExecutingEventData data => StartFilter("Filter (Execing)", data.Filter),
                    AfterPageFilterOnPageHandlerExecutingEventData data => Complete(data.Filter),
                    // PageFilterOnPageHandlerExecuted
                    BeforePageFilterOnPageHandlerExecutedEventData data => StartFilter("Filter (Execed)", data.Filter),
                    AfterPageFilterOnPageHandlerExecutedEventData data => Complete(data.Filter),

                    // PageFilterOnPageHandlerSelection
                    BeforePageFilterOnPageHandlerSelectionEventData data => StartFilter("Filter (Selection)", data.Filter),
                    AfterPageFilterOnPageHandlerSelectionEventData data => Complete(data.Filter),
                    // PageFilterOnPageHandlerSelected
                    BeforePageFilterOnPageHandlerSelectedEventData data => StartFilter("Filter (Selected)", data.Filter),
                    AfterPageFilterOnPageHandlerSelectedEventData data => Complete(data.Filter),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                // Don't error in profiling here, just flow it out
                MiniProfiler.DefaultOptions?.OnInternalError?.Invoke(ex);
            }
        }
    }
}
#endif
