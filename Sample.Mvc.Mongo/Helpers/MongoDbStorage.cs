using System;
using System.Collections.Generic;
using System.Linq;

using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using StackExchange.Profiling.Helpers;
using StackExchange.Profiling.Data;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a MongoDb database.
    /// </summary>
    public class MongoDbStorage : DatabaseStorageBase
    {
        private string mongoConnectionString;

        #region Collections

        private MongoDatabase _db;
        private MongoDatabase Db
        {
            get
            {
                if (_db == null)
                {
                    MongoServer server = MongoServer.Create(mongoConnectionString);
                    _db = server.GetDatabase("MiniProfiler");
                }
                return _db;
            }
        }

        private MongoCollection<MiniProfilerPoco> _profilers;
        private MongoCollection<MiniProfilerPoco> Profilers
        {
            get
            {
                if (_profilers == null)
                {
                    _profilers = Db.GetCollection<MiniProfilerPoco>("profilers");
                }
                return _profilers;
            }
        }

        private MongoCollection<TimingPoco> _timings;
        private MongoCollection<TimingPoco> Timings
        {
            get
            {
                if (_timings == null)
                {
                    _timings = Db.GetCollection<TimingPoco>("timings");
                }
                return _timings;
            }            
        }
        
        private MongoCollection<SqlTimingPoco> _sqltimings;
        private MongoCollection<SqlTimingPoco> SqlTimings
        {
            get
            {
                if (_sqltimings == null)
                {
                    _sqltimings = Db.GetCollection<SqlTimingPoco>("sqltimings");
                }
                return _sqltimings;
            }
        }

        private MongoCollection<SqlTimingParameterPoco> _sqltimingparamss;
        private MongoCollection<SqlTimingParameterPoco> SqlTimingParams
        {
            get
            {
                if (_sqltimingparamss == null)
                {
                    _sqltimingparamss = Db.GetCollection<SqlTimingParameterPoco>("sqltimingparams");
                }
                return _sqltimingparamss;
            }
        }

        private MongoCollection<ClientTimingPoco> _clientTimings;
        private MongoCollection<ClientTimingPoco> ClientTimings
        {
            get
            {
                if (_clientTimings == null)
                {
                    _clientTimings = Db.GetCollection<ClientTimingPoco>("clienttimings");
                }
                return _clientTimings;
            }
        }

        #endregion

        /// <summary>
        /// Returns a new <see cref="MongoDbStorage"/>. MongoDb connection string will default to "mongodb://localhost"
        /// </summary>
        public MongoDbStorage(string connectionString)
            :base(connectionString)
        {
            mongoConnectionString = "mongodb://localhost";

        }

        /// <summary>
        /// Returns a new <see cref="MongoDbStorage"/>.
        /// </summary>
        public MongoDbStorage(string connectionString, string mongodbConnectionString)
            : base(connectionString)
        {
            mongoConnectionString = mongodbConnectionString;
   
        }

        /// <summary>
        /// Stores <param name="profiler"/> to MongoDB under its <see cref="MiniProfiler.Id"/>; 
        /// stores all child Timings and SqlTimings to their respective tables.
        /// </summary>
        public override void Save(MiniProfiler profiler)
        {
            var profilerPoco = new MiniProfilerPoco
            {
                Id = profiler.Id.ToString(),
                Name = Truncate(profiler.Name, 200),
                Started = profiler.Started,
                MachineName = Truncate(profiler.MachineName, 100),
                User = Truncate(profiler.User, 100),
                Level = profiler.Level,
                RootTimingId = profiler.Root.Id,
                DurationMilliseconds = (double)profiler.DurationMilliseconds,
                DurationMillisecondsInSql = (double)profiler.DurationMillisecondsInSql,
                HasSqlTimings = profiler.HasSqlTimings,
                HasDuplicateSqlTimings = profiler.HasDuplicateSqlTimings,
                HasTrivialTimings = profiler.HasTrivialTimings,
                HasAllTrivialTimings = profiler.HasAllTrivialTimings,
                TrivialDurationThresholdMilliseconds = (double)profiler.TrivialDurationThresholdMilliseconds,
                HasUserViewed = profiler.HasUserViewed
            };

            var result = Profilers.Save(profilerPoco, SafeMode.True);

            if (result.UpdatedExisting == false)
            {
                //Save Root Timing
                SaveTiming(profiler, profiler.Root);
            }

            // we may have a missing client timing - re save
            if (profiler.ClientTimings != null)
            {
                SaveClientTiming(profiler);
            }
            
        }

        private string Truncate(string s, int maxLength)
        {
            return s != null && s.Length > maxLength ? s.Substring(0, maxLength) : s;
        }

        private void SaveClientTiming(MiniProfiler profiler)
        {
            if (profiler.ClientTimings == null || profiler.ClientTimings.Timings == null || profiler.ClientTimings.Timings.Count == 0) return;

            foreach (var ct in profiler.ClientTimings.Timings)
            {
                ClientTimings.Save(new ClientTimingPoco
                {
                    Id = profiler.Id.ToString(),
                    Name = ct.Name,
                    Start = (double)ct.Start,
                    Duration = (double)ct.Duration
                });
            }
        }

        /// <summary>
        /// Saves parameter Timing to the timings table.
        /// </summary>
        private void SaveTiming(MiniProfiler profiler, Timing t)
        {
            var timingPoco = new TimingPoco
            {
                Id = t.Id.ToString(),
                MiniProfilerId = profiler.Id.ToString(),
                ParentTimingId =  t.IsRoot ? (string)null : t.ParentTiming.Id.ToString(),
                Name = Truncate(t.Name, 200),
                Depth = t.Depth,
                StartMilliseconds = (double)t.StartMilliseconds,
                DurationMilliseconds = (double)t.DurationMilliseconds,
                DurationWithoutChildrenMilliseconds = (double)t.DurationWithoutChildrenMilliseconds,
                SqlTimingsDurationMilliseconds = (double)t.SqlTimingsDurationMilliseconds,
                IsRoot = t.IsRoot,
                HasChildren = t.HasChildren,
                IsTrivial = t.IsTrivial,
                HasSqlTimings = t.HasSqlTimings,
                HasDuplicateSqlTimings = t.HasDuplicateSqlTimings,
                ExecutedReaders = t.ExecutedReaders,
                ExecutedScalars = t.ExecutedScalars,
                ExecutedNonQueries = t.ExecutedNonQueries
            };

            Timings.Insert(timingPoco);

            if (t.HasSqlTimings)
            {
                foreach (var st in t.SqlTimings)
                {
                    SaveSqlTiming(profiler, st);
                }
            }

            if (t.HasChildren)
            {
                foreach (var child in t.Children)
                {
                    SaveTiming(profiler, child);
                }
            }
        }

        /// <summary>
        /// Saves parameter Timing to the sqltimings collection.
        /// </summary>
        private void SaveSqlTiming(MiniProfiler profiler, SqlTiming s)
        {
            var sqlTimingPoco = new SqlTimingPoco
            {
                Id = s.Id.ToString(),
                MiniProfilerId = profiler.Id.ToString(),
                ParentTimingId = s.ParentTiming.Id.ToString(),
                ExecuteType = s.ExecuteType,
                StartMilliseconds = (double)s.StartMilliseconds,
                DurationMilliseconds = (double)s.DurationMilliseconds,
                FirstFetchDurationMilliseconds = (double)s.FirstFetchDurationMilliseconds,
                IsDuplicate = s.IsDuplicate,
                StackTraceSnippet = Truncate(s.StackTraceSnippet, 200),
                CommandString = s.CommandString
            };

            SqlTimings.Insert(sqlTimingPoco);

            if (s.Parameters != null && s.Parameters.Count > 0)
            {
                SaveSqlTimingParameters(profiler, s);
            }
        }

        /// <summary>
        /// Saves parameter Timing to the sqltimingparams collection.
        /// </summary>
        private void SaveSqlTimingParameters(MiniProfiler profiler, SqlTiming s)
        {
            foreach (var p in s.Parameters)
            {
                var sqltimingParamPoco = new SqlTimingParameterPoco
                {
                    MiniProfilerId = profiler.Id.ToString(),
                    ParentSqlTimingId = s.Id.ToString(),
                    Name = Truncate(p.Name, 150),
                    DbType = Truncate(p.DbType, 50),
                    Size = p.Size,
                    Value = p.Value
                };

                SqlTimingParams.Insert(sqltimingParamPoco);
            }
        }

        /// <summary>
        /// Loads the MiniProfiler identifed by 'id' from the database.
        /// </summary>
        public override MiniProfiler Load(Guid id)
        {
            var query = Query.EQ("_id", id.ToString());

            var profilerPoco = Profilers.FindOne(query);

            if (profilerPoco != null)
            {
                var profiler = new MiniProfiler
                {
                    Id = Guid.Parse(profilerPoco.Id),
                    Name = profilerPoco.Name,
                    Started = profilerPoco.Started,
                    MachineName = profilerPoco.MachineName,
                    User = profilerPoco.User,
                    Level = profilerPoco.Level,
                    HasUserViewed = profilerPoco.HasUserViewed
                };

                if (profiler != null)  //This is very similar to the Load logic in the SqlServerStorage.  
                    //Perhaps another abstraction layer(or moving some logic into the Base) which both Mongo and Sql inherit from would eliminate somewhat repetitive code.
                {
                    var timings = LoadTimings(profiler.Id);
                    var sqlTimings = LoadSqlTimings(profiler.Id);
                    var sqlParams = LoadSqlTimingParameters(profiler.Id);
                    var clientTimingList = LoadClientTimings(profiler.Id);
                    ClientTimings clientTimings = null;
                    if (clientTimingList.Count > 0)
                    {
                        clientTimings = new ClientTimings();
                        clientTimings.Timings = clientTimingList;
                    }

                    MapTimings(profiler, timings, sqlTimings, sqlParams, clientTimings);
                }

                profiler.OnDeserialized();

                return profiler;
            }

            return null;
        }

        private List<Timing> LoadTimings(Guid profilerId)
        {
            var timings = new List<Timing>();

            var query = Query.EQ("MiniProfilerId", profilerId.ToString());

            var timingPocos = Timings.Find(query).ToList();

            foreach (var poco in timingPocos)
            {
                timings.Add(new Timing
                {
                    Id = Guid.Parse(poco.Id),
                    ParentTimingId = poco.ParentTimingId == null ? (Guid?)null : Guid.Parse(poco.ParentTimingId),
                    Name = poco.Name,
                    StartMilliseconds = (decimal)poco.StartMilliseconds,
                    DurationMilliseconds = (decimal?)poco.DurationMilliseconds
                });
            }

            return timings;
        }

        private List<SqlTiming> LoadSqlTimings(Guid profilerId)
        {
            var sqlTimings = new List<SqlTiming>();

            var query = Query.EQ("MiniProfilerId", profilerId.ToString());

            var sqlTimingPocos = SqlTimings.Find(query).ToList();

            foreach (var poco in sqlTimingPocos)
            {
                sqlTimings.Add(new SqlTiming
                {
                    Id = Guid.Parse(poco.Id),
                    ParentTimingId = Guid.Parse(poco.ParentTimingId),
                    ExecuteType = poco.ExecuteType,
                    StartMilliseconds = (decimal)poco.StartMilliseconds,
                    DurationMilliseconds = (decimal)poco.DurationMilliseconds,
                    FirstFetchDurationMilliseconds = (decimal)poco.FirstFetchDurationMilliseconds,
                    IsDuplicate = poco.IsDuplicate,
                    StackTraceSnippet = poco.StackTraceSnippet,
                    CommandString = poco.CommandString
                });
            }

            return sqlTimings;
        }

        private List<SqlTimingParameter> LoadSqlTimingParameters(Guid profilerId)
        {
            var sqltimingparams = new List<SqlTimingParameter>();

            var query = Query.EQ("MiniProfilerId", profilerId.ToString());

            var sqltimingparamPocos = SqlTimingParams.Find(query).ToList();

            foreach (var poco in sqltimingparamPocos)
            {
                sqltimingparams.Add(new SqlTimingParameter
                {
                    ParentSqlTimingId = Guid.Parse(poco.ParentSqlTimingId),
                    Name = poco.Name,
                    DbType = poco.DbType,
                    Size = poco.Size,
                    Value = poco.Value
                });
            }

            return sqltimingparams;
        }

        private List<ClientTimings.ClientTiming> LoadClientTimings(Guid profilerId)
        {
            var clientTimings = new List<ClientTimings.ClientTiming>();

            var query = Query.EQ("_id", profilerId.ToString());

            var clientTimingPocos = ClientTimings.Find(query).ToList();

            foreach (var poco in clientTimingPocos)
            {
                clientTimings.Add(new ClientTimings.ClientTiming
                {
                    Name = poco.Name,
                    Start = (decimal)poco.Start,
                    Duration = (decimal)poco.Duration
                });
            }

            return clientTimings;
        }

        /// <summary>
        /// Sets the profiler as unviewed
        /// </summary>
        public override void SetUnviewed(string user, Guid id)
        {
            var profiler = Profilers.FindOne(Query.EQ("_id", id.ToString()));

            if (profiler != null)
            {
                profiler.HasUserViewed = false;
                Profilers.Save(profiler);
            }
        }

        /// <summary>
        /// Sets the profiler as view
        /// </summary>
        public override void SetViewed(string user, Guid id)
        {
            var profiler = Profilers.FindOne(Query.EQ("_id", id.ToString()));

            if (profiler != null)
            {
                profiler.HasUserViewed = true;
                Profilers.Save(profiler);
            }
        }

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <see cref="MiniProfiler.Settings.UserProvider"/>.</param>
        public override List<Guid> GetUnviewedIds(string user)
        {
            var query = Query.And(
                    Query<MiniProfilerPoco>.EQ(p => p.User, user),
                    Query<MiniProfilerPoco>.EQ(p => p.HasUserViewed, false));
            var guids = Profilers.FindAs<MiniProfilerPoco>(query).Select(p => Guid.Parse(p.Id)).ToList();
            return guids;
        }

        public override IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Decending)
        {
            IMongoQuery query = null;

            if(start != null){
                query = Query.And(Query.GT("Started", (DateTime)start));
            }
            if(finish != null){
                query = Query.And(Query.LT("Started", (DateTime)finish));
            }

            var profilers = Profilers.Find(query).Take(maxResults);

            if (orderBy == ListResultsOrder.Decending)
            {
                profilers = profilers.OrderByDescending(p => p.Started);
            }
            else
            {
                profilers = profilers.OrderBy(p => p.Started);
            }

            return profilers.Select(p => Guid.Parse(p.Id));
        }

        #region Poco Classes

        //In order to use Guids as the Id in MongoDb we have to use a strongly typed class and the [BsonId] attribute on the Id.  Otherwise Mongo defaults to ObjectIds which cannot be cast to Guids.

        class MiniProfilerPoco
        {
            [BsonId]
            public string Id { get; set; }
            public string Name { get; set; }
            public DateTime Started { get; set; }
            public string MachineName { get; set; }
            public string User { get; set; }
            public ProfileLevel Level { get; set; }
            public Guid RootTimingId { get; set; }
            public double DurationMilliseconds { get; set; }
            public double DurationMillisecondsInSql { get; set; }
            public bool HasSqlTimings { get; set; }
            public bool HasDuplicateSqlTimings { get; set; }
            public bool HasTrivialTimings { get; set; }
            public bool HasAllTrivialTimings { get; set; }
            public double TrivialDurationThresholdMilliseconds { get; set; }
            public bool HasUserViewed { get; set; }
        }

        class TimingPoco
        {
            [BsonId]
            public string Id { get; set; }
            public string MiniProfilerId { get; set; }
            public string ParentTimingId { get; set; }
            public string Name { get; set; }
            public short Depth { get; set; }
            public double StartMilliseconds { get; set; }
            public double DurationMilliseconds { get; set; }
            public double DurationWithoutChildrenMilliseconds { get; set; }
            public double SqlTimingsDurationMilliseconds { get; set; }
            public bool IsRoot { get; set; }
            public bool HasChildren { get; set; }
            public bool IsTrivial { get; set; }
            public bool HasSqlTimings { get; set; }
            public bool HasDuplicateSqlTimings { get; set; }
            public int ExecutedReaders { get; set; }
            public int ExecutedScalars { get; set; }
            public int ExecutedNonQueries { get; set; }
        }

        class SqlTimingPoco
        {
            [BsonId]
            public string Id { get; set; }
            public string MiniProfilerId { get; set; }
            public string ParentTimingId { get; set; }
            public ExecuteType ExecuteType { get; set; }
            public double StartMilliseconds { get; set; }
            public double DurationMilliseconds { get; set; }
            public double FirstFetchDurationMilliseconds { get; set; }
            public bool IsDuplicate { get; set; }
            public string StackTraceSnippet { get; set; }
            public string CommandString { get; set; }
        }

        class SqlTimingParameterPoco
        {
            [BsonId]
            public Guid Id {get;set;}
            public string MiniProfilerId { get; set; }
            public string ParentSqlTimingId { get; set; }
            public string Name { get; set; }
            public string DbType { get; set; }
            public int Size { get; set; }
            public string Value { get; set; }
        }

        class ClientTimingPoco
        {
            [BsonId]
            public string Id { get; set; }
            public string Name { get; set; }
            public double Start { get; set; }
            public double Duration { get; set; }
        }

        #endregion

    }
}
