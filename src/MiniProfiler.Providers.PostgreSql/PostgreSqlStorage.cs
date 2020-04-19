using Dapper;
using Npgsql;
using StackExchange.Profiling.Internal;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a PostgreSQL Server database.
    /// </summary>
    public class PostgreSqlStorage : DatabaseStorageBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlStorage"/> class with the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to use.</param>
        public PostgreSqlStorage(string connectionString) : base(connectionString) { /* base call */ }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlStorage"/> class with the specified connection string
        /// and the given table names to use.
        /// </summary>
        /// <param name="connectionString">The connection string to use.</param>
        /// <param name="profilersTable">The table name to use for MiniProfilers.</param>
        /// <param name="timingsTable">The table name to use for MiniProfiler Timings.</param>
        /// <param name="clientTimingsTable">The table name to use for MiniProfiler Client Timings.</param>
        public PostgreSqlStorage(string connectionString, string profilersTable, string timingsTable, string clientTimingsTable)
            : base(connectionString, profilersTable, timingsTable, clientTimingsTable) { }

        private string _saveSql, _saveTimingsSql, _saveClientTimingsSql;

        private string SaveSql => _saveSql ??= $@"
INSERT INTO {MiniProfilersTable}
            (Id, RootTimingId, Name, Started, DurationMilliseconds, ""User"", HasUserViewed, MachineName, CustomLinksJson, ClientTimingsRedirectCount)
SELECT      @Id, @RootTimingId, @Name, @Started, @DurationMilliseconds, @User, @HasUserViewed, @MachineName, @CustomLinksJson, @ClientTimingsRedirectCount
WHERE NOT EXISTS (SELECT 1 FROM {MiniProfilersTable} WHERE Id = @Id)";

        private string SaveTimingsSql => _saveTimingsSql ??= $@"
INSERT INTO {MiniProfilerTimingsTable}
            (Id, MiniProfilerId, ParentTimingId, Name, DurationMilliseconds, StartMilliseconds, IsRoot, Depth, CustomTimingsJson)
SELECT      @Id, @MiniProfilerId, @ParentTimingId, @Name, @DurationMilliseconds, @StartMilliseconds, @IsRoot, @Depth, @CustomTimingsJson
WHERE NOT EXISTS (SELECT 1 FROM {MiniProfilerTimingsTable} WHERE Id = @Id)";

        private string SaveClientTimingsSql => _saveClientTimingsSql ??= $@"
INSERT INTO {MiniProfilerClientTimingsTable}
            (Id, MiniProfilerId, Name, Start, Duration)
