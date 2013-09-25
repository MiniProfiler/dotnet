using System;
using System.Web;
using System.Web.WebPages;

namespace StackExchange.Profiling.Mvc
{
    /// <summary>
    /// Used to provide MVC-specific extensions for gathering <see cref="ClientTimingHelper"/> information.
    /// </summary>
    public static class ClientTimingHelperExtensions
    {
        /// <summary>
        /// This needs to be called at the beginning of the layout for client side probe support, returns nothing if mini profiler is not enabled
        /// </summary>
        public static IHtmlString InitClientTimings(this WebPageBase page)
        {
            if (MiniProfiler.Current == null) return null;
            return new HtmlString(ClientTimingHelper.InitScript);
        }

        /// <summary>
        /// To be used inline in razor pages - times a script be sure to call <c>InitClientTimings</c> first
        /// </summary>
        public static IHtmlString TimeScript(this WebPageBase page, string name, Func<object, HelperResult> html)
        {
            var result = html(null).ToHtmlString();
            return new HtmlString(ClientTimingHelper.TimeScript(name, result));
        }

        /// <summary>
        /// To be used inline in razor pages - times a script be sure to call <c>InitClientTimings</c> first
        /// </summary>
        public static IHtmlString TimeScript(this WebPageBase page, string name, IHtmlString html)
        {
            return new HtmlString(ClientTimingHelper.TimeScript(name, html.ToHtmlString()));
        }

        /// <summary>
        /// To be used inline in razor pages - times a script be sure to call <c>InitClientTimings</c> first
        /// </summary>
        public static IHtmlString TimeScript(this WebPageBase page, string name, string html)
        {
            return new HtmlString(ClientTimingHelper.TimeScript(name, html));
        }
    }
}