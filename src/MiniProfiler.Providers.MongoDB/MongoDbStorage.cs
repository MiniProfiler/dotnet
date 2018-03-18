using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a MongoDb database.
    /// </summary>
    public class MongoDbStorage : IAsyncStorage, IDisposable
    {
        private readonly MongoClient _client;
        private readonly IMongoCollection<MiniProfiler> _collection;

        /// <summary>
        /// Returns a new <see cref="MongoDbStorage"/>. MongoDb connection string will default to "mongodb://localhost"
        /// </summary>
        /// <param name="connectionString">The MongoDB connection string.</param>
        public MongoDbStorage(string connectionString)
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(MiniProfiler)))
            {
                BsonClassMapFields();
            }

            _client = new MongoClient(connectionString);
            _collection = _client
                .GetDatabase("MiniProfiler")
                .GetCollection<MiniProfiler>("profilers");
        }

        private static void BsonClassMapFields()
        {
            BsonClassMap.RegisterClassMap<MiniProfiler>(
                map =>
                {
                    map.MapIdField(c => c.Id);
                    map.MapField(c => c.Name);
                    map.MapField(c => c.Started);
                    map.MapField(c => c.DurationMilliseconds);
                    map.MapField(c => c.MachineName);
                    map.MapField(c => c.CustomLinks);
                    map.MapField(c => c.Root);
                    map.MapField(c => c.ClientTimings);
                    map.MapField(c => c.User);
                    map.MapField(c => c.HasUserViewed);
                });

            BsonClassMap.RegisterClassMap<ClientTiming>(
                map =>
                {
                    map.MapField(x => x.Name);
                    map.MapField(x => x.Start);
                    map.MapField(x => x.Duration);
                });

            BsonClassMap.RegisterClassMap<CustomTiming>(
                map =>
                {
                    map.MapField(x => x.Id);
                    map.MapField(x => x.CommandString);
                    map.MapField(x => x.ExecuteType);
                    map.MapField(x => x.StackTraceSnippet);
                    map.MapField(x => x.StartMilliseconds);
                    map.MapField(x => x.DurationMilliseconds);
                    map.MapField(x => x.FirstFetchDurationMilliseconds);
                    map.MapField(x => x.Errored);
                });

            BsonClassMap.RegisterClassMap<Timing>(
                map =>
                {
                    map.MapField(x => x.Id);
                    map.MapField(x => x.Name);
                    map.MapField(x => x.DurationMilliseconds);
                    map.MapField(x => x.StartMilliseconds);
                    map.MapField(x => x.Children);
                    map.MapField(x => x.CustomTimings);
                });
        }

        public MongoDbStorage WithIndexCreation()
        {
            _collection.Indexes.CreateOne(Builders<MiniProfiler>.IndexKeys.Ascending(_ => _.Started));
            _collection.Indexes.CreateOne(Builders<MiniProfiler>.IndexKeys.Ascending(_ => _.HasUserViewed));
            return this;
        }

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <see cref="MiniProfiler.User"/>.</param>
        public List<Guid> GetUnviewedIds(string user) => _collection.Find(p => p.User == user && !p.HasUserViewed).Project(p => p.Id).ToList();

        /// <summary>
        /// Asynchronously returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <see cref="MiniProfiler.User"/>.</param>
        public Task<List<Guid>> GetUnviewedIdsAsync(string user) => _collection.Find(p => p.User == user && !p.HasUserViewed).Project(p => p.Id).ToListAsync();

        /// <summary>
        /// List the MiniProfiler Ids for the given search criteria.
        /// </summary>
        /// <param name="maxResults">The max number of results</param>
        /// <param name="start">Search window start</param>
        /// <param name="finish">Search window end</param>
        /// <param name="orderBy">Result order</param>
        /// <returns>The list of GUID keys</returns>
        public IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            return GetListQuery(maxResults, start, finish, orderBy).ToList();
        }

        /// <summary>
        /// Asynchronously returns the MiniProfiler Ids for the given search criteria.
        /// </summary>
        /// <param name="maxResults">The max number of results</param>
        /// <param name="start">Search window start</param>
        /// <param name="finish">Search window end</param>
        /// <param name="orderBy">Result order</param>
        /// <returns>The list of GUID keys</returns>
        public async Task<IEnumerable<Guid>> ListAsync(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            return await GetListQuery(maxResults, start, finish, orderBy).ToListAsync().ConfigureAwait(false);
        }

        private IFindFluent<MiniProfiler, Guid> GetListQuery(int maxResults, DateTime? start, DateTime? finish, ListResultsOrder orderBy)
        {
            var query = FilterDefinition<MiniProfiler>.Empty;

            if (start != null)
            {
                query = Builders<MiniProfiler>.Filter.And(Builders<MiniProfiler>.Filter.Gte(profiler => profiler.Started, (DateTime)start));
            }

            if (finish != null)
            {
                query = Builders<MiniProfiler>.Filter.And(Builders<MiniProfiler>.Filter.Lte(profiler => profiler.Started, (DateTime)finish));
            }

            var profilers = _collection.Find(query).Limit(maxResults);

            profilers = orderBy == ListResultsOrder.Descending
                ? profilers.SortByDescending(p => p.Started)
                : profilers.SortBy(p => p.Started);

            return profilers.Project(p => p.Id);
        }

        /// <summary>
        /// Loads the <c>MiniProfiler</c> identified by 'id' from the database.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public MiniProfiler Load(Guid id) => _collection.Find(p => p.Id == id).FirstOrDefault();

        /// <summary>
        /// Loads the <c>MiniProfiler</c> identified by 'id' from the database.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public Task<MiniProfiler> LoadAsync(Guid id) => _collection.Find(p => p.Id == id).FirstOrDefaultAsync();

        /// <summary>
        /// Stores to <c>profilers</c> under its <see cref="MiniProfiler.Id"/>;
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public void Save(MiniProfiler profiler)
        {
            _collection.ReplaceOne(
                p => p.Id == profiler.Id,
                profiler,
                new UpdateOptions
                {
                    IsUpsert = true
                });
        }

        /// <summary>
        /// Asynchronously stores to <c>profilers</c> under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public Task SaveAsync(MiniProfiler profiler)
        {
            return _collection.ReplaceOneAsync(
                p => p.Id == profiler.Id,
                profiler,
                new UpdateOptions
                {
                    IsUpsert = true
                });
        }

        /// <summary>
        /// Sets a particular profiler session so it is considered "unviewed"  
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public void SetUnviewed(string user, Guid id)
        {
            var set = Builders<MiniProfiler>.Update.Set(profiler => profiler.HasUserViewed, false);
            _collection.UpdateOne(p => p.Id == id, set);
        }

        /// <summary>
        /// Asynchronously sets a particular profiler session so it is considered "unviewed"  
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public async Task SetUnviewedAsync(string user, Guid id)
        {
            var set = Builders<MiniProfiler>.Update.Set(profiler => profiler.HasUserViewed, false);
            await _collection.UpdateOneAsync(p => p.Id == id, set).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public void SetViewed(string user, Guid id)
        {
            var set = Builders<MiniProfiler>.Update.Set(profiler => profiler.HasUserViewed, true);
            _collection.UpdateOne(p => p.Id == id, set);
        }

        /// <summary>
        /// Asynchronously sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public async Task SetViewedAsync(string user, Guid id)
        {
            var set = Builders<MiniProfiler>.Update.Set(profiler => profiler.HasUserViewed, true);
            await _collection.UpdateOneAsync(p => p.Id == id, set).ConfigureAwait(false);
        }

        public MongoClient GetClient() => _client;

        /// <summary>
        /// Disposes the database connection, if present.
        /// </summary>
        public void Dispose() { }
    }
}
