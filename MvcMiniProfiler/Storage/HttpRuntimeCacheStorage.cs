using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MvcMiniProfiler.Storage
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to the <see cref="System.Web.HttpRuntime.Cache"/> with absolute expiration.
    /// </summary>
    public class HttpRuntimeCacheStorage : IStorage
    {
        /// <summary>
        /// The string that prefixes all keys that MiniProfilers are saved under, e.g.
        /// "mini-profiler-ecfb0050-7ce8-4bf1-bf82-2cb38e90e31e".
        /// </summary>
        public const string CacheKeyPrefix = "mini-profiler-";

        /// <summary>
        /// How long to cache each <see cref="MiniProfiler"/> for (i.e. the absolute expiration parameter of 
        /// <see cref="System.Web.Caching.Cache.Insert(string, object, System.Web.Caching.CacheDependency, System.DateTime, System.TimeSpan, System.Web.Caching.CacheItemUpdateCallback)"/>)
        /// </summary>
        public TimeSpan CacheDuration { get; set; }

        /// <summary>
        /// Returns a new HttpRuntimeCacheStorage class that will cache MiniProfilers for the specified duration.
        /// </summary>
        public HttpRuntimeCacheStorage(TimeSpan cacheDuration)
        {
            CacheDuration = cacheDuration;
        }

        /// <summary>
        /// Saves <paramref name="profiler"/> to the HttpRuntime.Cache under a key concated with <see cref="CacheKeyPrefix"/>
        /// and the parameter's <see cref="MiniProfiler.Id"/>.
        /// </summary>
        public void SaveMiniProfiler(MiniProfiler profiler)
        {
            HttpRuntime.Cache.Insert(
                    key: GetCacheKey(profiler.Id),
                    value: profiler,
                    dependencies: null,
                    absoluteExpiration: DateTime.Now.Add(CacheDuration), // servers will cache based on local now
                    slidingExpiration: System.Web.Caching.Cache.NoSlidingExpiration,
                    priority: System.Web.Caching.CacheItemPriority.Low,
                    onRemoveCallback: null);
        }

        /// <summary>
        /// Returns the originally-stored <see cref="MiniProfiler"/> 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public MiniProfiler LoadMiniProfiler(Guid id)
        {
            return HttpRuntime.Cache[GetCacheKey(id)] as MiniProfiler;
        }

        private string GetCacheKey(Guid id)
        {
            return CacheKeyPrefix + id;
        }

    }
}
