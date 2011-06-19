using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

using MvcMiniProfiler.Helpers;

namespace MvcMiniProfiler.Storage
{
    /// <summary>
    /// Understands how to save MiniProfiler results to a MSSQL database, allowing more permanent storage and
    /// querying of slow results.
    /// </summary>
    public abstract class DatabaseStorageBase : IStorage
    {
        protected string ConnectionString { get; set; }

        /// <summary>
        /// Returns a new SqlServerDatabaseStorage object that will insert into the database identified by connectionString.
        /// </summary>
        public DatabaseStorageBase(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public virtual void SaveMiniProfiler(Guid id, MiniProfiler profiler)
        {
            const string sql =
@"insert into MiniProfiler
            (Id,
             Name,
             Started,
             MachineName,
             Level,
             RootTimingId,
             DurationMilliseconds,
             DurationMillisecondsInSql,
             HasSqlTimings,
             HasDuplicateSqlTimings,
             HasTrivialTimings,
             HasAllTrivialTimings,
             TrivialDurationThresholdMilliseconds)
select       @Id,
             @Name,
             @Started,
             @MachineName,
             @Level,
             @RootTimingId,
             @DurationMilliseconds,
             @DurationMillisecondsInSql,
             @HasSqlTimings,
             @HasDuplicateSqlTimings,
             @HasTrivialTimings,
             @HasAllTrivialTimings,
             @TrivialDurationThresholdMilliseconds
where not exists (select 1 from MiniProfiler where Id = @Id)"; // this syntax works on both mssql and sqlite

            using (var conn = GetOpenConnection())
            {
                var insertCount = conn.Execute(sql, new
                {
                    Id = profiler.Id,
                    Name = profiler.Name,
                    Started = profiler.Started,
                    MachineName = profiler.MachineName,
                    Level = profiler.Level,
                    RootTimingId = profiler.Root.Id,
                    DurationMilliseconds = profiler.DurationMilliseconds,
                    DurationMillisecondsInSql = profiler.DurationMillisecondsInSql,
                    HasSqlTimings = profiler.HasSqlTimings,
                    HasDuplicateSqlTimings = profiler.HasDuplicateSqlTimings,
                    HasTrivialTimings = profiler.HasTrivialTimings,
                    HasAllTrivialTimings = profiler.HasAllTrivialTimings,
                    TrivialDurationThresholdMilliseconds = profiler.TrivialDurationThresholdMilliseconds
                });

                if (insertCount > 0)
                    SaveTiming(conn, profiler.Root);
            }
        }

        protected virtual void SaveTiming(DbConnection conn, Timing t)
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
                MiniProfilerId = t.Profiler.Id,
                ParentTimingId = t.IsRoot ? (Guid?)null : t.ParentTiming.Id,
                Name = t.Name,
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
                    SaveSqlTiming(conn, st, t);
                }
            }

