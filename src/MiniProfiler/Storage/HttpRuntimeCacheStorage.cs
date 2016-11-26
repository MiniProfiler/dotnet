using System;
using System.Collections.Generic;
using System.Web;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to the <see cref="System.Web.HttpRuntime.Cache"/> 
    /// with absolute expiration.
    /// </summary>
    public class HttpRuntimeCacheStorage : IStorage
    {
        /// <summary>
        /// Identifies a MiniProfiler result and only contains the needed info for sorting a list of profiling sessions.
        /// </summary>
        /// <remarks>SortedList on uses the comparer for both key lookups and insertion</remarks>
        private class ProfileInfo : IComparable<ProfileInfo>
        {
            public Guid Id { get; set; }
            public DateTime Started { get; set; }

            public int CompareTo(ProfileInfo other)
            {
                var comp = Started.CompareTo(other.Started);
                if (comp == 0) comp = Id.CompareTo(other.Id);
                return comp;
            }
        }

        /// <summary>
        /// Syncs access to runtime cache when adding a new list of ids for a user.
        /// </summary>
        private static readonly object AddPerUserUnviewedIdsLock = new object();

        private readonly SortedList<ProfileInfo, object> _profiles = new SortedList<ProfileInfo, object>();

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
        /// Initialises a new instance of the <see cref="HttpRuntimeCacheStorage"/> class. 
        /// Returns a new HttpRuntimeCacheStorage class that will cache MiniProfilers for the specified duration.
        /// </summary>
        public HttpRuntimeCacheStorage(TimeSpan cacheDuration)
        {
            CacheDuration = cacheDuration;
        }

        /// <summary>
        /// Saves <paramref name="profiler"/> to the HttpRuntime.Cache under a key concatenated with <see cref="CacheKeyPrefix"/>
        /// and the parameter's <see cref="MiniProfiler.Id"/>.
        /// </summary>
        public void Save(MiniProfiler profiler)
        {
            InsertIntoCache(GetCacheKey(profiler.Id), profiler);
            
            lock (_profiles)
            {
                var profileInfo = new ProfileInfo { Id = profiler.Id, Started = profiler.Started };
                if (_profiles.IndexOfKey(profileInfo) < 0) 
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
        /// remembers we did not view the profile
        /// </summary>
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
        /// Set the profile to viewed for this user
        /// </summary>
        public void SetViewed(string user, Guid id)
        {
            var ids = GetPerUserUnviewedIds(user);

            lock (ids)
            {
                ids.Remove(id);
            }
        }

        /// <summary>
        /// Returns the saved <see cref="MiniProfiler"/> identified by <paramref name="id"/>. Also marks the resulting
        /// profiler <see cref="MiniProfiler.HasUserViewed"/> to true.
        /// </summary>
        public MiniProfiler Load(Guid id) => HttpRuntime.Cache[GetCacheKey(id)] as MiniProfiler;

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">
        /// User identified by the current <c>MiniProfiler.Settings.UserProvider</c>.
        /// </param>
        public List<Guid> GetUnviewedIds(string user)
        {
            var ids = GetPerUserUnviewedIds(user);
            lock (ids)
            {
                return new List<Guid>(ids);
            }
        }

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

        private string GetCacheKey(Guid id) => CacheKeyPrefix + id;

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
                if (start != null) idxStart = BinaryClosestSearch(start.Value);
                if (finish != null) idxFinish = BinaryClosestSearch(finish.Value);

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

        private int BinaryClosestSearch(DateTime date)
        {
            int lower = 0;
            int upper = _profiles.Count - 1;

            while (lower <= upper)
            {
                int adjustedIndex = lower + ((upper - lower) >> 1);
                int comparison = _profiles.Keys[adjustedIndex].Started.CompareTo(date);
                if (comparison == 0)
                {
                    return adjustedIndex;
                }
                if (comparison < 0)
                {
                    lower = adjustedIndex + 1;
                }
                else
                {
                    upper = adjustedIndex - 1;
                }
            }
            return lower;
        }
    }
}
