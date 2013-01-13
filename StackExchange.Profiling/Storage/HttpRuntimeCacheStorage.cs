namespace StackExchange.Profiling.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Web;

    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to the <see cref="System.Web.HttpRuntime.Cache"/> with absolute expiration.
    /// </summary>
    public class HttpRuntimeCacheStorage : IStorage
    {
        /// <summary>
        /// The profile info.
        /// FYI: SortedList on uses the comparer for both key lookups and insertion
        /// </summary>
        public class ProfileInfo : IComparable<ProfileInfo>
        {
            /// <summary>
            /// Gets or sets the started.
            /// </summary>
            public DateTime Started { get; set; }

            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            public Guid Id { get; set; }

            /// <summary>
            /// compare the profile information.
            /// </summary>
            /// <param name="other">The other profile info instance..</param>
            /// <returns>the comparison result.</returns>
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

        /// <summary>
        /// The profiles.
        /// </summary>
        private readonly SortedList<ProfileInfo, object> _profiles = new SortedList<ProfileInfo, object>();

        /// <summary>
        /// The string that prefixes all keys that MiniProfilers are saved under, e.g.
        /// <c>"mini-profiler-ecfb0050-7ce8-4bf1-bf82-2cb38e90e31e".</c>
        /// </summary>
        public const string CacheKeyPrefix = "mini-profiler-";

        /// <summary>
        /// Gets or sets how long to cache each <see cref="MiniProfiler"/> for (i.e. the absolute expiration parameter of 
        /// <see cref="System.Web.Caching.Cache.Insert(string, object, System.Web.Caching.CacheDependency, System.DateTime, System.TimeSpan, System.Web.Caching.CacheItemUpdateCallback)"/>)
        /// </summary>
        public TimeSpan CacheDuration { get; set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="HttpRuntimeCacheStorage"/> class. 
        /// Returns a new HttpRuntimeCacheStorage class that will cache MiniProfilers for the specified duration.
        /// </summary>
        /// <param name="cacheDuration">
        /// The cache Duration.
        /// </param>
        public HttpRuntimeCacheStorage(TimeSpan cacheDuration)
        {
            CacheDuration = cacheDuration;
        }

        /// <summary>
        /// Saves <paramref name="profiler"/> to the HttpRuntime.Cache under a key concatenated with <see cref="CacheKeyPrefix"/>
        /// and the parameter's <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The profiler.</param>
        public void Save(MiniProfiler profiler)
        {
            InsertIntoCache(GetCacheKey(profiler.Id), profiler);
            
            lock (this._profiles)
            {
                var profileInfo = new ProfileInfo { Id = profiler.Id, Started = profiler.Started };
                if (this._profiles.IndexOfKey(profileInfo) < 0) 
                {
                    this._profiles.Add(profileInfo, null);
                }

                while (this._profiles.Count > 0)
                {
                    var first = this._profiles.Keys[0];
                    if (first.Started < DateTime.UtcNow.Add(-CacheDuration))
                    {
                        this._profiles.RemoveAt(0);
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
        /// <param name="user">The user.</param>
        /// <param name="id">The id.</param>
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
        /// <param name="user">The user.</param>
        /// <param name="id">The id.</param>
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
        /// <param name="id">The id.</param>
        /// <returns>the mini profiler</returns>
        public MiniProfiler Load(Guid id)
        {
            var result = HttpRuntime.Cache[GetCacheKey(id)] as MiniProfiler;
            return result;
        }

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">
        /// User identified by the current <c>MiniProfiler.Settings.UserProvider</c>.
        /// </param>
        /// <returns>the list of keys.</returns>
        public List<Guid> GetUnviewedIds(string user)
        {
            var ids = GetPerUserUnviewedIds(user);
            lock (ids)
            {
                return new List<Guid>(ids);
            }
        }

        /// <summary>
        /// insert into cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// get the cache key.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>a string containing the cache key</returns>
        private string GetCacheKey(Guid id)
        {
            return CacheKeyPrefix + id;
        }

        /// <summary>
        /// get the per user un-viewed cache key.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>a string containing the un-viewed key</returns>
        private string GetPerUserUnviewedCacheKey(string user)
        {
            return CacheKeyPrefix + "unviewed-for-user-" + user;
        }

        /// <summary>
        /// get the per user un viewed ids.
        /// </summary>
        /// <param name="profiler">The profiler.</param>
        /// <returns>the list of keys</returns>
        private List<Guid> GetPerUserUnviewedIds(MiniProfiler profiler)
        {
            return GetPerUserUnviewedIds(profiler.User);
        }

        /// <summary>
        /// get the per user un-viewed ids.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>the list of keys</returns>
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
        /// list the result keys.
        /// </summary>
        /// <param name="maxResults">The max results.</param>
        /// <param name="start">The start.</param>
        /// <param name="finish">The finish.</param>
        /// <param name="orderBy">order by.</param>
        /// <returns>the list of keys in the result.</returns>
        public IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            var guids = new List<Guid>(); 
            lock (this._profiles)
            {
                int idxStart = 0;
                int idxFinish = this._profiles.Count - 1;
                if (start != null) idxStart = BinaryClosestSearch(start.Value);
                if (finish != null) idxFinish = BinaryClosestSearch(finish.Value);

                if (idxStart < 0) idxStart = 0;
                if (idxFinish >= this._profiles.Count) idxFinish = this._profiles.Count - 1;

                var keys = this._profiles.Keys;

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
        /// The closest binary search.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>The <see cref="int"/>.</returns>
        private int BinaryClosestSearch(DateTime date)
        {
            int lower = 0;
            int upper = this._profiles.Count - 1;

            while (lower <= upper)
            {
                int adjustedIndex = lower + ((upper - lower) >> 1);
                int comparison = this._profiles.Keys[adjustedIndex].Started.CompareTo(date);
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
