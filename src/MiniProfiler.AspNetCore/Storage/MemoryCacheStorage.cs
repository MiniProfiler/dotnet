using Microsoft.Extensions.Caching.Memory;
using System;

namespace StackExchange.Profiling.Storage
{
    // TODO: Finish implementing
    public class MemoryCacheStorage //: IAsyncStorage
    {
        /// <summary>
        /// The string that prefixes all keys that MiniProfilers are saved under, e.g.
        /// <c>"mini-profiler-ecfb0050-7ce8-4bf1-bf82-2cb38e90e31e".</c>
        /// </summary>
        public const string CacheKeyPrefix = "mini-profiler-";

        /// <summary>
        /// Gets or sets how long to cache each <see cref="MiniProfiler"/> for, in absolute terms.
        /// </summary>
        public TimeSpan CacheDuration { get; }

        /// <summary>
        /// Creates a memory cache provider, storing each result in the provided IMemoryCache
        /// for the specified duration.
        /// </summary>
        /// <param name="cache">The <see cref="IMemoryCache"/> to use for storage.</param>
        /// <param name="cacheDuration">The duration to cache each profiler, before it expires from cache.</param>
        public MemoryCacheStorage(IMemoryCache cache, TimeSpan cacheDuration)
        {
            CacheDuration = cacheDuration;
        }

        private string GetCacheKey(Guid id) => CacheKeyPrefix + id;

        private string GetPerUserUnviewedCacheKey(string user) => CacheKeyPrefix + "unviewed-for-user-" + user;

    }
}