SELECT      @Id, @MiniProfilerId, @Name, @Start, @Duration
WHERE NOT EXISTS (SELECT 1 FROM {MiniProfilerClientTimingsTable} WHERE Id = @Id)";

        /// <summary>
        /// Stores to <c>dbo.MiniProfilers</c> under its <see cref="MiniProfiler.Id"/>;
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public override void Save(MiniProfiler profiler)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                conn.Execute(SaveSql, new
                {
                    profiler.Id,
                    profiler.Started,
                    Name = profiler.Name.Truncate(200),
                    User = profiler.User.Truncate(100),
                    RootTimingId = profiler.Root?.Id,
                    profiler.DurationMilliseconds,
                    profiler.HasUserViewed,
                    MachineName = profiler.MachineName.Truncate(100),
                    profiler.CustomLinksJson,
                    ClientTimingsRedirectCount = profiler.ClientTimings?.RedirectCount
                });

                var timings = new List<Timing>();
                if (profiler.Root != null)
                {
                    profiler.Root.MiniProfilerId = profiler.Id;
                    FlattenTimings(profiler.Root, timings);
                }

                conn.Execute(SaveTimingsSql, timings.Select(timing => new
                {
                    timing.Id,
                    timing.MiniProfilerId,
                    timing.ParentTimingId,
                    Name = timing.Name.Truncate(200),
                    timing.DurationMilliseconds,
                    timing.StartMilliseconds,
                    timing.IsRoot,
                    timing.Depth,
                    timing.CustomTimingsJson
                }));

                if (profiler.ClientTimings?.Timings?.Any() ?? false)
                {
                    // set the profilerId (isn't needed unless we are storing it)
                    foreach (var timing in profiler.ClientTimings.Timings)
                    {
                        timing.MiniProfilerId = profiler.Id;
                        timing.Id = Guid.NewGuid();
                    }

                    conn.Execute(SaveClientTimingsSql, profiler.ClientTimings.Timings.Select(timing => new
                    {
                        timing.Id,
                        timing.MiniProfilerId,
                        Name = timing.Name.Truncate(200),
                        timing.Start,
                        timing.Duration
                    }));
                }
            }
        }

        /// <summary>
        /// Asynchronously stores to <c>dbo.MiniProfilers</c> under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public override async Task SaveAsync(MiniProfiler profiler)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync().ConfigureAwait(false);
                await conn.ExecuteAsync(SaveSql, new
                {
                    profiler.Id,
                    profiler.Started,
                    Name = profiler.Name.Truncate(200),
                    User = profiler.User.Truncate(100),
                    RootTimingId = profiler.Root?.Id,
                    profiler.DurationMilliseconds,
                    profiler.HasUserViewed,
                    MachineName = profiler.MachineName.Truncate(100),
                    profiler.CustomLinksJson,
                    ClientTimingsRedirectCount = profiler.ClientTimings?.RedirectCount
                }).ConfigureAwait(false);

                var timings = new List<Timing>();
                if (profiler.Root != null)
                {
                    profiler.Root.MiniProfilerId = profiler.Id;
                    FlattenTimings(profiler.Root, timings);
                }

                await conn.ExecuteAsync(SaveTimingsSql, timings.Select(timing => new
                {
                    timing.Id,
                    timing.MiniProfilerId,
                    timing.ParentTimingId,
                    Name = timing.Name.Truncate(200),
                    timing.DurationMilliseconds,
                    timing.StartMilliseconds,
                    timing.IsRoot,
                    timing.Depth,
                    timing.CustomTimingsJson
                })).ConfigureAwait(false);

                if (profiler.ClientTimings?.Timings?.Any() ?? false)
                {
                    // set the profilerId (isn't needed unless we are storing it)
                    foreach (var timing in profiler.ClientTimings.Timings)
                    {
                        timing.MiniProfilerId = profiler.Id;
                        timing.Id = Guid.NewGuid();
                    }
                    await conn.ExecuteAsync(SaveClientTimingsSql, profiler.ClientTimings.Timings.Select(timing => new
                    {
                        timing.Id,
                        timing.MiniProfilerId,
                        Name = timing.Name.Truncate(200),
                        timing.Start,
                        timing.Duration
                    })).ConfigureAwait(false);
                }
            }
        }

        private string _loadSql;

        private string LoadSql => _loadSql ??= $@"
