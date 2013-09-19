using System;
using System.Web;
using System.Web.WebPages;

#if ASP_NET_MVC3
namespace StackExchange.Profiling
{
    /// <summary>
    /// Used to provide 
    /// </summary>
    partial class ClientTimingHelper
    {
        /// <summary>
        /// This needs to be called at the beginning of the layout for client side probe support, returns nothing if mini profiler is not enabled
        /// </summary>
        /// <param name="page">The page.</param>
        /// <returns>return the initialisation script.</returns>
        public static IHtmlString InitClientTimings(this WebPageBase page)
        {
            if (MiniProfiler.Current == null) return null;
            return new HtmlString(InitScript);
        }

        /// <summary>
        /// To be used inline in razor pages - times a script be sure to call <c>InitClientTimings</c> first
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="name">The name.</param>
        /// <param name="html">The html.</param>
        /// <returns>the timing script content</returns>
        public static IHtmlString TimeScript(this WebPageBase page, string name, Func<object, HelperResult> html)
        {
            var result = html(null).ToHtmlString();
            return new HtmlString(TimeScript(name, result));
        }

        /// <summary>
        /// To be used inline in razor pages - times a script be sure to call <c>InitClientTimings</c> first
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="name">The name.</param>
        /// <param name="html">The html.</param>
        /// <returns>a string containing the time script</returns>
        public static IHtmlString TimeScript(this WebPageBase page, string name, IHtmlString html)
        {
            return new HtmlString(TimeScript(name, html.ToHtmlString()));
        }

        /// <summary>
        /// To be used inline in razor pages - times a script be sure to call <c>InitClientTimings</c> first
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="name">The name.</param>
        /// <param name="html">The html.</param>
        /// <returns>a string containing the time script.</returns>
        public static IHtmlString TimeScript(this WebPageBase page, string name, string html)
        {
            return new HtmlString(TimeScript(name, html));
        }
    }
}
#endif