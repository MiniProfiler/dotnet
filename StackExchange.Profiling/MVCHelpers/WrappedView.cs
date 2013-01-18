#if ASP_NET_MVC3
namespace StackExchange.Profiling.MVCHelpers
{
    using System.Web.Mvc;

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
        /// Initialises a new instance of the <see cref="WrappedView"/> class. 
        /// </summary>
        /// <param name="wrapped">
        /// IView to wrap
        /// </param>
        /// <param name="name">
        /// Name/Path to view
        /// </param>
        /// <param name="isPartial">
        /// Whether view is Partial
        /// </param>
        public WrappedView(IView wrapped, string name, bool isPartial)
        {
            _wrapped = wrapped;
            Name = name;
            IsPartial = isPartial;
        }

        /// <summary>
        /// Renders the WrappedView and logs profiling data
        /// </summary>
        /// <param name="viewContext">
        /// The view Context.
        /// </param>
        /// <param name="writer">
        /// The writer.
        /// </param>
        public void Render(ViewContext viewContext, System.IO.TextWriter writer)
        {
            using (MiniProfiler.Current.Step("Render " + (IsPartial ? "partial" : string.Empty) + ": " + Name))
            {
                _wrapped.Render(viewContext, writer);
            }
        }
    }
}
#endif