using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.DiagnosticAdapter;

namespace StackExchange.Profiling.Internal
{
    /// <summary>
    /// Diagnostic listener for Microsoft.AspNetCore.Mvc.* events
    /// </summary>
    internal class MvcViewDiagnosticListener : IMiniProfilerDiagnosticListener
    {
        /// <summary>
        /// Diagnostic Listener name to handle
        /// </summary>
        public string ListenerName => "Microsoft.AspNetCore";

        /// <summary>
        /// Handles BeforeView events. Fired before a view renders.
        /// </summary>
        /// <param name="view">The view being rendered.</param>
        [DiagnosticName("Microsoft.AspNetCore.Mvc.BeforeView")]
        public void OnBeforeView(IView view)
        {
            // Available: IView view, ViewContext viewContext
            MiniProfiler.Current?.Step("Render: " + view.Path);
        }

        /// <summary>
        /// Handles AfterView events. Fired after a view renders.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Mvc.AfterView")]
        public void OnAfterView()
        {
            // Available: IView view, ViewContext viewContext
            MiniProfiler.Current?.Head.Stop();
        }

        /// <summary>
        /// Handles BeforeViewComponent events. Fired before a ViewComponent runs.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Mvc.BeforeViewComponent")]
        public void OnBeforeViewComponent(ViewComponentContext context)
        {
            // Available: ActionDescriptor actionDescriptor, ViewComponentContext context, object viewComponent
            MiniProfiler.Current?.Step("View Component: " + context.ViewComponentDescriptor?.DisplayName);
        }

        /// <summary>
        /// Handles AfterViewComponent events. Fired after a ViewComponent runs.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Mvc.AfterViewComponent")]
        public void OnAfterViewComponent()
        {
            // Available: ActionDescriptor actionDescriptor, ViewComponentContext context, IViewComponentResult result, object viewComponent
            MiniProfiler.Current?.Head.Stop();
        }

        /// <summary>
        /// Handles ViewComponentBeforeViewExecute events. Fired before a ViewComponent's view executes.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Mvc.ViewComponentBeforeViewExecute")]
        public void OnViewComponentBeforeViewExecute(ViewComponentContext context)
        {
            // Available: ActionDescriptor actionDescriptor, ViewComponentContext context, IView view
            MiniProfiler.Current?.Step("View Component: " + context.ViewComponentDescriptor?.DisplayName);
        }

        /// <summary>
        /// Handles ViewComponentAfterViewExecute events. Fired after a ViewComponent's view executes.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Mvc.ViewComponentAfterViewExecute")]
        public void OnViewComponentAfterViewExecute()
        {
            // Available: ActionDescriptor actionDescriptor, ViewComponentContext context, IView view
            MiniProfiler.Current?.Head.Stop();
        }

        /* 
         * There's no start event for these...maybe used for exceptions later in the NotFound case.
         * 
        /// <summary>
        /// Handles ViewFound events.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Mvc.ViewFound")]
        public void OnViewFound()
        {
            // Available: ActionContext actionContext, bool isMainPage, PartialViewResult viewResult, string viewName, IView view
        }

        /// <summary>
        /// Handles ViewNotFound events.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Mvc.ViewNotFound")]
        public void OnViewNotFound()
        {
            // Available: ActionContext actionContext, bool isMainPage, PartialViewResult viewResult, string viewName, IEnumerable<string> searchedLocations
        }
        */
    }
}
