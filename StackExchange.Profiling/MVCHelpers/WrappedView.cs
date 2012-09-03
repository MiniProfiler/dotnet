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
        public IView wrapped;
        /// <summary>
        /// ViewName of wrapped View
        /// </summary>
        public string name;
        /// <summary>
        /// Flag as to whether wrapped is a PartialView
        /// </summary>
        public bool isPartial;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="wrapped">IView to wrap</param>
        /// <param name="name">Name/Path to view</param>
        /// <param name="isPartial">Whether view is Partial</param>
        public WrappedView(IView wrapped, string name, bool isPartial)
        {
            this.wrapped = wrapped;
            this.name = name;
            this.isPartial = isPartial;
        }

        /// <summary>
        /// Renders the WrappedView and logs profiling data
        /// </summary>
        /// <param name="viewContext"></param>
        /// <param name="writer"></param>
        public void Render(ViewContext viewContext, System.IO.TextWriter writer)
        {
            using (MiniProfiler.Current.Step("Render " + (isPartial ? "partial" : "") + ": " + name))
            {
                wrapped.Render(viewContext, writer);
            }
        }
    }
}
#endif