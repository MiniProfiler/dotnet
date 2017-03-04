using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Threading.Tasks;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Tag helper to profile child contents in ASP.NET Core views, e.g. 
    /// &lt;profile name="My Step" /&gt;
    /// ...child content...
    /// &lt;/profile&gt;
    /// </summary>
    [HtmlTargetElement("profile", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class ProfileTagHelper : TagHelper
    {
        /// <summary>
        /// The name of this <see cref="MiniProfiler"/> step.
        /// </summary>
        [HtmlAttributeName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Processes this section, profiling the contents within.
        /// </summary>
        /// <param name="context">The context to render in.</param>
        /// <param name="output">The output to render to.</param>
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            using (MiniProfiler.Current.Step(Name))
            {
#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.
                output.Content = await output.GetChildContentAsync();
#pragma warning restore RCS1090 // Call 'ConfigureAwait(false)'.
            }
        }
    }
}
