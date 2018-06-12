using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace StackExchange.Profiling.Internal
{
    /// <summary>
    /// A MiniProfiler-Wrapped <see cref="IViewComponentInvoker"/>.
    /// </summary>
    public class ProfilingViewComponentInvoker : IViewComponentInvoker
    {
        private readonly IViewComponentInvoker _defaultViewComponentInvoker;

        /// <summary>
        /// Creates a new <see cref="ProfilingViewComponentInvoker"/>.
        /// </summary>
        /// <param name="defaultViewComponentInvoker">The <see cref="IViewComponentInvoker"/> to wrap.</param>
        public ProfilingViewComponentInvoker(IViewComponentInvoker defaultViewComponentInvoker) =>
            _defaultViewComponentInvoker = defaultViewComponentInvoker;

        /// <summary>
        /// Invokes the wrapped view component, wrapped in a profiler step.
        /// </summary>
        /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
        public async Task InvokeAsync(ViewComponentContext context)
        {
            var viewComponentName = context.ViewComponentDescriptor.ShortName;

            using (MiniProfiler.Current.Step("ViewComponent: " + viewComponentName))
            {
                await _defaultViewComponentInvoker.InvokeAsync(context);
            }
        }
    }
}
