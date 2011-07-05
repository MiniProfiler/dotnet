using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.Common;

using MvcMiniProfiler.Helpers;

namespace MvcMiniProfiler.Storage
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
        /// Stores <param name="profiler"/> to dbo.MiniProfilers under its <see cref="MiniProfiler.Id"/>; 
        /// stores all child Timings and SqlTimings to their respective tables.
        /// </summary>
        public override void SaveMiniProfiler(MiniProfiler profiler)
        {
            const string sql =
@"insert into MiniProfilers
            (Id,
             Name,
             Started,
             MachineName,
             User,
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
                    Name = profiler.Name,
                    Started = profiler.Started,
                    MachineName = profiler.MachineName,
                    User = profiler.User,
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
                    SaveTiming(conn, profiler, profiler.Root);
            }
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
                StackTraceSnippet = st.StackTraceSnippet,
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
                    Name = p.Name,
                    DbType = p.DbType,
                    Size = p.Size,
                    Value = p.Value
                });
            }
        }

        /// <summary>
        /// Loads the MiniProfiler identifed by 'id' from the database.
        /// </summary>
        public override MiniProfiler LoadMiniProfiler(Guid id)
        {
            const string sql =
@"select * from MiniProfilers where Id = @id
select * from MiniProfilerTimings where  MiniProfilerId = @id order by RowId
select * from MiniProfilerSqlTimings where MiniProfilerId = @id order by RowId
select * from MiniProfilerSqlTimingParameters where MiniProfilerId = @id";

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
                    var sqlParameters = multi.Read<SqlTimingParameter>().ToList();
                    MapTimings(result, timings, sqlTimings, sqlParameters);
                }
            }

            return result;
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
     User                                 nvarchar(100) null,
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
     Id                             uniqueidentifier not null,
     MiniProfilerId                 uniqueidentifier not null,
     ParentTimingId                 uniqueidentifier not null,
     ExecuteType                    tinyint not null,
     StartMilliseconds              decimal(7, 1) not null,
     DurationMilliseconds           decimal(7, 1) not null,
     FirstFetchDurationMilliseconds decimal(7, 1) null,
     IsDuplicate                    bit not null,
     StackTraceSnippet              nvarchar(200) not null,
     CommandString                  nvarchar(max) not null -- sqlite: remove (max)
  )

create table MiniProfilerSqlTimingParameters
  (
     MiniProfilerId    uniqueidentifier not null,
     ParentSqlTimingId uniqueidentifier not null,
     Name              varchar(130) not null,
     DbType            varchar(50) null,
     Size              int null,
     Value             nvarchar(max) null -- sqlite: remove (max)
  )";
    }
}
