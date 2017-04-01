using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace StackExchange.Profiling.Internal
{
    /// <summary>
    /// Internal MiniProfiler architecture, not meant for consumption.
    /// This can and probably will break without warning. Don't use the .Internal namespace directly.
    /// You can wrap your view engines with this view to enable profiling on views and partial.
    /// </summary>
    internal class ProfilingViewEngine : IViewEngine
    {
        private readonly IViewEngine _wrapped;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilingViewEngine"/> class. 
        /// </summary>
        /// <param name="wrapped">Original view engine to profile</param>
        public ProfilingViewEngine(IViewEngine wrapped)
        {
            _wrapped = wrapped;
        }

        /// <summary>
        /// Finds the view with the given <paramref name="viewName"/> using view locations and information from the
        /// <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/>.</param>
        /// <param name="viewName">The name of the view.</param>
        /// <param name="isMainPage">Determines if the page being found is the main page for an action.</param>
        /// <returns>The <see cref="ViewEngineResult"/> of locating the view.</returns>
        public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage)
        {
            using (MiniProfiler.Current.Step("Find: " + viewName))
            {
                var found = _wrapped.FindView(context, viewName, isMainPage);
                if (found.View != null)
                {
                    return ViewEngineResult.Found(viewName, new WrappedView(found.View));
                }
                return found;
            }
        }

        /// <summary>
        /// Gets the view with the given <paramref name="viewPath"/>, relative to <paramref name="executingFilePath"/>
        /// unless <paramref name="viewPath"/> is already absolute.
        /// </summary>
        /// <param name="executingFilePath">The absolute path to the currently-executing view, if any.</param>
        /// <param name="viewPath">The path to the view.</param>
        /// <param name="isMainPage">Determines if the page being found is the main page for an action.</param>
        /// <returns>The <see cref="ViewEngineResult"/> of locating the view.</returns>
        public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage) =>
            _wrapped.GetView(executingFilePath, viewPath, isMainPage);
    }
}