SELECT * FROM {MiniProfilersTable} WHERE Id = @id;
SELECT * FROM {MiniProfilerTimingsTable} WHERE MiniProfilerId = @id ORDER BY StartMilliseconds;
SELECT * FROM {MiniProfilerClientTimingsTable} WHERE MiniProfilerId = @id ORDER BY Start;";

        /// <summary>
        /// Loads the <c>MiniProfiler</c> identified by 'id' from the database.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public override MiniProfiler Load(Guid id)
        {
            MiniProfiler result;
            using (var conn = GetConnection())
            {
                using (var multi = conn.QueryMultiple(LoadSql, new { id }))
                {
                    result = multi.ReadSingleOrDefault<MiniProfiler>();
                    var timings = multi.Read<Timing>().AsList();
                    var clientTimings = multi.Read<ClientTiming>().AsList();

                    ConnectTimings(result, timings, clientTimings);
                }
            }

            if (result != null)
            {
                // HACK: stored dates are UTC, but are pulled out as local time
                result.Started = new DateTime(result.Started.Ticks, DateTimeKind.Utc);
            }
            return result;
        }

        /// <summary>
        /// Loads the <c>MiniProfiler</c> identified by 'id' from the database.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public override async Task<MiniProfiler> LoadAsync(Guid id)
        {
            MiniProfiler result;
            using (var conn = GetConnection())
            {
                using (var multi = await conn.QueryMultipleAsync(LoadSql, new { id }).ConfigureAwait(false))
                {
                    result = await multi.ReadSingleOrDefaultAsync<MiniProfiler>().ConfigureAwait(false);
                    var timings = (await multi.ReadAsync<Timing>().ConfigureAwait(false)).AsList();
                    var clientTimings = (await multi.ReadAsync<ClientTiming>().ConfigureAwait(false)).AsList();

                    ConnectTimings(result, timings, clientTimings);
                }
            }

            if (result != null)
            {
                // HACK: stored dates are UTC, but are pulled out as local time
                result.Started = new DateTime(result.Started.Ticks, DateTimeKind.Utc);
            }
            return result;
        }

        /// <summary>
        /// Sets a particular profiler session so it is considered "unviewed"  
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public override void SetUnviewed(string user, Guid id) => ToggleViewed(user, id, false);

        /// <summary>
        /// Asynchronously sets a particular profiler session so it is considered "unviewed"  
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public override Task SetUnviewedAsync(string user, Guid id) => ToggleViewedAsync(user, id, false);

        /// <summary>
        /// Sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public override void SetViewed(string user, Guid id) => ToggleViewed(user, id, true);

        /// <summary>
        /// Asynchronously sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public override Task SetViewedAsync(string user, Guid id) => ToggleViewedAsync(user, id, true);

        private string _toggleViewedSql;

        private string ToggleViewedSql => _toggleViewedSql ??= $@"
Update {MiniProfilersTable} 
   Set HasUserViewed = @hasUserVeiwed 
 Where Id = @id 
   And ""User"" = @user";

        private void ToggleViewed(string user, Guid id, bool hasUserVeiwed)
        {
            using (var conn = GetConnection())
            {
                conn.Execute(ToggleViewedSql, new { id, user, hasUserVeiwed });
            }
        }

        private async Task ToggleViewedAsync(string user, Guid id, bool hasUserVeiwed)
        {
            using (var conn = GetConnection())
            {
                await conn.ExecuteAsync(ToggleViewedSql, new { id, user, hasUserVeiwed }).ConfigureAwait(false);
            }
        }

        private string _getUnviewedIdsSql;

        private string GetUnviewedIdsSql => _getUnviewedIdsSql ??= $@"
  Select Id
    From {MiniProfilersTable}
   Where ""User"" = @user
     And HasUserViewed = false
