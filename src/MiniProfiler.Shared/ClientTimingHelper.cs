using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Pro
    /// </summary>
    public static class ClientTimingHelper
    {
        /// <summary>
        /// This code needs to be inserted in the page before client timings work
        /// </summary>
        public const string InitScript = "<script>mPt=function(){var t={};return{results:function(){return t},start:function(n){t[n]={start:new Date().getTime()}},end:function(n){t[n].end=new Date().getTime()},flush:function(){t={};}}}();</script>";

        /// <summary>
        /// You can wrap an HTML block with timing wrappers using this helper
        /// </summary>
        /// <param name="name">The name of the block to time.</param>
        /// <param name="html">The HTML to wrap in this timing.</param>
        public static string TimeScript(string name, string html)
        {
            if (MiniProfiler.Current != null)
            {
                var sb = StringBuilderCache.Get();
                name = name.Replace("'", "\\'");
                sb.Append("<script>mPt.start('").Append(name).Append("')</script>");
                sb.Append(html);
                sb.Append("<script>mPt.end('").Append(name).Append("')</script>");
                return sb.ToStringRecycle();
            }

            return html;
        }
    }
}
