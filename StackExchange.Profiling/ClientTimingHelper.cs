using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StackExchange.Profiling
{
    public static partial class ClientTimingHelper
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

    }
}
