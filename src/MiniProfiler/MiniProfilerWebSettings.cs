using System;
using System.Web;

namespace StackExchange.Profiling
{
    public static class MiniProfilerWebSettings
    {
        /// <summary>
        /// A function that determines who can access the MiniProfiler results url and list url.  It should return true when
        /// the request client has access to results, false for a 401 to be returned. HttpRequest parameter is the current request and
        /// </summary>
        /// <remarks>
        /// The HttpRequest parameter that will be passed into this function should never be null.
        /// </remarks>
        public static Func<HttpRequest, bool> Results_Authorize { get; set; }

        /// <summary>
        /// Special authorization function that is called for the list results (listing all the profiling sessions), 
        /// we also test for results authorize always. This must be set and return true, to enable the listing feature.
        /// </summary>
        public static Func<HttpRequest, bool> Results_List_Authorize { get; set; }
    }
}
