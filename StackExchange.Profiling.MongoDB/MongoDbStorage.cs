using System;
using System.Collections.Generic;
using System.Linq;

using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using StackExchange.Profiling.Data;
using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling.MongoDB
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a MongoDb database.
    /// </summary>
    public class MongoDbStorage : DatabaseStorageBase
    {
        #region Collections

        private MongoDatabase _db;
        private MongoDatabase Db
        {
            get
            {
                if (_db == null)
                {
                    var client = new MongoClient(ConnectionString);
                    var server = client.GetServer();
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
            : base(connectionString)
        {
        }

        /// <summary>
        /// Stores <param name="profiler"/> to MongoDB under its <see cref="MiniProfiler.Id"/>; 
        /// stores all child Timings and SqlTimings to their respective tables.
        /// </summary>
        public override void Save(MiniProfiler profiler)
        {
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
            }

            return null;
        }

        /// <summary>
        /// Sets the profiler as unviewed
        /// </summary>
        public override void SetUnviewed(string user, Guid id)
        {
            Profilers.Update(Query.EQ("_id", id.ToString()),
                Update<MiniProfilerPoco>.Set(poco => poco.HasUserViewed, false));
        }

        /// <summary>
        /// Sets the profiler as view
        /// </summary>
        public override void SetViewed(string user, Guid id)
        {
            Profilers.Update(Query.EQ("_id", id.ToString()),
                Update<MiniProfilerPoco>.Set(poco => poco.HasUserViewed, true));
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
            var guids = Profilers.Find(query).Select(p => Guid.Parse(p.Id)).ToList();
            return guids;
        }

        public override IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            IMongoQuery query = null;

            if (start != null)
            {
                query = Query.And(Query.GT("Started", (DateTime)start));
            }
            if (finish != null)
            {
                query = Query.And(Query.LT("Started", (DateTime)finish));
            }

            var profilers = Profilers.Find(query).Take(maxResults);

            profilers = orderBy == ListResultsOrder.Descending
                ? profilers.OrderByDescending(p => p.Started)
                : profilers.OrderBy(p => p.Started);

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
            public Guid RootTimingId { get; set; }
            public double DurationMilliseconds { get; set; }
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
            public SqlExecuteType ExecuteType { get; set; }
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
            public Guid Id { get; set; }
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
