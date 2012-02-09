using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.Common;

using StackExchange.Profiling.Helpers;
using StackExchange.Profiling.Helpers.Dapper;
using System.Runtime.Serialization;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a MSSQL database.
    /// </summary>
    public class SqlServerStorage : DatabaseStorageBase
    {
        /// <summary>
        /// Returns a new <see cref="SqlServerStorage"/>.
        /// </summary>
        public SqlServerStorage(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        /// A full install of Sql Server can return multiple result sets in one query, allowing the use of <see cref="SqlMapper.QueryMultiple"/>.
        /// However, Sql Server CE and Sqlite cannot do this, so inheritors for those providers can return false here.
        /// </summary>
        public virtual bool EnableBatchSelects { get { return true; } }

        /// <summary>
        /// Stores <param name="profiler"/> to dbo.MiniProfilers under its <see cref="MiniProfiler.Id"/>; 
        /// stores all child Timings and SqlTimings to their respective tables.
        /// </summary>
        public override void Save(MiniProfiler profiler)
        {
            const string sql =
@"insert into MiniProfilers
            (Id,
             Name,
             Started,
             MachineName,
             [User],
             Level,
             RootTimingId,
             DurationMilliseconds,
             DurationMillisecondsInSql,
             HasSqlTimings,
             HasDuplicateSqlTimings,
             HasTrivialTimings,
             HasAllTrivialTimings,
             TrivialDurationThresholdMilliseconds,
             HasUserViewed)
select       @Id,
             @Name,
             @Started,
             @MachineName,
             @User,
             @Level,
             @RootTimingId,
             @DurationMilliseconds,
             @DurationMillisecondsInSql,
             @HasSqlTimings,
             @HasDuplicateSqlTimings,
             @HasTrivialTimings,
             @HasAllTrivialTimings,
             @TrivialDurationThresholdMilliseconds,
             @HasUserViewed
where not exists (select 1 from MiniProfilers where Id = @Id)"; // this syntax works on both mssql and sqlite

            using (var conn = GetOpenConnection())
            {
                var insertCount = conn.Execute(sql, new
                {
                    Id = profiler.Id,
                    Name = profiler.Name.Truncate(200),
                    Started = profiler.Started,
                    MachineName = profiler.MachineName.Truncate(100),
                    User = profiler.User.Truncate(100),
                    Level = profiler.Level,
                    RootTimingId = profiler.Root.Id,
                    DurationMilliseconds = profiler.DurationMilliseconds,
                    DurationMillisecondsInSql = profiler.DurationMillisecondsInSql,
                    HasSqlTimings = profiler.HasSqlTimings,
                    HasDuplicateSqlTimings = profiler.HasDuplicateSqlTimings,
                    HasTrivialTimings = profiler.HasTrivialTimings,
                    HasAllTrivialTimings = profiler.HasAllTrivialTimings,
                    TrivialDurationThresholdMilliseconds = profiler.TrivialDurationThresholdMilliseconds,
                    HasUserViewed = profiler.HasUserViewed
                });

                if (insertCount > 0)
                {
                    SaveTiming(conn, profiler, profiler.Root);
                }

                // we may have a missing client timing - re save
                if (profiler.ClientTimings != null)
                {
                    conn.Execute("delete from MiniProfilerClientTimings where MiniProfilerId = @Id", new { profiler.Id });
                    SaveClientTiming(conn, profiler);
                }
            }
        }

        static string insertClientTimingSql = null;
        protected virtual void SaveClientTiming(DbConnection conn, MiniProfiler profiler)
        {
            if (profiler.ClientTimings == null) return;

            if (insertClientTimingSql == null)
            {
                string[] cols = typeof(ClientTimings).GetProperties().Where(p =>
                {
                    var attr = p.GetCustomAttributes(typeof(DataMemberAttribute), false);
                    return attr != null && attr.Length == 1;
                })
                .Select(p => p.Name)
                .ToArray();

                insertClientTimingSql = "insert into MiniProfilerClientTimings(MiniProfilerId," + string.Join(",", cols) + ") values (@MiniProfilerId, " +
                    string.Join(",", cols.Select(c => "@" + c)) + ")";
            }

            var dp = new DynamicParameters(profiler.ClientTimings);
            dp.Add("@MiniProfilerId", profiler.Id);

            conn.Execute(insertClientTimingSql, dp);
        }

        /// <summary>
        /// Saves parameter Timing to the dbo.MiniProfilerTimings table.
        /// </summary>
        protected virtual void SaveTiming(DbConnection conn, MiniProfiler profiler, Timing t)
        {
            const string sql =
@"insert into MiniProfilerTimings
            (Id,
             MiniProfilerId,
             ParentTimingId,
             Name,
             Depth,
             StartMilliseconds,
             DurationMilliseconds,
             DurationWithoutChildrenMilliseconds,
             SqlTimingsDurationMilliseconds,
             IsRoot,
             HasChildren,
             IsTrivial,
             HasSqlTimings,
             HasDuplicateSqlTimings,
             ExecutedReaders,
             ExecutedScalars,
             ExecutedNonQueries)
values      (@Id,
             @MiniProfilerId,
             @ParentTimingId,
             @Name,
             @Depth,
             @StartMilliseconds,
             @DurationMilliseconds,
             @DurationWithoutChildrenMilliseconds,
             @SqlTimingsDurationMilliseconds,
             @IsRoot,
             @HasChildren,
             @IsTrivial,
             @HasSqlTimings,
             @HasDuplicateSqlTimings,
             @ExecutedReaders,
             @ExecutedScalars,
             @ExecutedNonQueries)";

            conn.Execute(sql, new
            {
                Id = t.Id,
                MiniProfilerId = profiler.Id,
                ParentTimingId = t.IsRoot ? (Guid?)null : t.ParentTiming.Id,
                Name = t.Name.Truncate(200),
                Depth = t.Depth,
                StartMilliseconds = t.StartMilliseconds,
                DurationMilliseconds = t.DurationMilliseconds,
                DurationWithoutChildrenMilliseconds = t.DurationWithoutChildrenMilliseconds,
                SqlTimingsDurationMilliseconds = t.SqlTimingsDurationMilliseconds,
                IsRoot = t.IsRoot,
                HasChildren = t.HasChildren,
                IsTrivial = t.IsTrivial,
                HasSqlTimings = t.HasSqlTimings,
                HasDuplicateSqlTimings = t.HasDuplicateSqlTimings,
                ExecutedReaders = t.ExecutedReaders,
                ExecutedScalars = t.ExecutedScalars,
                ExecutedNonQueries = t.ExecutedNonQueries
            });

            if (t.HasSqlTimings)
            {
                foreach (var st in t.SqlTimings)
                {
                    SaveSqlTiming(conn, profiler, st);
                }
            }

            if (t.HasChildren)
            {
                foreach (var child in t.Children)
                {
                    SaveTiming(conn, profiler, child);
                }
            }
        }

        /// <summary>
        /// Saves parameter SqlTiming to the dbo.MiniProfilerSqlTimings table.
        /// </summary>
        protected virtual void SaveSqlTiming(DbConnection conn, MiniProfiler profiler, SqlTiming st)
        {
            const string sql =
@"insert into MiniProfilerSqlTimings
            (Id,
             MiniProfilerId,
             ParentTimingId,
             ExecuteType,
             StartMilliseconds,
             DurationMilliseconds,
             FirstFetchDurationMilliseconds,
             IsDuplicate,
             StackTraceSnippet,
             CommandString)
values      (@Id,
             @MiniProfilerId,
             @ParentTimingId,
             @ExecuteType,
             @StartMilliseconds,
             @DurationMilliseconds,
             @FirstFetchDurationMilliseconds,
             @IsDuplicate,
             @StackTraceSnippet,
             @CommandString)";

            conn.Execute(sql, new
            {
                Id = st.Id,
                MiniProfilerId = profiler.Id,
                ParentTimingId = st.ParentTiming.Id,
                ExecuteType = st.ExecuteType,
                StartMilliseconds = st.StartMilliseconds,
                DurationMilliseconds = st.DurationMilliseconds,
                FirstFetchDurationMilliseconds = st.FirstFetchDurationMilliseconds,
                IsDuplicate = st.IsDuplicate,
                StackTraceSnippet = st.StackTraceSnippet.Truncate(200),
                CommandString = st.CommandString
            });

            if (st.Parameters != null && st.Parameters.Count > 0)
            {
                SaveSqlTimingParameters(conn, profiler, st);
            }
        }

        /// <summary>
        /// Saves any SqlTimingParameters used in the profiled SqlTiming to the dbo.MiniProfilerSqlTimingParameters table.
        /// </summary>
        protected virtual void SaveSqlTimingParameters(DbConnection conn, MiniProfiler profiler, SqlTiming st)
        {
            const string sql =
@"insert into MiniProfilerSqlTimingParameters
            (MiniProfilerId,
             ParentSqlTimingId,
             Name,
             DbType,
             Size,
             Value)
values      (@MiniProfilerId,
             @ParentSqlTimingId,
             @Name,
             @DbType,
             @Size,
             @Value)";

            foreach (var p in st.Parameters)
            {
                conn.Execute(sql, new
                {
                    MiniProfilerId = profiler.Id,
                    ParentSqlTimingId = st.Id,
                    Name = p.Name.Truncate(130),
                    DbType = p.DbType.Truncate(50),
                    Size = p.Size,
                    Value = p.Value
                });
            }
        }

        private static readonly Dictionary<Type, string> LoadSqlStatements = new Dictionary<Type, string>
        {
            { typeof(MiniProfiler), "select * from MiniProfilers where Id = @id" },
            { typeof(Timing), "select * from MiniProfilerTimings where MiniProfilerId = @id order by RowId" },
            { typeof(SqlTiming), "select * from MiniProfilerSqlTimings where MiniProfilerId = @id order by RowId" },
            { typeof(SqlTimingParameter), "select * from MiniProfilerSqlTimingParameters where MiniProfilerId = @id" },
            { typeof(ClientTimings), "select * from MiniProfilerClientTimings where MiniProfilerId = @id"}
        };

        private static readonly string LoadSqlBatch = string.Join("\n", LoadSqlStatements.Select(pair => pair.Value));

        /// <summary>
        /// Loads the MiniProfiler identifed by 'id' from the database.
        /// </summary>
        public override MiniProfiler Load(Guid id)
        {
            using (var conn = GetOpenConnection())
            {
                var idParameter = new { id };
                var result = EnableBatchSelects ? LoadInBatch(conn, idParameter) : LoadIndividually(conn, idParameter);

                if (result != null)
                {
                    // HACK: stored dates are utc, but are pulled out as local time
                    result.Started = new DateTime(result.Started.Ticks, DateTimeKind.Utc);
                }

                return result;
            }
        }

        /// <summary>
        /// sets the session to unviewed 
        /// </summary>
        public override void SetUnviewed(string user, Guid id)
        {
            using (var conn = GetOpenConnection())
            {
                conn.Execute("update MiniProfilers set HasUserViewed = 0 where Id = @id", new { id });
            }
        }

        /// <summary>
        /// sets the session to viewed
        /// </summary>
        public override void SetViewed(string user, Guid id)
        {
            using (var conn = GetOpenConnection())
            {
                conn.Execute("update MiniProfilers set HasUserViewed = 1 where Id = @id", new { id });
            }
        }

        private MiniProfiler LoadInBatch(DbConnection conn, object idParameter)
        {
            MiniProfiler result;

            using (var multi = conn.QueryMultiple(LoadSqlBatch, idParameter))
            {
                result = multi.Read<MiniProfiler>().SingleOrDefault();

                if (result != null)
                {
                    var timings = multi.Read<Timing>().ToList();
                    var sqlTimings = multi.Read<SqlTiming>().ToList();
                    var sqlParameters = multi.Read<SqlTimingParameter>().ToList();
                    var clientTimings = multi.Read<ClientTimings>().ToList();
                    MapTimings(result, timings, sqlTimings, sqlParameters, clientTimings);
                }
            }

            return result;
        }

        private MiniProfiler LoadIndividually(DbConnection conn, object idParameter)
        {
            var result = LoadFor<MiniProfiler>(conn, idParameter).SingleOrDefault();

            if (result != null)
            {
                var timings = LoadFor<Timing>(conn, idParameter);
                var sqlTimings = LoadFor<SqlTiming>(conn, idParameter);
                var sqlParameters = LoadFor<SqlTimingParameter>(conn, idParameter);
                var clientTimings = LoadFor<ClientTimings>(conn, idParameter);
                MapTimings(result, timings, sqlTimings, sqlParameters,clientTimings);
            }

            return result;
        }

        private List<T> LoadFor<T>(DbConnection conn, object idParameter)
        {
            return conn.Query<T>(LoadSqlStatements[typeof(T)], idParameter).ToList();
        }


        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <see cref="MiniProfiler.Settings.UserProvider"/>.</param>
        public override List<Guid> GetUnviewedIds(string user)
        {
            const string sql =
@"select Id
from   MiniProfilers
where  [User] = @user
and    HasUserViewed = 0
order  by Started";

            using (var conn = GetOpenConnection())
            {
                return conn.Query<Guid>(sql, new { user }).ToList();
            }
        }


        public override IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Decending)
        {
            var builder = new SqlBuilder();
            var t = builder.AddTemplate("select top " + maxResults + " Id from MiniProfilers /**where**/ /**orderby**/");

            if (start != null) { builder.Where("Started > @start", new { start }); };
            if (finish != null) { builder.Where("Started < @finish", new { finish }); };

            if (orderBy == ListResultsOrder.Decending)
            {
                builder.OrderBy("Started desc");
            }
            else 
            {
                builder.OrderBy("Started asc");
            }

            using (var conn = GetOpenConnection())
            {
                return conn.Query<Guid>(t.RawSql, t.Parameters).ToList();
            }
        }

        /// <summary>
        /// Returns a connection to Sql Server.
        /// </summary>
        protected override DbConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        /// <summary>
        /// Creates needed tables. Run this once on your database.
        /// </summary>
        /// <remarks>
        /// Works in sql server and sqlite (with documented removals).
        /// TODO: add indexes
        /// </remarks>
        public const string TableCreationScript =
@"create table MiniProfilers
  (
     Id                                   uniqueidentifier not null primary key,
     Name                                 nvarchar(200) not null,
     Started                              datetime not null,
     MachineName                          nvarchar(100) null,
     [User]                               nvarchar(100) null,
     Level                                tinyint null,
     RootTimingId                         uniqueidentifier null,
     DurationMilliseconds                 decimal(7, 1) not null,
     DurationMillisecondsInSql            decimal(7, 1) null,
     HasSqlTimings                        bit not null,
     HasDuplicateSqlTimings               bit not null,
     HasTrivialTimings                    bit not null,
     HasAllTrivialTimings                 bit not null,
     TrivialDurationThresholdMilliseconds decimal(5, 1) null,
     HasUserViewed                        bit not null
  );

create table MiniProfilerTimings
  (
     RowId                               integer primary key identity, -- sqlite: replace identity with autoincrement
     Id                                  uniqueidentifier not null,
     MiniProfilerId                      uniqueidentifier not null,
     ParentTimingId                      uniqueidentifier null,
     Name                                nvarchar(200) not null,
     Depth                               smallint not null,
     StartMilliseconds                   decimal(7, 1) not null,
     DurationMilliseconds                decimal(7, 1) not null,
     DurationWithoutChildrenMilliseconds decimal(7, 1) not null,
     SqlTimingsDurationMilliseconds      decimal(7, 1) null,
     IsRoot                              bit not null,
     HasChildren                         bit not null,
     IsTrivial                           bit not null,
     HasSqlTimings                       bit not null,
     HasDuplicateSqlTimings              bit not null,
     ExecutedReaders                     smallint not null,
     ExecutedScalars                     smallint not null,
     ExecutedNonQueries                  smallint not null
  );

create table MiniProfilerSqlTimings
  (
     RowId                          integer primary key identity, -- sqlite: replace identity with autoincrement
     Id                             uniqueidentifier not null,
     MiniProfilerId                 uniqueidentifier not null,
     ParentTimingId                 uniqueidentifier not null,
     ExecuteType                    tinyint not null,
     StartMilliseconds              decimal(7, 1) not null,
     DurationMilliseconds           decimal(7, 1) not null,
     FirstFetchDurationMilliseconds decimal(7, 1) null,
     IsDuplicate                    bit not null,
     StackTraceSnippet              nvarchar(200) not null,
     CommandString                  nvarchar(max) not null -- sqlite: remove (max) -- sql server ce: replace with ntext
  );

create table MiniProfilerSqlTimingParameters
  (
     MiniProfilerId    uniqueidentifier not null,
     ParentSqlTimingId uniqueidentifier not null,
     Name              nvarchar(130) not null,
     DbType            nvarchar(50) null,
     Size              int null,
     Value             nvarchar(max) null -- sqlite: remove (max) -- sql server ce: replace with ntext
  );

create table MiniProfilerClientTimings
(
  MiniProfilerId    uniqueidentifier not null,
  RedirectCount int,
  NavigationStart decimal(7,1),
  UnloadEventStart decimal(7,1),
  UnloadEventEnd decimal(7,1),
  RedirectStart decimal(7,1),
  RedirectEnd decimal(7,1),
  FetchStart decimal(7,1),
  DomainLookupStart decimal(7,1),
  DomainLookupEnd decimal(7,1),
  ConnectStart decimal(7,1),
  ConnectEnd decimal(7,1),
  SecureConnectionStart decimal(7,1),
  RequestStart decimal(7,1),
  ResponseStart decimal(7,1),
  ResponseEnd decimal(7,1),
  DomLoading decimal(7,1),
  DomInteractive decimal(7,1),
  DomContentLoadedEventStart decimal(7,1),
  DomContentLoadedEventEnd decimal(7,1),
  DomComplete decimal(7,1),
  LoadEventStart decimal(7,1),
  LoadEventEnd decimal(7,1)     
)

";

    }
}
