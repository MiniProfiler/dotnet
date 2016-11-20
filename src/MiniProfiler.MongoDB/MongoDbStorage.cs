using System;
using System.Collections.Generic;
using System.Linq;

using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling.MongoDB
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a MongoDb database.
    /// </summary>
    public class MongoDbStorage : DatabaseStorageBase
    {
        private IMongoDatabase _db;
        private IMongoDatabase Db =>
            _db ?? (_db = new MongoClient(ConnectionString).GetDatabase("MiniProfiler"));

        private IMongoCollection<MiniProfilerPoco> _profilers;
        private IMongoCollection<MiniProfilerPoco> Profilers =>
            _profilers ?? (_profilers = Db.GetCollection<MiniProfilerPoco>("profilers"));

        private IMongoCollection<TimingPoco> _timings;
        private IMongoCollection<TimingPoco> Timings =>
            _timings ?? (_timings = Db.GetCollection<TimingPoco>("timings"));

        private IMongoCollection<CustomTimingPoco> _customTimings;
        private IMongoCollection<CustomTimingPoco> CustomTimings =>
            _customTimings ?? (_customTimings = Db.GetCollection<CustomTimingPoco>("customtimings"));

        private IMongoCollection<ClientTimingPoco> _clientTimings;
        private IMongoCollection<ClientTimingPoco> ClientTimings =>
            _clientTimings ?? (_clientTimings = Db.GetCollection<ClientTimingPoco>("clienttimings"));
        
        /// <summary>
        /// Returns a new <see cref="MongoDbStorage"/>. MongoDb connection string will default to "mongodb://localhost"
        /// </summary>
        public MongoDbStorage(string connectionString) : base(connectionString) { }

        /// <summary>
        /// Stores <param name="profiler"/> to MongoDB under its <see cref="MiniProfiler.Id"/>; 
        /// stores all child Timings and SqlTimings to their respective tables.
        /// </summary>
        public override void Save(MiniProfiler profiler)
        {
            var miniProfilerPoco = MiniProfilerPoco.FromProfiler(profiler);
            var result = Profilers.Save(miniProfilerPoco);

            if (!result.UpdatedExisting)
            {
                SaveTiming(profiler.Root);
            }

            SaveClientTimings(profiler);
        }

        private void SaveTiming(Timing timing)
        {
            var rootTiming = TimingPoco.FromTiming(timing);
            Timings.InsertOne(rootTiming);

            if (timing.HasChildren)
            {
                foreach (var child in timing.Children)
                {
                    SaveTiming(child);
                }
            }

            if (timing.HasCustomTimings)
            {
                foreach (var customTimingsKV in timing.CustomTimings)
                {
                    SaveCustomTimings(timing, customTimingsKV);
                }
            }
        }

        private void SaveClientTimings(MiniProfiler profiler)
        {
            if (profiler.ClientTimings == null || profiler.ClientTimings.Timings == null || !profiler.ClientTimings.Timings.Any())
                return;

            foreach (var clientTiming in profiler.ClientTimings.Timings)
            {
                clientTiming.MiniProfilerId = profiler.Id;
                clientTiming.Id = Guid.NewGuid();

                var clientTimingPoco = ClientTimingPoco.FromClientTiming(clientTiming);
                ClientTimings.InsertOne(clientTimingPoco);
            }
        }

        private void SaveCustomTimings(Timing timing, KeyValuePair<string, List<CustomTiming>> customTimingsKV)
        {
            var key = customTimingsKV.Key;
            var customTimings = customTimingsKV.Value;

            foreach (var customTiming in customTimings)
            {
                var customTimingPoco = CustomTimingPoco.FromCustomTiming(timing, key, customTiming);
                CustomTimings.InsertOne(customTimingPoco);
            }
        }

        /// <summary>
        /// Loads the MiniProfiler identifed by 'id' from the database.
        /// </summary>
        public override MiniProfiler Load(Guid id)
        {
            var profilerPoco = Profilers.Find(Builders<MiniProfilerPoco>.Filter.Eq(poco => poco.Id, id)).FirstOrDefault();
            var miniProfiler = ProfilerPocoToProfiler(profilerPoco);

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
            var timingPoco = Timings.Find(Builders<TimingPoco>.Filter.Eq(poco => poco.Id, id)).FirstOrDefault();
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
                    Timings.Find(Builders<TimingPoco>.Filter.Eq(poco => poco.ParentTimingId, parentId))
                        .Sort(Builders<TimingPoco>.Sort.Ascending(poco => poco.StartMilliseconds))
                        .ToList()
                        .Select(TimingPocoToTiming)
                        .ToList();

            // TODO: Holy crap this is slow, can we do this not n+1 in Mongo?
            foreach(var timing in childrenTimings)
            {
                timing.Children = LoadChildrenTimings(timing.Id);
                timing.CustomTimings = LoadCustomTimings(timing.Id);
            }

            return childrenTimings;
        }

        private Dictionary<string, List<CustomTiming>> LoadCustomTimings(Guid timingId)
        {
            var customTimingPocos = CustomTimings
                .Find(Builders<CustomTimingPoco>.Filter.Eq(poco => poco.TimingId, timingId))
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
                .Find(Builders<ClientTimingPoco>.Filter.Eq(poco => poco.MiniProfilerId, profiler.Id))
                .ToList()
                .Select(ClientTimingPocoToClientTiming)
                .ToList();

            timings.ForEach(timing => timing.MiniProfilerId = profiler.Id);

            if (timings.Any() || profiler.ClientTimingsRedirectCount.HasValue)
                return new ClientTimings
                {
                    Timings = timings,
                    RedirectCount = profiler.ClientTimingsRedirectCount ?? 0
                };

            return null;
        }

        #region Mapping

        private static MiniProfiler ProfilerPocoToProfiler(MiniProfilerPoco profilerPoco)
        {
            if (profilerPoco == null)
                return null;

#pragma warning disable 618
            var miniProfiler = new MiniProfiler
#pragma warning restore 618
            {
                Id = profilerPoco.Id,
                MachineName = profilerPoco.MachineName,
                User = profilerPoco.User,
                HasUserViewed = profilerPoco.HasUserViewed,
                Name = profilerPoco.Name,
                Started = profilerPoco.Started,
                RootTimingId = profilerPoco.RootTimingId,
                DurationMilliseconds = (decimal) profilerPoco.DurationMilliseconds
            };

            return miniProfiler;
        }

        private static Timing TimingPocoToTiming(TimingPoco timingPoco)
        {
            if (timingPoco == null)
                return null;

#pragma warning disable 618
            return new Timing
#pragma warning restore 618
            {
                Id = timingPoco.Id,
                Name = timingPoco.Name,
                StartMilliseconds = (decimal) timingPoco.StartMilliseconds,
                DurationMilliseconds = (decimal) timingPoco.DurationMilliseconds,
            };
        }

        private static CustomTiming CustomTimingPocoToCustomTiming(CustomTimingPoco customTimingPoco)
        {
#pragma warning disable 618
            return new CustomTiming
#pragma warning restore 618
            {
                CommandString = customTimingPoco.CommandString,
                DurationMilliseconds = customTimingPoco.DurationMilliseconds,
                FirstFetchDurationMilliseconds = customTimingPoco.FirstFetchDurationMilliseconds,
                StartMilliseconds = customTimingPoco.StartMilliseconds,
                ExecuteType = customTimingPoco.ExecuteType,
                StackTraceSnippet = customTimingPoco.StackTraceSnippet
            };
        }

        private static ClientTimings.ClientTiming ClientTimingPocoToClientTiming(ClientTimingPoco clientTimingPoco)
        {
            return new ClientTimings.ClientTiming
            {
                Id = clientTimingPoco.Id,
                Duration = (decimal) clientTimingPoco.Duration,
                Name = clientTimingPoco.Name,
                Start = (decimal) clientTimingPoco.Start
            };
        }

        #endregion

        /// <summary>
        /// Sets the profiler as unviewed
        /// </summary>
        public override void SetUnviewed(string user, Guid id)
        {
            Profilers.UpdateOne(Builders<MiniProfilerPoco>.Filter.Eq("_id", id.ToString()),
                Builders<MiniProfilerPoco>.Update.Set(poco => poco.HasUserViewed, false));
        }

        /// <summary>
        /// Sets the profiler as view
        /// </summary>
        public override void SetViewed(string user, Guid id)
        {
            Profilers.UpdateOne(Builders<MiniProfilerPoco>.Filter.Eq("_id", id.ToString()),
                Builders<MiniProfilerPoco>.Update.Set(poco => poco.HasUserViewed, true));
        }

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <see cref="MiniProfiler.Settings.UserProvider"/>.</param>
        public override List<Guid> GetUnviewedIds(string user)
        {
            var query = Builders<MiniProfilerPoco>.Filter.And(
                    Builders<MiniProfilerPoco>.Filter.Eq(p => p.User, user),
                    Builders<MiniProfilerPoco>.Filter.Eq(p => p.HasUserViewed, false));
            var guids = Profilers.Find(query).ToList().Select(p => p.Id).ToList();
            return guids;
        }

        public override IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            FilterDefinition<MiniProfilerPoco> query = null;

            // TODO: This is bugged, as finish would win, rather than applying both;
            if (start != null)
            {
                query = Builders<MiniProfilerPoco>.Filter.Gt(nameof(MiniProfilerPoco.Started), (DateTime)start);
            }
            if (finish != null)
            {
                query = Builders<MiniProfilerPoco>.Filter.Lt(nameof(MiniProfilerPoco.Started), (DateTime)finish);
            }

            var profilers = Profilers.Find(query).Limit(maxResults).ToEnumerable();

            profilers = orderBy == ListResultsOrder.Descending
                ? profilers.OrderByDescending(p => p.Started)
                : profilers.OrderBy(p => p.Started);

            return profilers.Select(p => p.Id);
        }

        #region Poco Classes

        //In order to use Guids as the Id in MongoDb we have to use a strongly typed class and the [BsonId] attribute on the Id.  Otherwise Mongo defaults to ObjectIds which cannot be cast to Guids.

        class MiniProfilerPoco
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

            public static MiniProfilerPoco FromProfiler(MiniProfiler profiler)
            {
                return new MiniProfilerPoco
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
            }
        }

        class TimingPoco
        {
            [BsonId]
            public Guid Id { get; set; }
            public Guid? ParentTimingId { get; set; }
            public string Name { get; set; }
            public double StartMilliseconds { get; set; }
            public double DurationMilliseconds { get; set; }

            public static TimingPoco FromTiming(Timing timing)
            {
                return new TimingPoco
                {
                    Id = timing.Id,
                    ParentTimingId = timing.IsRoot ? (Guid?)null : timing.ParentTiming.Id,
                    Name = timing.Name,
                    StartMilliseconds = (double)timing.StartMilliseconds,
                    DurationMilliseconds = (double)timing.DurationMilliseconds
                };
            }
        }

        class CustomTimingPoco
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

            public static CustomTimingPoco FromCustomTiming(Timing timing, string key, CustomTiming customTiming)
            {
                return new CustomTimingPoco
                {
                    Id = customTiming.Id,
                    Key = key,
                    TimingId = timing.Id,
                    CommandString = customTiming.CommandString,
                    ExecuteType = customTiming.ExecuteType,
                    StackTraceSnippet = customTiming.StackTraceSnippet,
                    StartMilliseconds = customTiming.StartMilliseconds,
                    DurationMilliseconds = customTiming.DurationMilliseconds,
                    FirstFetchDurationMilliseconds = customTiming.FirstFetchDurationMilliseconds
                };
            }
        }

        class ClientTimingPoco
        {
            [BsonId]
            public Guid Id { get; set; }
            public Guid MiniProfilerId { get; set; }
            public string Name { get; set; }
            public double Start { get; set; }
            public double Duration { get; set; }

            public static ClientTimingPoco FromClientTiming(ClientTimings.ClientTiming clientTiming)
            {
                return new ClientTimingPoco
                {
                    Id = clientTiming.Id,
                    MiniProfilerId = clientTiming.MiniProfilerId,
                    Name = clientTiming.Name,
                    Start = (double)clientTiming.Start,
                    Duration = (double)clientTiming.Duration
                };
            }
        }

        #endregion
    }
}
