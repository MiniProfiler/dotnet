﻿using System.Web.Mvc;

namespace StackExchange.Profiling.Mvc
{
    /// <summary>
    /// Wrapped MVC View that ProfilingViewEngine uses to log profiling data
    /// </summary>
    public class WrappedView : IView
    {
        /// <summary>
        /// Gets MVC IView that is wrapped by the ProfilingViewEngine.
        /// </summary>
        public IView Wrapped { get; private set; }

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
                var view = Wrapped as RazorView;
                return view != null ? view.ViewPath : null;
            }
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="WrappedView"/> class. 
        /// </summary>
        public WrappedView(IView wrapped, string name, bool isPartial)
        {
            Wrapped = wrapped;
            Name = name;
            IsPartial = isPartial;
        }

        /// <summary>
        /// Renders the WrappedView and logs profiling data
        /// </summary>
        public void Render(ViewContext viewContext, System.IO.TextWriter writer)
        {
            var prof = MiniProfiler.Current;
            string name = prof != null ? ("Render" + (IsPartial ? " partial" : "") + ": " + Name) : null;
            using (prof.Step(name))
            {
                Wrapped.Render(viewContext, writer);
            }
        }
    }
}