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
        public const string InitScript = "<script>mPt=function(){var t=[];return{results:function(){return t},probe:function(n){t.push({d:new Date(),n:n})},flush:function(){t=[]}}}()</script>";

        /// <summary>
        /// You can wrap an html block with timing wrappers using this helper
        /// </summary>
        /// <param name="name">The name of the block to time.</param>
        /// <param name="html">The html to wrap in this timing.</param>
        public static string TimeScript(string name, string html)
        {
            if (MiniProfiler.Current != null)
            {
                name = name.Replace("'", "\\'");
                var probe = "<script>mPt.probe('" + name + "')</script>";
                html = probe + html + probe;
            }

            return html;
        }
    }
}
