namespace StackExchange.Profiling.Wcf.Storage
{
    using System;
    using System.Collections.Generic;

    using StackExchange.Profiling.Storage;
    using StackExchange.Profiling.Wcf.Helpers;

    /// <summary>
    /// Provides a mechanism for just storing the results of the profiling in the request context items
    /// This gives us just enough storage to return the results as part of the headers and no more
    /// Use this in a N-tier scenario when the calling tier understands results and is able to persist them
    /// </summary>
    public class WcfRequestInstanceStorage : IStorage
    {
        /// <summary>
        /// The string that prefixes all keys that <c>MiniProfilers</c> are saved under, e.g.
        /// <c>"mini-profiler-ecfb0050-7ce8-4bf1-bf82-2cb38e90e31e".</c>
        /// </summary>
        private const string CacheKeyPrefix = "mini-profiler-";

        /// <summary>
        /// save the profiler.
        /// </summary>
        /// <param name="profiler">The profiler to save.</param>
        public void Save(MiniProfiler profiler)
        {
            var context = WcfInstanceContext.Current;

            // Do nothing if we are not being called inside a WCF method
            // Alternatively, this could throw an Exception
            if (context == null)
                return;

            context.Items[GetCacheKey(profiler.Id)] = profiler;
        }

        /// <summary>
        /// load the profiler.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>the loaded mini profiler.</returns>
        public MiniProfiler Load(Guid id)
        {
            var context = WcfInstanceContext.Current;

            // Do nothing if we are not being called inside a WCF method
            // Alternatively, this could throw an Exception
            if (context == null)
                return null;

            var profiler = context.Items[GetCacheKey(id)] as MiniProfiler;
            if (profiler != null)
            {
                profiler.HasUserViewed = true;
            }

            return profiler;
        }

        /// <summary>
        /// We're not going to worry about un-viewed ids for this - there should only be one method associated with this, just return it
        /// </summary>
        /// <param name="user">a string containing the user name.</param>
        /// <returns>the list of keys</returns>
        public List<Guid> GetUnviewedIds(string user)
        {
            return new List<Guid>();
        }

        /// <summary>
        /// trivial implementation - we do not do any per user stuff ... so skip
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="id">The id.</param>
        public void SetUnviewed(string user, Guid id)
        {
        }

        /// <summary>
        /// trivial implementation
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="id">The id.</param>
        public void SetViewed(string user, Guid id)
        {
        }

        /// <summary>
        /// the list of keys.
        /// </summary>
        /// <param name="maxResults">The max results.</param>
        /// <param name="start">The start.</param>
        /// <param name="finish">The finish.</param>
        /// <param name="orderBy">The order by.</param>
        /// <returns>the set of keys.</returns>
        public IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            // some brave soul that know WCF needs to implement this
            throw new NotImplementedException("Looking for a dev who can support the WCF bits in Mini Profiler");
        }

        /// <summary>
        /// get the cache key.
        /// </summary>
        /// <param name="guid">The key.</param>
        /// <returns>the prefix, and key</returns>
        private object GetCacheKey(Guid guid)
        {
            return CacheKeyPrefix + guid.ToString();
        }
    }
}
