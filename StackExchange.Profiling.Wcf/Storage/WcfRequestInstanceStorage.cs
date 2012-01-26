using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StackExchange.Profiling.Storage;
using StackExchange.Profiling.Wcf.Helpers;

namespace StackExchange.Profiling.Wcf.Storage
{
    /// <summary>
    /// Provides a mechanism for just storing the results of the profiling in the request context items
    /// This gives us just enough storage to return the results as part of the headers and no more
    /// Use this in a N-tier scenario when the calling tier understands results and is able to persist them
    /// </summary>
    public class WcfRequestInstanceStorage : IStorage
    {
        /// <summary>
        /// The string that prefixes all keys that MiniProfilers are saved under, e.g.
        /// "mini-profiler-ecfb0050-7ce8-4bf1-bf82-2cb38e90e31e".
        /// </summary>
        private const string CacheKeyPrefix = "mini-profiler-";

        public void Save(MiniProfiler profiler)
        {
            var context = WcfInstanceContext.Current;
            // Do nothing if we are not being called inside a WCF method
            // Alternatively, this could throw an Exception
            if (context == null)
                return;

            context.Items[GetCacheKey(profiler.Id)] = profiler;
        }

        private object GetCacheKey(Guid guid)
        {
            return CacheKeyPrefix + guid.ToString();
        }

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
        /// We're not going to worry about unviewed ids for this - there should only be one method associated with this, just return it
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public List<Guid> GetUnviewedIds(string user)
        {
            return new List<Guid>();
        }
    }
}
