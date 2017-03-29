﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Profiling.Helpers;
using StackExchange.Redis;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// StackExchange.Redis based storage provider for <see cref="MiniProfiler"/> results.
    /// </summary>
    public class RedisStorage : IAsyncStorage
    {
        private readonly IDatabase _database;

        /// <summary>
        /// Gets or sets the key prefix for individual profiling results. Default is "MiniProfiler_Result_".
        /// </summary>
        public RedisKey ProfilerResultKeyPrefix { get; set; } = "MiniProfiler_Result_";

        /// <summary>
        /// Gets or sets the list key for the profiling results list. Default is "MiniProfiler_ResultList".
        /// </summary>
        public RedisKey ProfilerResultListKey { get; set; } = "MiniProfiler_ResultList";

        /// <summary>
        /// Gets or sets the key prefix for the per-user set of unviewed profiling results. Default is "MiniProfiler_UnviewedResultSet_".
        /// </summary>
        public RedisKey ProfilerResultUnviewedSetKeyPrefix { get; set; } = "MiniProfiler_UnviewedResultSet_";

        /// <summary>
        /// Gets or sets how long to cache each <see cref="MiniProfiler"/> for, in absolute terms. Default is 60 minutes.
        /// </summary>
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(60);

        /// <summary>
        /// Gets or sets the maximum number of profiling results that will be stored in the profiling list. Default is 100.
        /// </summary>
        public int ResultListMaxLength { get; set; } = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisStorage"/> class with the specified Redis database.
        /// </summary>
        /// <param name="database">The Redis database to use.</param>
        public RedisStorage(IDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
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
            // TODO: start/finish not yet supported - will simply return all results

            IEnumerable<RedisValue> ids;
            switch (orderBy)
            {
                case ListResultsOrder.Ascending:
                    ids = _database.ListRange(ProfilerResultListKey, maxResults + 1, -1).Reverse();
                    break;
                case ListResultsOrder.Descending:
                default:
                    ids = _database.ListRange(ProfilerResultListKey, 0, maxResults - 1);
                    break;
            }
            return ids.Select(x => Guid.Parse(x));
        }

        /// <summary>
        /// Stores <paramref name="profiler"/> under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        /// <remarks>
        /// Should also ensure the profiler is stored as being un-viewed by its profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        public void Save(MiniProfiler profiler)
        {
            var id = profiler.Id.ToString();
            RedisKey key = ProfilerResultKeyPrefix.Append(id);
            RedisValue value = profiler.ToJson();

            _database.StringSet(key, value, expiry: CacheDuration);

            _database.ListLeftPush(ProfilerResultListKey, id);
            _database.ListTrim(ProfilerResultListKey, 0, ResultListMaxLength - 1);
            _database.KeyExpire(ProfilerResultListKey, CacheDuration);
        }

        /// <summary>
        /// Returns a <see cref="MiniProfiler"/> from storage based on <paramref name="id"/>,
        /// which should map to <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        /// <remarks>
        /// Should also update that the resulting profiler has been marked as viewed by its
        /// profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        public MiniProfiler Load(Guid id)
        {
            RedisKey key = ProfilerResultKeyPrefix.Append(id.ToString());
            RedisValue value = _database.StringGet(key);
            return MiniProfiler.FromJson(value);
        }

        /// <summary>
        /// Returns whether or not the storage implementation needs to call <see cref="SetUnviewed(string, Guid)"/>
        /// or <see cref="SetUnviewedAsync(string, Guid)"/> after the initial <see cref="Save(MiniProfiler)"/> or
        /// <see cref="SaveAsync(MiniProfiler)"/> call. For example in a database this is likely false, whereas in
        /// Redis and similar it's likely true (e.g. separately adding the profiler ID to a list).
        /// </summary>
        public bool SetUnviewedAfterSave => true;

        /// <summary>
        /// Sets a particular profiler session so it is considered "un-viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public void SetUnviewed(string user, Guid id)
        {
            RedisKey key = ProfilerResultUnviewedSetKeyPrefix.Append(user);
            RedisValue value = id.ToString();
            _database.SetAdd(key, value);
            _database.KeyExpire(key, CacheDuration);
        }

        /// <summary>
        /// Sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public void SetViewed(string user, Guid id)
        {
            RedisKey key = ProfilerResultUnviewedSetKeyPrefix.Append(user);
            RedisValue value = id.ToString();
            _database.SetRemove(key, value);
        }

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <c>MiniProfiler.Settings.UserProvider</c></param>
        public List<Guid> GetUnviewedIds(string user)
        {
            RedisKey key = ProfilerResultUnviewedSetKeyPrefix.Append(user);
            var ids = _database.SetMembers(key);
            return ids.Select(x => Guid.Parse(x)).ToList();
        }

        /// <summary>
        /// Asynchronously list the latest profiling results.
        /// </summary>
        /// <param name="maxResults">The maximum number of results to return.</param>
        /// <param name="start">(Optional) The start of the date range to fetch.</param>
        /// <param name="finish">(Optional) The end of the date range to fetch.</param>
        /// <param name="orderBy">(Optional) The order to fetch profiler IDs in.</param>
        public async Task<IEnumerable<Guid>> ListAsync(
            int maxResults,
            DateTime? start = null,
            DateTime? finish = null,
            ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            // TODO: start/finish not yet supported - will simply return all results

            IEnumerable<RedisValue> ids;
            switch (orderBy)
            {
                case ListResultsOrder.Ascending:
                    ids = (await _database.ListRangeAsync(ProfilerResultListKey, maxResults + 1, -1).ConfigureAwait(false)).Reverse();
                    break;
                case ListResultsOrder.Descending:
                default:
                    ids = await _database.ListRangeAsync(ProfilerResultListKey, 0, maxResults - 1).ConfigureAwait(false);
                    break;
            }
            return ids.Select(x => Guid.Parse(x));
        }

        /// <summary>
        /// Asynchronously stores <paramref name="profiler"/> under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        /// <remarks>
        /// Should also ensure the profiler is stored as being un-viewed by its profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        public async Task SaveAsync(MiniProfiler profiler)
        {
            var id = profiler.Id.ToString();
            RedisKey key = ProfilerResultKeyPrefix.Append(id);
            RedisValue value = profiler.ToJson();

            await _database.StringSetAsync(key, value, expiry: CacheDuration).ConfigureAwait(false);

            await _database.ListLeftPushAsync(ProfilerResultListKey, id).ConfigureAwait(false);
            await _database.ListTrimAsync(ProfilerResultListKey, 0, ResultListMaxLength - 1).ConfigureAwait(false);
            await _database.KeyExpireAsync(ProfilerResultListKey, CacheDuration).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously returns a <see cref="MiniProfiler"/> from storage based on <paramref name="id"/>,
        /// which should map to <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        /// <remarks>
        /// Should also update that the resulting profiler has been marked as viewed by its
        /// profiling <see cref="MiniProfiler.User"/>.
        /// </remarks>
        public async Task<MiniProfiler> LoadAsync(Guid id)
        {
            RedisKey key = ProfilerResultKeyPrefix.Append(id.ToString());
            RedisValue value = await _database.StringGetAsync(key).ConfigureAwait(false); ;
            return MiniProfiler.FromJson(value);
        }

        /// <summary>
        /// Asynchronously sets a particular profiler session so it is considered "un-viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public async Task SetUnviewedAsync(string user, Guid id)
        {
            RedisKey key = ProfilerResultUnviewedSetKeyPrefix.Append(user);
            RedisValue value = id.ToString();
            await _database.SetAddAsync(key, value).ConfigureAwait(false);
            await _database.KeyExpireAsync(key, CacheDuration).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public async Task SetViewedAsync(string user, Guid id)
        {
            RedisKey key = ProfilerResultUnviewedSetKeyPrefix.Append(user);
            RedisValue value = id.ToString();
            await _database.SetRemoveAsync(key, value).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <c>MiniProfiler.Settings.UserProvider</c></param>
        public async Task<List<Guid>> GetUnviewedIdsAsync(string user)
        {
            RedisKey key = ProfilerResultUnviewedSetKeyPrefix.Append(user);
            var ids = await _database.SetMembersAsync(key).ConfigureAwait(false);
            return ids.Select(x => Guid.Parse(x)).ToList();
        }
    }
}
