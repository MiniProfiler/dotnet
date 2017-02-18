using StackExchange.Profiling.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to the <see cref="HttpRuntime.Cache"/> 
    /// with absolute expiration.
    /// </summary>
    /// <remarks>
    /// Note: all Async members are actually synchronous, there's simply no async wins to be had here.
    /// </remarks>
    public class HttpRuntimeCacheStorage : IAsyncStorage
    {
        private readonly SortedList<ProfilerSortedKey, object> _profiles = new SortedList<ProfilerSortedKey, object>();

        /// <summary>
        /// Syncs access to runtime cache when adding a new list of ids for a user.
        /// </summary>
        private static readonly object AddPerUserUnviewedIdsLock = new object();

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
        /// Initialises a new instance of the <see cref="HttpRuntimeCacheStorage"/> class. 
        /// Returns a new HttpRuntimeCacheStorage class that will cache MiniProfilers for the specified duration.
        /// </summary>
        /// <param name="cacheDuration">The duration to store each <see cref="MiniProfiler"/> for, before it expires.</param>
        public HttpRuntimeCacheStorage(TimeSpan cacheDuration)
        {
            CacheDuration = cacheDuration;
        }

        /// <summary>
        /// Saves <paramref name="profiler"/> to the HttpRuntime.Cache under a key concatenated with 
        /// <see cref="CacheKeyPrefix"/> and the parameter's <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public void Save(MiniProfiler profiler)
        {
            InsertIntoCache(GetCacheKey(profiler.Id), profiler);

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
        }

        /// <summary>
        /// Saves <paramref name="profiler"/> to the HttpRuntime.Cache under a key concatenated with <see cref="CacheKeyPrefix"/>
        /// and the parameter's <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public Task SaveAsync(MiniProfiler profiler)
        {
            Save(profiler);
            return Task.CompletedTask;
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
        }

        /// <summary>
        /// Set the profile to unviewed for this user
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public Task SetUnviewedAsync(string user, Guid id)
        {
            SetUnviewed(user, id);
            return Task.CompletedTask;
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
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns the saved <see cref="MiniProfiler"/> identified by <paramref name="id"/>. Also marks the resulting
        /// profiler <see cref="MiniProfiler.HasUserViewed"/> to true.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public MiniProfiler Load(Guid id) => HttpRuntime.Cache[GetCacheKey(id)] as MiniProfiler;

        /// <summary>
        /// Returns the saved <see cref="MiniProfiler"/> identified by <paramref name="id"/>. Also marks the resulting
        /// profiler <see cref="MiniProfiler.HasUserViewed"/> to true.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public Task<MiniProfiler> LoadAsync(Guid id) => Task.FromResult(Load(id));

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <c>MiniProfiler.Settings.UserProvider</c></param>
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
        /// <param name="user">User identified by the current <c>MiniProfiler.Settings.UserProvider</c></param>
        public Task<List<Guid>> GetUnviewedIdsAsync(string user) => Task.FromResult(GetUnviewedIds(user));

        private void InsertIntoCache(string key, object value)
        {
            var expiration = DateTime.Now.Add(CacheDuration);

            // use insert instead of add; add fails if the item already exists
            HttpRuntime.Cache.Insert(
                key: key,
                value: value,
                dependencies: null,
                absoluteExpiration: expiration, // servers will cache based on local now
                slidingExpiration: System.Web.Caching.Cache.NoSlidingExpiration,
                priority: System.Web.Caching.CacheItemPriority.Low,
                onRemoveCallback: null);
        }

        private string GetCacheKey(Guid id) => CacheKeyPrefix + id.ToString();

        private string GetPerUserUnviewedCacheKey(string user) => CacheKeyPrefix + "unviewed-for-user-" + user;

        private List<Guid> GetPerUserUnviewedIds(string user)
        {
            var key = GetPerUserUnviewedCacheKey(user);
            var result = HttpRuntime.Cache[key] as List<Guid>;

            if (result == null)
            {
                lock (AddPerUserUnviewedIdsLock)
                {
                    // check again, as we could have been waiting
                    result = HttpRuntime.Cache[key] as List<Guid>;
                    if (result == null)
                    {
                        result = new List<Guid>();
                        InsertIntoCache(key, result);
                    }
                }
            }

            return result;
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
    }
}
