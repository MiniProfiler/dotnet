using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Tag helper to render MiniProfiler in ASP.NET Core views, e.g. &lt;mini-profiler position="Right" /&gt;
    /// </summary>
    [HtmlTargetElement("mini-profiler", TagStructure = TagStructure.WithoutEndTag)]
    public class MiniProfilerScriptTagHelper : TagHelper
    {
        /// <summary>
        /// The view context of this tag helper, for accessing HttpContext on render.
        /// </summary>
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        /// <summary>
        /// The UI position to render the profiler in (defaults to <see cref="MiniProfilerBaseOptions.PopupRenderPosition"/>).
        /// </summary>
        [HtmlAttributeName("position")]
        public RenderPosition? Position { get; set; }

        /// <summary>
        /// Whether to show trivial timings column initially or not (defaults to <see cref="MiniProfilerBaseOptions.PopupShowTrivial"/>).
        /// </summary>
        [HtmlAttributeName("show-trivial")]
        public bool? ShowTrivial { get; set; }

        /// <summary>
        /// Whether to show time with children column initially or not (defaults to <see cref="MiniProfilerBaseOptions.PopupShowTimeWithChildren"/>).
        /// </summary>
        [HtmlAttributeName("show-time-with-children")]
        public bool? ShowTimeWithChildren { get; set; }

        /// <summary>
        /// The maximum number of profilers to show (before the oldest is removed - defaults to <see cref="MiniProfilerBaseOptions.PopupMaxTracesToShow"/>).
        /// </summary>
        [HtmlAttributeName("max-traces")]
        public int? MaxTraces { get; set; }

        /// <summary>
        /// Whether to show the controls (defaults to <see cref="MiniProfilerBaseOptions.ShowControls"/>).
        /// </summary>
        [HtmlAttributeName("show-controls")]
        public bool? ShowControls { get; set; }

        /// <summary>
        /// Whether to start hidden (defaults to <see cref="MiniProfilerBaseOptions.PopupStartHidden"/>).
        /// </summary>
        [HtmlAttributeName("start-hidden")]
        public bool? StartHidden { get; set; }

        /// <summary>
        /// The color scheme to start with:
        /// </summary>
        [HtmlAttributeName("color-scheme")]
        public ColorScheme? ColorScheme { get; set; }

        /// <summary>
        /// The options to use when rendering this MiniProfiler.
        /// Note: overrides all other options.
        /// </summary>
        [HtmlAttributeName("options")]
        public RenderOptions RenderOptions { get; set; }

        private RenderOptions GetOptions()
        {
            var options = RenderOptions ?? new RenderOptions();

            if (Position.HasValue) options.Position = Position;
            if (ShowTrivial.HasValue) options.ShowTrivial = ShowTrivial;
            if (ShowTimeWithChildren.HasValue) options.ShowTimeWithChildren = ShowTimeWithChildren;
            if (MaxTraces.HasValue) options.MaxTracesToShow = MaxTraces;
            if (ShowControls.HasValue) options.ShowControls = ShowControls;
            if (StartHidden.HasValue) options.StartHidden = StartHidden;
            if (ColorScheme.HasValue) options.ColorScheme = ColorScheme;

            return options;
        }

        /// <summary>
        /// Processes this tag, rendering some lovely HTML.
        /// </summary>
        /// <param name="context">The context to render in.</param>
        /// <param name="output">The output to render to.</param>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;
            var tag = MiniProfiler.Current.RenderIncludes(
                        ViewContext.HttpContext,
                        GetOptions());
            output.Content.SetHtmlContent(tag);
        }
    }
}
