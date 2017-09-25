using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Profiling.Helpers;
using StackExchange.Profiling.Internal;
using Microsoft.Extensions.Caching.Memory;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// A IMemoryCache-based provider for storing MiniProfiler instances (based on System.Runtime.Caching.MemoryCache)
    /// </summary>
    public class MemoryCacheStorage : IAsyncStorage
    {
        private readonly IMemoryCache _cache;
        private MemoryCacheEntryOptions CacheEntryOptions { get; }
        private readonly SortedList<ProfilerSortedKey, object> _profiles = new SortedList<ProfilerSortedKey, object>();

        /// <summary>
        /// The string that prefixes all keys that MiniProfilers are saved under, e.g.
        /// <c>"mini-profiler-ecfb0050-7ce8-4bf1-bf82-2cb38e90e31e".</c>
        /// </summary>
        public const string CacheKeyPrefix = "mini-profiler-";

        /// <summary>
        /// Gets or sets how long to cache each <see cref="MiniProfiler"/> for, in absolute terms.
        /// </summary>
        public TimeSpan CacheDuration { get; set; }

        /// <summary>
        /// Creates a memory cache provider, storing each result in the provided IMemoryCache
        /// for the specified duration.
        /// </summary>
        /// <param name="cache">The <see cref="IMemoryCache"/> to use for storage.</param>
        /// <param name="cacheDuration">The duration to cache each profiler, before it expires from cache.</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="cache"/> is null.</exception>
        public MemoryCacheStorage(IMemoryCache cache, TimeSpan cacheDuration)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            CacheDuration = cacheDuration;
            CacheEntryOptions = new MemoryCacheEntryOptions { SlidingExpiration = cacheDuration };
        }

        private string GetCacheKey(Guid id) => CacheKeyPrefix + id.ToString();

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <c>MiniProfilerOptions.UserProvider</c></param>
        public List<Guid> GetUnviewedIds(string user)
        {
            var ids = GetPerUserUnviewedIds(user);
            lock (ids)
            {
                return new List<Guid>(ids);
            }
        }

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <c>MiniProfilerOptions.UserProvider</c></param>
        public Task<List<Guid>> GetUnviewedIdsAsync(string user) => Task.FromResult(GetUnviewedIds(user));

        private string GetPerUserUnviewedCacheKey(string user) => CacheKeyPrefix + "unviewed-for-user-" + user;

        private List<Guid> GetPerUserUnviewedIds(string user)
        {
            var key = GetPerUserUnviewedCacheKey(user);
            return _cache.Get(key) as List<Guid> ?? new List<Guid>();
        }

        /// <summary>
        /// List the latest profiling results.
        /// </summary>
        /// <param name="maxResults">The maximum number of results to return.</param>
        /// <param name="start">(Optional) The start of the date range to fetch.</param>
        /// <param name="finish">(Optional) The end of the date range to fetch.</param>
        /// <param name="orderBy">(Optional) The order to fetch profiler IDs in.</param>
        public IEnumerable<Guid> List(
            int maxResults,
            DateTime? start = null,
            DateTime? finish = null,
            ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            var guids = new List<Guid>();
            lock (_profiles)
            {
                int idxStart = 0;
                int idxFinish = _profiles.Count - 1;
                if (start != null) idxStart = _profiles.BinaryClosestSearch(start.Value);
                if (finish != null) idxFinish = _profiles.BinaryClosestSearch(finish.Value);

                if (idxStart < 0) idxStart = 0;
                if (idxFinish >= _profiles.Count) idxFinish = _profiles.Count - 1;

                var keys = _profiles.Keys;

                if (orderBy == ListResultsOrder.Ascending)
                {
                    for (int i = idxStart; i <= idxFinish; i++)
                    {
                        guids.Add(keys[i].Id);
                        if (guids.Count == maxResults) break;
                    }
                }
                else
                {
                    for (int i = idxFinish; i >= idxStart; i--)
                    {
                        guids.Add(keys[i].Id);
                        if (guids.Count == maxResults) break;
                    }
                }
            }
            return guids;
        }

        /// <summary>
        /// List the latest profiling results.
        /// </summary>
        /// <param name="maxResults">The maximum number of results to return.</param>
        /// <param name="start">(Optional) The start of the date range to fetch.</param>
        /// <param name="finish">(Optional) The end of the date range to fetch.</param>
        /// <param name="orderBy">(Optional) The order to fetch profiler IDs in.</param>
        public Task<IEnumerable<Guid>> ListAsync(
            int maxResults,
            DateTime? start = null,
            DateTime? finish = null,
            ListResultsOrder orderBy = ListResultsOrder.Descending) => Task.FromResult(List(maxResults, start, finish, orderBy));

        /// <summary>
        /// Returns the saved <see cref="MiniProfiler"/> identified by <paramref name="id"/>. Also marks the resulting
        /// profiler <see cref="MiniProfiler.HasUserViewed"/> to true.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public MiniProfiler Load(Guid id) => _cache.Get(GetCacheKey(id)) as MiniProfiler;

        /// <summary>
        /// Returns the saved <see cref="MiniProfiler"/> identified by <paramref name="id"/>. Also marks the resulting
        /// profiler <see cref="MiniProfiler.HasUserViewed"/> to true.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public Task<MiniProfiler> LoadAsync(Guid id) => Task.FromResult(Load(id));

        /// <summary>
        /// Saves <paramref name="profiler"/> to the HttpRuntime.Cache under a key concatenated with <see cref="CacheKeyPrefix"/>
        /// and the parameter's <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public void Save(MiniProfiler profiler)
        {
            if (profiler == null) return;

            // use insert instead of add; add fails if the item already exists
            _cache.Set(GetCacheKey(profiler.Id), profiler, DateTime.UtcNow + CacheDuration);

            var profileInfo = new ProfilerSortedKey(profiler);
            lock (_profiles)
            {
                if (!_profiles.ContainsKey(profileInfo))
                {
                    _profiles.Add(profileInfo, null);
                }

                while (_profiles.Count > 0)
                {
                    var first = _profiles.Keys[0];
                    if (first.Started < DateTime.UtcNow.Add(-CacheDuration))
                    {
                        _profiles.RemoveAt(0);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            if (!profiler.HasUserViewed)
            {
                SetUnviewed(profiler.User, profiler.Id);
            }
        }

        /// <summary>
        /// Saves <paramref name="profiler"/> to the HttpRuntime.Cache under a key concatenated with <see cref="CacheKeyPrefix"/>
        /// and the parameter's <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public Task SaveAsync(MiniProfiler profiler)
        {
            Save(profiler);
            return Polyfills.CompletedTask;
        }

        /// <summary>
        /// Set the profile to unviewed for this user
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public void SetUnviewed(string user, Guid id)
        {
            var ids = GetPerUserUnviewedIds(user);
            lock (ids)
            {
                if (!ids.Contains(id))
                {
                    ids.Add(id);
                }
            }
            var key = GetPerUserUnviewedCacheKey(user);
            _cache.Set(key, ids, DateTime.UtcNow + CacheDuration);
        }

        /// <summary>
        /// Set the profile to unviewed for this user
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public Task SetUnviewedAsync(string user, Guid id)
        {
            SetUnviewed(user, id);
            return Polyfills.CompletedTask;
        }

        /// <summary>
        /// Set the profile to viewed for this user
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public void SetViewed(string user, Guid id)
        {
            var ids = GetPerUserUnviewedIds(user);
            lock (ids)
            {
                ids.Remove(id);
            }
        }

        /// <summary>
        /// Set the profile to viewed for this user
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public Task SetViewedAsync(string user, Guid id)
        {
            SetViewed(user, id);
            return Polyfills.CompletedTask;
        }
    }
}
