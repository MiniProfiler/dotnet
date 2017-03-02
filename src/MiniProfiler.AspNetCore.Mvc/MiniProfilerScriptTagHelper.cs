using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StackExchange.Profiling
{
    [HtmlTargetElement("mini-profiler", TagStructure = TagStructure.WithoutEndTag)]
    public class MiniProfilerScriptTagHelper : TagHelper
    {
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public RenderPosition Position { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;
            output.Content.SetHtmlContent(MiniProfiler.Current.RenderIncludes(ViewContext.HttpContext, Position));
        }
    }
}