Order By Started";

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <c>MiniProfilerOptions.UserProvider</c></param>
        /// <returns>The list of keys for the supplied user</returns>
        public override List<Guid> GetUnviewedIds(string user)
        {
            using (var conn = GetConnection())
            {
                return conn.Query<Guid>(GetUnviewedIdsSql, new { user }).AsList();
            }
        }

        /// <summary>
        /// Asynchronously returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <c>MiniProfilerOptions.UserProvider</c></param>
        /// <returns>The list of keys for the supplied user</returns>
        public override async Task<List<Guid>> GetUnviewedIdsAsync(string user)
        {
            using (var conn = GetConnection())
            {
                return (await conn.QueryAsync<Guid>(GetUnviewedIdsSql, new { user }).ConfigureAwait(false)).AsList();
            }
        }

        /// <summary>
        /// List the MiniProfiler Ids for the given search criteria.
        /// </summary>
        /// <param name="maxResults">The max number of results</param>
        /// <param name="start">Search window start</param>
        /// <param name="finish">Search window end</param>
        /// <param name="orderBy">Result order</param>
        /// <returns>The list of GUID keys</returns>
        public override IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            using (var conn = GetConnection())
            {
                var query = BuildListQuery(start, finish, orderBy);
                return conn.Query<Guid>(query, new { maxResults, start, finish }).AsList();
            }
        }

        /// <summary>
        /// Asynchronously returns the MiniProfiler Ids for the given search criteria.
        /// </summary>
        /// <param name="maxResults">The max number of results</param>
        /// <param name="start">Search window start</param>
        /// <param name="finish">Search window end</param>
        /// <param name="orderBy">Result order</param>
        /// <returns>The list of GUID keys</returns>
        public override async Task<IEnumerable<Guid>> ListAsync(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            using (var conn = GetConnection())
            {
                var query = BuildListQuery(start, finish, orderBy);
                return await conn.QueryAsync<Guid>(query, new { maxResults, start, finish }).ConfigureAwait(false);
            }
        }

        private string BuildListQuery(DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            var sb = StringBuilderCache.Get();
            sb.Append(@"
Select Id
  From ").Append(MiniProfilersTable).Append(@"
");
            if (finish != null)
            {
                sb.AppendLine("Where Started < @finish");
            }
            if (start != null)
            {
                sb.AppendLine(finish != null
                    ? "  And Started > @start"
                    : "Where Started > @start");
            }
            sb.Append("Order By ").AppendLine(orderBy == ListResultsOrder.Descending ? "Started Desc" : "Started Asc")
                .AppendLine("Limit {=maxResults} ");

            return sb.ToStringRecycle();
        }

        /// <summary>
        /// Returns a connection to Sql Server.
        /// </summary>
        protected override DbConnection GetConnection() => new NpgsqlConnection(ConnectionString);

        /// <summary>
        /// SQL statements to create the SQL Server tables.
        /// </summary>
        protected override IEnumerable<string> GetTableCreationScripts()
        {
            yield return $@"
CREATE TABLE {MiniProfilersTable}
(
    RowId                                serial primary key,
    Id                                   uuid not null, -- don't cluster on a guid
    RootTimingId                         uuid null,
    Name                                 varchar(200) null,
    Started                              timestamp(3) not null,
    DurationMilliseconds                 decimal(15,1) not null,
    ""User""                               varchar(100) null,
    HasUserViewed                        boolean not null,
    MachineName                          varchar(100) null,
    CustomLinksJson                      varchar,
    ClientTimingsRedirectCount           integer null
);
-- displaying results selects everything based on the main MiniProfilers.Id column
CREATE UNIQUE INDEX IX_{MiniProfilersTable}_Id ON {MiniProfilersTable} (Id);
                
-- speeds up a query that is called on every .Stop()
CREATE INDEX IX_{MiniProfilersTable}_User_HasUserViewed_Includes ON {MiniProfilersTable} (""User"", HasUserViewed); 

CREATE TABLE {MiniProfilerTimingsTable}
(
    RowId                               serial primary key,
    Id                                  uuid not null,
    MiniProfilerId                      uuid not null,
    ParentTimingId                      uuid null,
    Name                                varchar(200) not null,
    DurationMilliseconds                decimal(15,3) not null,
    StartMilliseconds                   decimal(15,3) not null,
    IsRoot                              boolean not null,
    Depth                               smallint not null,
    CustomTimingsJson                   varchar null
);

CREATE UNIQUE INDEX IX_{MiniProfilerTimingsTable}_Id ON {MiniProfilerTimingsTable} (Id);
CREATE INDEX IX_{MiniProfilerTimingsTable}_MiniProfilerId ON {MiniProfilerTimingsTable} (MiniProfilerId);

CREATE TABLE {MiniProfilerClientTimingsTable}
(
    RowId                               serial primary key,
    Id                                  uuid not null,
    MiniProfilerId                      uuid not null,
    Name                                varchar(200) not null,
    Start                               decimal(9, 3) not null,
    Duration                            decimal(9, 3) not null
);

CREATE UNIQUE INDEX IX_{MiniProfilerClientTimingsTable}_Id on {MiniProfilerClientTimingsTable} (Id);
CREATE INDEX IX_{MiniProfilerClientTimingsTable}_MiniProfilerId on {MiniProfilerClientTimingsTable} (MiniProfilerId);             
";
        }
    }
}
