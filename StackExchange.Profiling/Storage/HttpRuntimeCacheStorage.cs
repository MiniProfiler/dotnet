using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to the <see cref="System.Web.HttpRuntime.Cache"/> with absolute expiration.
    /// </summary>
    public class HttpRuntimeCacheStorage : IStorage
    {
        // FYI: SortedList on uses the comparer for both key lookups and insertion
        class ProfileInfo : IComparable<ProfileInfo>
        {
            public DateTime Started { get; set; }
            public Guid Id { get; set; }

            public int CompareTo(ProfileInfo other)
            {
                var comp = Started.CompareTo(other.Started);
                if (comp == 0) comp = Id.CompareTo(other.Id);
                return comp;
            }
        }

        SortedList<ProfileInfo, object> profiles = new SortedList<ProfileInfo, object>();

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
        public void Save(MiniProfiler profiler)
        {
            InsertIntoCache(GetCacheKey(profiler.Id), profiler);
            
            lock (profiles)
            {
                var profileInfo = new ProfileInfo { Id = profiler.Id, Started = profiler.Started };
                if (profiles.IndexOfKey(profileInfo) < 0) 
                {
                    profiles.Add(profileInfo, null);
                }

                while (profiles.Count > 0)
                {
                    var first = profiles.Keys[0];
                    if (first.Started < DateTime.UtcNow.Add(-CacheDuration))
                    {
                        profiles.RemoveAt(0);
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
        public MiniProfiler Load(Guid id)
        {
            var result = HttpRuntime.Cache[GetCacheKey(id)] as MiniProfiler;
            return result;
        }

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <see cref="MiniProfiler.Settings.UserProvider"/>.</param>
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

        private string GetCacheKey(Guid id)
        {
            return CacheKeyPrefix + id;
        }

        private string GetPerUserUnviewedCacheKey(string user)
        {
            return CacheKeyPrefix + "unviewed-for-user-" + user;
        }

        private List<Guid> GetPerUserUnviewedIds(MiniProfiler profiler)
        {
            return GetPerUserUnviewedIds(profiler.User);
        }

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

        public IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Decending)
        {
            List<Guid> guids = new List<Guid>(); 
            lock (profiles)
            {
                int idxStart = 0;
                int idxFinish = profiles.Count - 1;
                if (start != null) idxStart = BinaryClosestSearch(start.Value);
                if (finish != null) idxFinish = BinaryClosestSearch(finish.Value);

                if (idxStart < 0) idxStart = 0;
                if (idxFinish >= profiles.Count) idxFinish = profiles.Count - 1;

                var keys = profiles.Keys;

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
            int upper = profiles.Count - 1;

            while (lower <= upper) {
                int adjustedIndex = lower + ((upper - lower) >> 1);
                int comparison = profiles.Keys[adjustedIndex].Started.CompareTo(date);
                if (comparison == 0)
                    return adjustedIndex;
                else if (comparison < 0)
                    lower = adjustedIndex + 1;
                else
                    upper = adjustedIndex - 1;
            }
            return lower;
        }

        /// <summary>
        /// Syncs access to runtime cache when adding a new list of ids for a user.
        /// </summary>
        private static readonly object AddPerUserUnviewedIdsLock = new object();

        
    }
}
