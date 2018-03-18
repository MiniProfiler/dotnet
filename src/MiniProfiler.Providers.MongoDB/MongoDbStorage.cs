using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a MongoDb database.
    /// </summary>
    public class MongoDbStorage : IAsyncStorage, IDisposable
    {
        /// <summary>
        /// Gets or sets how we connect to the database used to save/load MiniProfiler results.
        /// </summary>
        protected string ConnectionString { get; set; }

        private IMongoDatabase _db;
        private IMongoDatabase Db
        {
            get
            {
                if (_db == null)
                {
                    var client = new MongoClient(ConnectionString);
                    _db = client.GetDatabase("MiniProfiler");
                }
                return _db;
            }
        }

        private IMongoCollection<MiniProfilerPoco> _profilers;
        private IMongoCollection<MiniProfilerPoco> Profilers => _profilers ?? (_profilers = Db.GetCollection<MiniProfilerPoco>("profilers"));

        private IMongoCollection<TimingPoco> _timings;
        private IMongoCollection<TimingPoco> Timings => _timings ?? (_timings = Db.GetCollection<TimingPoco>("timings"));

        private IMongoCollection<CustomTimingPoco> _customTimings;
        private IMongoCollection<CustomTimingPoco> CustomTimings => _customTimings ?? (_customTimings = Db.GetCollection<CustomTimingPoco>("customtimings"));

        private IMongoCollection<ClientTimingPoco> _clientTimings;
        private IMongoCollection<ClientTimingPoco> ClientTimings => _clientTimings ?? (_clientTimings = Db.GetCollection<ClientTimingPoco>("clienttimings"));

        /// <summary>
        /// Returns a new <see cref="MongoDbStorage"/>. MongoDb connection string will default to "mongodb://localhost"
        /// </summary>
        /// <param name="connectionString">The MongoDB connection string.</param>
        public MongoDbStorage(string connectionString)
        {
            ConnectionString = connectionString;
        }

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <see cref="MiniProfiler.User"/>.</param>
        public List<Guid> GetUnviewedIds(string user) => Profilers.Find(p => p.User == user && !p.HasUserViewed).Project(p => p.Id).ToList();

        /// <summary>
        /// Asynchronously returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <see cref="MiniProfiler.User"/>.</param>
        public async Task<List<Guid>> GetUnviewedIdsAsync(string user)
        {
            var guids = new List<Guid>();
            using (var cursor = await Profilers.FindAsync(p => p.User == user && !p.HasUserViewed).ConfigureAwait(false))
            {
                await cursor.ForEachAsync(profiler => guids.Add(profiler.Id)).ConfigureAwait(false);
            }
            return guids;
        }

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
            var query = FilterDefinition<MiniProfilerPoco>.Empty;

            if (start != null)
            {
                query = Builders<MiniProfilerPoco>.Filter.And(Builders<MiniProfilerPoco>.Filter.Gt(poco => poco.Started, (DateTime)start));
            }
            if (finish != null)
            {
                query = Builders<MiniProfilerPoco>.Filter.And(Builders<MiniProfilerPoco>.Filter.Gt(poco => poco.Started, (DateTime)finish));
            }

            var profilers = Profilers.Find(query).Limit(maxResults);

            profilers = orderBy == ListResultsOrder.Descending
                ? profilers.SortByDescending(p => p.Started)
                : profilers.SortBy(p => p.Started);

            return profilers.Project(p => p.Id).ToList();
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
            var query = FilterDefinition<MiniProfilerPoco>.Empty;

            if (start != null)
            {
                query = Builders<MiniProfilerPoco>.Filter.And(Builders<MiniProfilerPoco>.Filter.Gt(poco => poco.Started, (DateTime)start));
            }
            if (finish != null)
            {
                query = Builders<MiniProfilerPoco>.Filter.And(Builders<MiniProfilerPoco>.Filter.Gt(poco => poco.Started, (DateTime)finish));
            }

            var profilers = Profilers.Find(query).Limit(maxResults);

            profilers = orderBy == ListResultsOrder.Descending
                ? profilers.SortByDescending(p => p.Started)
                : profilers.SortBy(p => p.Started);

            return await profilers.Project(p => p.Id).ToListAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Loads the <c>MiniProfiler</c> identified by 'id' from the database.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public MiniProfiler Load(Guid id)
        {
            var poco = Profilers.Find(p => p.Id == id).FirstOrDefault();
            var miniProfiler = ProfilerPocoToProfiler(poco);

            if (miniProfiler != null)
            {
                var rootTiming = miniProfiler.RootTimingId.HasValue
                    ? LoadTiming(miniProfiler.RootTimingId.Value)
                    : null;

                if (rootTiming != null)
                {
                    miniProfiler.Root = rootTiming;
                }

                miniProfiler.ClientTimings = LoadClientTimings(miniProfiler);
            }

            return miniProfiler;
        }

        /// <summary>
        /// Loads the <c>MiniProfiler</c> identified by 'id' from the database.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public async Task<MiniProfiler> LoadAsync(Guid id)
        {
            var cursor = await Profilers.FindAsync(p => p.Id == id).ConfigureAwait(false);
            var miniProfiler = ProfilerPocoToProfiler(cursor.FirstOrDefault());

            if (miniProfiler != null)
            {
                var rootTiming = miniProfiler.RootTimingId.HasValue
                    ? LoadTiming(miniProfiler.RootTimingId.Value)
                    : null;

                if (rootTiming != null)
                {
                    miniProfiler.Root = rootTiming;
                }

                miniProfiler.ClientTimings = LoadClientTimings(miniProfiler);
            }

            return miniProfiler;
        }

        private Timing LoadTiming(Guid id)
        {
            var timingPoco = Timings.Find(poco => poco.Id == id).FirstOrDefault();
            var timing = TimingPocoToTiming(timingPoco);

            if (timing != null)
            {
                timing.Children = LoadChildrenTimings(timing.Id);
                timing.CustomTimings = LoadCustomTimings(timing.Id);
            }

            return timing;
        }

        private List<Timing> LoadChildrenTimings(Guid parentId)
        {
            var childrenTimings =
                    Timings.Find(poco => poco.ParentTimingId == parentId)
                        .SortBy(poco => poco.StartMilliseconds)
                        .ToList()
                        .Select(TimingPocoToTiming)
                        .ToList();

            childrenTimings.ForEach(timing =>
            {
                timing.Children = LoadChildrenTimings(timing.Id);
                timing.CustomTimings = LoadCustomTimings(timing.Id);
            });

            return childrenTimings;
        }

        private Dictionary<string, List<CustomTiming>> LoadCustomTimings(Guid timingId)
        {
            var customTimingPocos = CustomTimings
                .Find(poco => poco.TimingId == timingId)
                .ToList();

            return customTimingPocos
                .GroupBy(poco => poco.Key)
                .ToDictionary(grp => grp.Key,
                    grp => grp.OrderBy(poco => poco.StartMilliseconds)
                        .Select(CustomTimingPocoToCustomTiming).ToList());
        }

        private ClientTimings LoadClientTimings(MiniProfiler profiler)
        {
            var timings = ClientTimings
                .Find(poco => poco.MiniProfilerId == profiler.Id)
                .ToList()
                .Select(ClientTimingPocoToClientTiming)
                .ToList();

            timings.ForEach(timing => timing.MiniProfilerId = profiler.Id);

            if (timings.Count > 0 || profiler.ClientTimingsRedirectCount.HasValue)
            {
                return new ClientTimings
                {
                    Timings = timings,
                    RedirectCount = profiler.ClientTimingsRedirectCount ?? 0
                };
            }

            return null;
        }

        /// <summary>
        /// Stores to <c>profilers</c> under its <see cref="MiniProfiler.Id"/>;
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public void Save(MiniProfiler profiler)
        {
            var miniProfilerPoco = GetPoco(profiler);
            Profilers.InsertOne(miniProfilerPoco);

            void Insert<T>(IMongoCollection<T> coll, IEnumerable<T> source)
            {
                var items = source.ToList();
                if (items.Count > 0)
                {
                    coll.InsertMany(items);
                }
            }

            Insert(Timings, GetTimingPocos(profiler));
            Insert(CustomTimings, GetCustomTimingPocos(profiler));
            Insert(ClientTimings, GetClientTimingPocos(profiler));
        }

        /// <summary>
        /// Asynchronously stores to <c>profilers</c> under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public async Task SaveAsync(MiniProfiler profiler)
        {
            var miniProfilerPoco = GetPoco(profiler);
            await Profilers.WithWriteConcern(WriteConcern.Acknowledged).InsertOneAsync(miniProfilerPoco).ConfigureAwait(false);

            async Task Insert<T>(IMongoCollection<T> coll, IEnumerable<T> source)
            {
                var items = source.ToList();
                if (items.Count > 0)
                {
                    await coll.WithWriteConcern(WriteConcern.Acknowledged).InsertManyAsync(items).ConfigureAwait(false);
                }
            }

            await Insert(Timings, GetTimingPocos(profiler)).ConfigureAwait(false);
            await Insert(CustomTimings, GetCustomTimingPocos(profiler)).ConfigureAwait(false);
            await Insert(ClientTimings, GetClientTimingPocos(profiler)).ConfigureAwait(false);
        }

        private MiniProfilerPoco GetPoco(MiniProfiler profiler) => new MiniProfilerPoco
        {
            Id = profiler.Id,
            RootTimingId = profiler.Root != null ? profiler.Root.Id : (Guid?)null,
            Name = profiler.Name,
            Started = profiler.Started,
            DurationMilliseconds = (double)profiler.DurationMilliseconds,
            User = profiler.User,
            HasUserViewed = profiler.HasUserViewed,
            MachineName = profiler.MachineName,
            CustomLinksJson = profiler.CustomLinksJson,
            ClientTimingsRedirectCounts = profiler.ClientTimings != null ? profiler.ClientTimings.RedirectCount : (int?)null
        };

        private IEnumerable<TimingPoco> GetTimingPocos(MiniProfiler profiler)
        {
            foreach (var timing in profiler.GetTimingHierarchy())
            {
                yield return new TimingPoco
                {
                    Id = timing.Id,
                    ParentTimingId = timing.ParentTimingId,
                    Name = timing.Name,
                    StartMilliseconds = (double)timing.StartMilliseconds,
                    DurationMilliseconds = (double?)timing.DurationMilliseconds
                };
            }
        }

        private IEnumerable<CustomTimingPoco> GetCustomTimingPocos(MiniProfiler profiler)
        {
            foreach (var timing in profiler.GetTimingHierarchy())
            {
                if (timing.CustomTimings?.Count > 0)
                {
                    foreach (var kvp in timing.CustomTimings)
                    {
                        foreach (var entry in kvp.Value)
                        {
                            yield return new CustomTimingPoco
                            {
                                Id = entry.Id,
                                Key = kvp.Key,
                                TimingId = timing.Id,
                                CommandString = entry.CommandString,
                                ExecuteType = entry.ExecuteType,
                                StackTraceSnippet = entry.StackTraceSnippet,
                                StartMilliseconds = entry.StartMilliseconds,
                                DurationMilliseconds = entry.DurationMilliseconds,
                                FirstFetchDurationMilliseconds = entry.FirstFetchDurationMilliseconds
                            };
                        }
                    }
                }
            }
        }

        private IEnumerable<ClientTimingPoco> GetClientTimingPocos(MiniProfiler profiler)
        {
            if (profiler.ClientTimings?.Timings?.Count > 0)
            {
                profiler.ClientTimings.Timings.ForEach(x =>
                {
                    x.MiniProfilerId = profiler.Id;
                    x.Id = Guid.NewGuid();
                });

                foreach (var clientTiming in profiler.ClientTimings.Timings)
                {
                    yield return new ClientTimingPoco
                    {
                        Id = clientTiming.Id,
                        MiniProfilerId = clientTiming.MiniProfilerId,
                        Name = clientTiming.Name,
                        Start = (double)clientTiming.Start,
                        Duration = (double)clientTiming.Duration
                    };
                }
            }
        }

        /// <summary>
        /// Sets a particular profiler session so it is considered "unviewed"  
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public void SetUnviewed(string user, Guid id)
        {
            var set = Builders<MiniProfilerPoco>.Update.Set(poco => poco.HasUserViewed, false);
            Profilers.UpdateOne(p => p.Id == id, set);
        }

        /// <summary>
        /// Asynchronously sets a particular profiler session so it is considered "unviewed"  
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public async Task SetUnviewedAsync(string user, Guid id)
        {
            var set = Builders<MiniProfilerPoco>.Update.Set(poco => poco.HasUserViewed, false);
            await Profilers.UpdateOneAsync(p => p.Id == id, set).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public void SetViewed(string user, Guid id)
        {
            var set = Builders<MiniProfilerPoco>.Update.Set(poco => poco.HasUserViewed, true);
            Profilers.UpdateOne(p => p.Id == id, set);
        }

        /// <summary>
        /// Asynchronously sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public async Task SetViewedAsync(string user, Guid id)
        {
            var set = Builders<MiniProfilerPoco>.Update.Set(poco => poco.HasUserViewed, true);
            await Profilers.UpdateOneAsync(p => p.Id == id, set).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns a client to MongoDB Server.
        /// </summary>
        public MongoClient GetClient() => new MongoClient(ConnectionString);

        /// <summary>
        /// Disposes the database connection, if present.
        /// </summary>
        public void Dispose() {}

        private static MiniProfiler ProfilerPocoToProfiler(MiniProfilerPoco profilerPoco)
        {
            if (profilerPoco == null)
                return null;

#pragma warning disable CS0618 // Type or member is obsolete
            return new MiniProfiler()
            {
                Id = profilerPoco.Id,
                MachineName = profilerPoco.MachineName,
                User = profilerPoco.User,
                HasUserViewed = profilerPoco.HasUserViewed,
                Name = profilerPoco.Name,
                Started = profilerPoco.Started,
                RootTimingId = profilerPoco.RootTimingId,
                DurationMilliseconds = (decimal)profilerPoco.DurationMilliseconds
            };
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private static Timing TimingPocoToTiming(TimingPoco timingPoco)
        {
            if (timingPoco == null)
                return null;

#pragma warning disable CS0618 // Type or member is obsolete
            return new Timing
            {
                Id = timingPoco.Id,
                Name = timingPoco.Name,
                StartMilliseconds = (decimal)timingPoco.StartMilliseconds,
                DurationMilliseconds = (decimal)timingPoco.DurationMilliseconds,
            };
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private static CustomTiming CustomTimingPocoToCustomTiming(CustomTimingPoco customTimingPoco)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return new CustomTiming
            {
                CommandString = customTimingPoco.CommandString,
                DurationMilliseconds = customTimingPoco.DurationMilliseconds,
                FirstFetchDurationMilliseconds = customTimingPoco.FirstFetchDurationMilliseconds,
                StartMilliseconds = customTimingPoco.StartMilliseconds,
                ExecuteType = customTimingPoco.ExecuteType,
                StackTraceSnippet = customTimingPoco.StackTraceSnippet
            };
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private static ClientTiming ClientTimingPocoToClientTiming(ClientTimingPoco clientTimingPoco)
        {
            return new ClientTiming
            {
                Id = clientTimingPoco.Id,
                Duration = (decimal)clientTimingPoco.Duration,
                Name = clientTimingPoco.Name,
                Start = (decimal)clientTimingPoco.Start
            };
        }

        private class MiniProfilerPoco
        {
            [BsonId]
            public Guid Id { get; set; }
            public string Name { get; set; }
            public DateTime Started { get; set; }
            public string MachineName { get; set; }
            public string User { get; set; }
            public Guid? RootTimingId { get; set; }
            public double DurationMilliseconds { get; set; }
            public string CustomLinksJson { get; set; }
            public int? ClientTimingsRedirectCounts { get; set; }
            public bool HasUserViewed { get; set; }
        }

        private class TimingPoco
        {
            [BsonId]
            public Guid Id { get; set; }
            public Guid? ParentTimingId { get; set; }
            public string Name { get; set; }
            public double StartMilliseconds { get; set; }
            public double? DurationMilliseconds { get; set; }
        }

        private class CustomTimingPoco
        {
            [BsonId]
            public Guid Id { get; set; }
            public string Key { get; set; }
            public Guid TimingId { get; set; }
            public string CommandString { get; set; }
            public string ExecuteType { get; set; }
            public string StackTraceSnippet { get; set; }
            public decimal StartMilliseconds { get; set; }
            public decimal? DurationMilliseconds { get; set; }
            public decimal? FirstFetchDurationMilliseconds { get; set; }
        }

        private class ClientTimingPoco
        {
            [BsonId]
            public Guid Id { get; set; }
            public Guid MiniProfilerId { get; set; }
            public string Name { get; set; }
            public double Start { get; set; }
            public double Duration { get; set; }
        }
    }
}
