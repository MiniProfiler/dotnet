using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.Threading.Tasks;

namespace StackExchange.Profiling.Internal
{
    /// <summary>
    /// Internal MiniProfiler architecture, not meant for consumption.
    /// This can and probably will break without warning. Don't use the .Internal namespace directly.
    /// Wrapped MVC View that ProfilingViewEngine uses to log profiling data
    /// </summary>
    internal class WrappedView : IView
    {
        /// <summary>
        /// MVC IView that is wrapped by the ProfilingViewEngine
        /// </summary>
        private readonly IView _wrapped;

        /// <summary>
        /// Gets the wrapped view path.
        /// </summary>
        public string Path => _wrapped.Path;

        /// <summary>
        /// Initialises a new instance of the <see cref="WrappedView"/> class. 
        /// </summary>
        /// <param name="wrapped">The view to wrap in a profiler</param>
        public WrappedView(IView wrapped)
        {
            _wrapped = wrapped;
        }

        /// <summary>
        /// Renders the WrappedView and logs profiling data
        /// </summary>
        /// <param name="context">Context to render</param>
        public async Task RenderAsync(ViewContext context)
        {
            var prof = MiniProfiler.Current;
            string name = prof != null ? ("Render: " + Path) : null;
            using (prof.Step(name))
            {
                await _wrapped.RenderAsync(context).ConfigureAwait(false);
            }
        }
    }
}