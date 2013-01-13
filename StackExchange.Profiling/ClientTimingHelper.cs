namespace StackExchange.Profiling
{
    using System;
    using System.Web;
    using System.Web.WebPages;

    /// <summary>
    /// Used to provide 
    /// </summary>
    public static class ClientTimingHelper
    {
        /// <summary>
        /// This code needs to be inserted in the page before client timings work
        /// </summary>
        public const string InitScript = "<script type='text/javascript'>mPt=function(){var t=[];return{results:function(){return t},probe:function(n){t.push({d:new Date(),n:n})},flush:function(){t=[]}}}()</script>";

        /// <summary>
        /// You can wrap an html block with timing wrappers using this helper
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="html">The html.</param>
        /// <returns>a string containing the timing script.</returns>
        public static string TimeScript(string name, string html)
        {
            if (MiniProfiler.Current != null)
            {
                name = name.Replace("'", "\\'");
                var probe = "<script type='text/javascript'>mPt.probe('" + name + "')</script>";
                html = probe + html + probe;
            }

            return html;
        }

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
