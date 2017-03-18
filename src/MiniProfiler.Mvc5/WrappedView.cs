using System.IO;
using System.Web.Mvc;

namespace StackExchange.Profiling.Mvc
{
    /// <summary>
    /// Wrapped MVC View that ProfilingViewEngine uses to log profiling data
    /// </summary>
    public class WrappedView : IView
    {
        /// <summary>
        /// MVC IView that is wrapped by the ProfilingViewEngine
        /// </summary>
        private readonly IView _wrapped;

        /// <summary>
        /// Gets or sets the wrapped view name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the wrapped view is partial.
        /// </summary>
        public bool IsPartial { get; set; }

        /// <summary>
        /// Gets the wrapped view path.
        /// </summary>
        public string ViewPath => (_wrapped as RazorView)?.ViewPath;

        /// <summary>
        /// Initialises a new instance of the <see cref="WrappedView"/> class. 
        /// </summary>
        /// <param name="wrapped">The <see cref="IView"/> to be wrapped (and profiled).</param>
        /// <param name="name">The name of the view.</param>
        /// <param name="isPartial">Whether the view is a partial.</param>
        public WrappedView(IView wrapped, string name, bool isPartial)
        {
            _wrapped = wrapped;
            Name = name;
            IsPartial = isPartial;
        }

        /// <summary>
        /// Renders the WrappedView and logs profiling data.
        /// </summary>
        /// <param name="viewContext">The view context to render.</param>
        /// <param name="writer">The writer to render the view to.</param>
        public void Render(ViewContext viewContext, TextWriter writer)
        {
            var prof = MiniProfiler.Current;
            string name = prof != null ? ("Render" + (IsPartial ? " partial" : string.Empty) + ": " + Name) : null;
            using (prof.Step(name))
            {
                _wrapped.Render(viewContext, writer);
            }
        }
    }
}