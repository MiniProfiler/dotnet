using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Tag helper to profile script execution in ASP.NET Core views, e.g. 
    /// &lt;profile-script name="My Step" /&gt;
    /// ...script blocks...
    /// &lt;/profile-script&gt;
    /// </summary>
    [HtmlTargetElement("profile-script", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class ProfileScriptTagHelper : TagHelper
    {
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        /// <summary>
        /// The name of this <see cref="MiniProfiler"/> step.
        /// </summary>
        [HtmlAttributeName("name")]
        public string Name { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;
            output.Content = await output.GetChildContentAsync();

            if (MiniProfiler.Current == null)
                return;

            var pre = StringBuilderCache.Get();
            if (!ViewContext.ViewData.ContainsKey(MiniProfilerTimingTagHelper.ClientTimingKey))
            {
                pre.Append(ClientTimingHelper.InitScript);
                ViewContext.ViewData[MiniProfilerTimingTagHelper.ClientTimingKey] = true;
            }
            pre.Append($"<script>mPt.start('{Name}')</script>");
            output.PreContent.SetHtmlContent(pre.ToStringRecycle());
            output.PostContent.SetHtmlContent($"<script>mPt.end('{Name}')</script>");
        }
    }
}
