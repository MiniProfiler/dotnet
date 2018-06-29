using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Tag helper to render MiniProfiler client timing initalization in ASP.NET Core views, e.g.
    /// &lt;mini-profiler-timing /&gt;
    /// </summary>
    [HtmlTargetElement("mini-profiler-timing", TagStructure = TagStructure.WithoutEndTag)]
    public class MiniProfilerTimingTagHelper : TagHelper
    {
        internal const string ClientTimingKey = "MiniProfiler:ClientTimingScriptLoaded";

        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;

            if (MiniProfiler.Current == null || ViewContext.ViewData.ContainsKey(ClientTimingKey))
                return;

            output.Content.SetHtmlContent(ClientTimingHelper.InitScript);
            ViewContext.ViewData[ClientTimingKey] = true;
        }
    }
}
