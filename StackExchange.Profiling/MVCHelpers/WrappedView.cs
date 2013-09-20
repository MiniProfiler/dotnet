using System.Web.Mvc;

#if ASP_NET_MVC3
namespace StackExchange.Profiling.MVCHelpers
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
        public string ViewPath
        {
            get
            {
                var view = _wrapped as RazorView;
                return view != null ? view.ViewPath : null;
            }
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="WrappedView"/> class. 
        /// </summary>
        public WrappedView(IView wrapped, string name, bool isPartial)
        {
            _wrapped = wrapped;
            Name = name;
            IsPartial = isPartial;
        }

        /// <summary>
        /// Renders the WrappedView and logs profiling data
        /// </summary>
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