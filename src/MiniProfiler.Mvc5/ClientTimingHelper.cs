using System;
using System.Web;
using System.Web.WebPages;

namespace StackExchange.Profiling.Mvc
{
#pragma warning disable RCS1175 // Unused this parameter.
    /// <summary>
    /// Used to provide MVC-specific extensions for gathering <see cref="ClientTimingHelper"/> information.
    /// </summary>
    public static class ClientTimingHelperExtensions
    {
        /// <summary>
        /// This needs to be called at the beginning of the layout for client side probe support, returns nothing if mini profiler is not enabled
        /// </summary>
        /// <param name="page">Page being timed</param>
        public static IHtmlString InitClientTimings(this WebPageBase page) =>
            MiniProfiler.Current == null ? null : new HtmlString(ClientTimingHelper.InitScript);

        /// <summary>
        /// To be used inline in razor pages - times a script be sure to call <c>InitClientTimings</c> first
        /// </summary>
        /// <param name="page">Page being timed</param>
        /// <param name="name">Name of the script</param>
        /// <param name="html">Html helper to render</param>
        public static IHtmlString TimeScript(this WebPageBase page, string name, Func<object, HelperResult> html) =>
            new HtmlString(ClientTimingHelper.TimeScript(name, html(null).ToHtmlString()));

        /// <summary>
        /// To be used inline in razor pages - times a script be sure to call <c>InitClientTimings</c> first
        /// </summary>
        /// <param name="page">Page being timed</param>
        /// <param name="name">Name of the script</param>
        /// <param name="html">Html to render</param>
        public static IHtmlString TimeScript(this WebPageBase page, string name, IHtmlString html) =>
            new HtmlString(ClientTimingHelper.TimeScript(name, html.ToHtmlString()));

        /// <summary>
        /// To be used inline in razor pages - times a script be sure to call <c>InitClientTimings</c> first
        /// </summary>
        /// <param name="page">Page being timed</param>
        /// <param name="name">Name of the script</param>
        /// <param name="html">Html to render</param>
        public static IHtmlString TimeScript(this WebPageBase page, string name, string html) =>
            new HtmlString(ClientTimingHelper.TimeScript(name, html));
    }
#pragma warning restore RCS1175 // Unused this parameter.
}