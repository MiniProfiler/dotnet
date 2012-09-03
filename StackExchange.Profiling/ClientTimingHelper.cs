using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.WebPages;

namespace StackExchange.Profiling
{
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
        /// This needs to be called at the begining of the layout for client side probe support, returns nothing if mini profiler is not enabled
        /// </summary>
        public static IHtmlString InitClientTimings(this WebPageBase page)
        {
            if (MiniProfiler.Current == null) return null;
            return new HtmlString(InitScript);
        }

        /// <summary>
        /// To be used inline in razor pages - times a script be sure to call InitClientTimings first
        /// </summary>
        public static IHtmlString TimeScript(this WebPageBase page, string name, Func<object, HelperResult> html)
        {
            var result = html(null).ToHtmlString();
            return new HtmlString(TimeScript(name, result));
        }

        /// <summary>
        /// To be used inline in razor pages - times a script be sure to call InitClientTimings first
        /// </summary>
        public static IHtmlString TimeScript(this WebPageBase page, string name, IHtmlString html)
        {
            return new HtmlString(TimeScript(name, html.ToHtmlString()));
        }

        /// <summary>
        /// To be used inline in razor pages - times a script be sure to call InitClientTimings first
        /// </summary>
        public static IHtmlString TimeScript(this WebPageBase page, string name, string html)
        {
            return new HtmlString(TimeScript(name, html));
        }
    }
}