            if (t.HasChildren)
            {
                foreach (var child in t.Children)
                {
                    SaveTiming(conn, child);
                }
            }
        }

        protected virtual void SaveSqlTiming(DbConnection conn, SqlTiming st, Timing parent)
        {
            const string sql =
@"insert into MiniProfilerSqlTimings
            (MiniProfilerId,
             ParentTimingId,
             ExecuteType,
             StartMilliseconds,
             DurationMilliseconds,
             FirstFetchDurationMilliseconds,
             IsDuplicate,
             StackTraceSnippet,
             CommandString)
values      (@MiniProfilerId,
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
                MiniProfilerId = parent.Profiler.Id,
                ParentTimingId = parent.Id,
                ExecuteType = st.ExecuteType,
                StartMilliseconds = st.StartMilliseconds,
                DurationMilliseconds = st.DurationMilliseconds,
                FirstFetchDurationMilliseconds = st.FirstFetchDurationMilliseconds,
                IsDuplicate = st.IsDuplicate,
                StackTraceSnippet = st.StackTraceSnippet,
                CommandString = st.CommandString
            });
        }

        public virtual MiniProfiler LoadMiniProfiler(Guid id)
        {
            const string sql =
@"select * from MiniProfiler where Id = @id
select * from MiniProfilerTimings where  MiniProfilerId = @id order by RowId
select * from MiniProfilerSqlTimings where MiniProfilerId = @id order by RowId";

            MiniProfiler result = null;

            using (var conn = GetOpenConnection())
            using (var multi = conn.QueryMultiple(sql, new { id = id }))
            {
                result = multi.Read<MiniProfiler>().SingleOrDefault();

                if (result != null)
                {
                    // HACK: stored dates are utc, but are pulled out as local time - maybe use datetimeoffset data type?
                    result.Started = new DateTime(result.Started.Ticks, DateTimeKind.Utc);

                    var timings = multi.Read<Timing>().ToList();
                    var sqlTimings = multi.Read<SqlTiming>().ToList();
                    MapTimings(result, timings, sqlTimings);
                }
            }

            return result;
        }

        /// <summary>
        /// Giving freshly selected 'timings' and 'sqlTimings', this method puts them in the correct
        /// hierarchy under the 'result' MiniProfiler.
        /// </summary>
        protected void MapTimings(MiniProfiler result, List<Timing> timings, List<SqlTiming> sqlTimings)
        {
            var stack = new Stack<Timing>();
            stack.Push(timings.First());

            for (int i = 1; i < timings.Count; i++)
            {
                var cur = timings[i];
                foreach (var sqlTiming in sqlTimings)
                {
                    if (sqlTiming.ParentTimingId == cur.Id)
                        cur.AddSqlTiming(sqlTiming);
                }

                Timing head;

                while ((head = stack.Peek()).Id != cur.ParentTimingId)
                {
                    stack.Pop();
                }

                head.AddChild(cur);
                stack.Push(cur);
            }

            // TODO: .Root does all the above work again, but it's used after [DataContract] deserialization; refactor it out somehow
            result.Root = timings.First();
        }

        protected abstract DbConnection GetConnection();

        protected DbConnection GetOpenConnection()
        {
            var result = GetConnection();
            if (result.State != System.Data.ConnectionState.Open)
                result.Open();
            return result;
        }

        /// <summary>
        /// Creates needed tables. Run this once on your database.
        /// </summary>
        /// <remarks>
        /// Works in sql server and sqlite (with documented removals).
        /// TODO: add indexes
        /// </remarks>
        public const string TableCreationScript =
@"create table MiniProfiler
  (
     Id                                   uniqueidentifier not null primary key,
     Name                                 nvarchar(200) not null,
     Started                              datetime not null,
     MachineName                          nvarchar(100) null,
     Level                                tinyint null,
     RootTimingId                         uniqueidentifier null,
     DurationMilliseconds                 decimal(7, 1) not null,
     DurationMillisecondsInSql            decimal(7, 1) null,
     HasSqlTimings                        bit not null,
     HasDuplicateSqlTimings               bit not null,
     HasTrivialTimings                    bit not null,
     HasAllTrivialTimings                 bit not null,
     TrivialDurationThresholdMilliseconds decimal(5, 1) null
  )

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
  )

create table MiniProfilerSqlTimings
  (
     RowId                          integer primary key identity, -- sqlite: replace identity with autoincrement
     MiniProfilerId                 uniqueidentifier not null,
     ParentTimingId                 uniqueidentifier not null,
     ExecuteType                    tinyint not null,
     StartMilliseconds              decimal(7, 1) not null,
     DurationMilliseconds           decimal(7, 1) not null,
     FirstFetchDurationMilliseconds decimal(7, 1) null,
     IsDuplicate                    bit not null,
     StackTraceSnippet              nvarchar(200) not null,
     CommandString                  nvarchar(max) not null -- sqlite: remove (max)
  )";

    }
}
