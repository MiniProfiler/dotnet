using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a MySQL database.
    /// </summary>
    public sealed class MySqlStorage : DatabaseStorageBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlStorage"/> class with the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to use.</param>
        public MySqlStorage(string connectionString) : base(connectionString) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlStorage"/> class with the specified connection string
        /// and the given table names to use.
        /// </summary>
        /// <param name="connectionString">The connection string to use.</param>
        /// <param name="profilersTable">The table name to use for MiniProfilers.</param>
        /// <param name="timingsTable">The table name to use for MiniProfiler Timings.</param>
        /// <param name="clientTimingsTable">The table name to use for MiniProfiler Client Timings.</param>
        public MySqlStorage(string connectionString, string profilersTable, string timingsTable, string clientTimingsTable)
            : base(connectionString, profilersTable, timingsTable, clientTimingsTable) { }

        private string _saveSql, _saveTimingsSql, _saveClientTimingsSql;

        private string SaveSql => _saveSql ??= $@"
INSERT IGNORE INTO {MiniProfilersTable}
            (Id, RootTimingId, Name, Started, DurationMilliseconds, User, HasUserViewed, MachineName, CustomLinksJson, ClientTimingsRedirectCount)
VALUES(@Id, @RootTimingId, @Name, @Started, @DurationMilliseconds, @User, @HasUserViewed, @MachineName, @CustomLinksJson, @ClientTimingsRedirectCount)";

        private string SaveTimingsSql => _saveTimingsSql ??= $@"
INSERT IGNORE INTO {MiniProfilerTimingsTable}
            (Id, MiniProfilerId, ParentTimingId, Name, DurationMilliseconds, StartMilliseconds, IsRoot, Depth, CustomTimingsJson)
VALUES(@Id, @MiniProfilerId, @ParentTimingId, @Name, @DurationMilliseconds, @StartMilliseconds, @IsRoot, @Depth, @CustomTimingsJson)";

        private string SaveClientTimingsSql => _saveClientTimingsSql ??= $@"
INSERT IGNORE INTO {MiniProfilerClientTimingsTable}
            (Id, MiniProfilerId, Name, Start, Duration)
VALUES(@Id, @MiniProfilerId, @Name, @Start, @Duration)";

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

        private string _sqlStatements;

        private string SqlStatements => _sqlStatements ??= $@"
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
                using (var multi = conn.QueryMultiple(SqlStatements, new { id }))
                {
                    result = multi.ReadSingleOrDefault<MiniProfiler>();
                    var timings = multi.Read<Timing>().AsList();
                    var clientTimings = multi.Read<ClientTiming>().AsList();

                    ConnectTimings(result, timings, clientTimings);
                }
            }

            if (result != null)
            {
                // HACK: stored dates are UTC, but are pulled out as unspecified
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
                using (var multi = await conn.QueryMultipleAsync(SqlStatements, new { id }).ConfigureAwait(false))
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
UPDATE {MiniProfilersTable} 
   SET HasUserViewed = @hasUserViewed 
 WHERE Id = @id 
   AND User = @user";

        private void ToggleViewed(string user, Guid id, bool hasUserViewed)
        {
            using (var conn = GetConnection())
            {
                conn.Execute(ToggleViewedSql, new { id, user, hasUserViewed });
            }
        }

        private async Task ToggleViewedAsync(string user, Guid id, bool hasUserViewed)
        {
            using (var conn = GetConnection())
            {
                await conn.ExecuteAsync(ToggleViewedSql, new { id, user, hasUserViewed }).ConfigureAwait(false);
            }
        }

        private string _getUnviewedIdsSql;

        private string GetUnviewedIdsSql => _getUnviewedIdsSql ??= $@"
  SELECT Id
    FROM {MiniProfilersTable}
   WHERE User = @user
     AND HasUserViewed = 0
ORDER BY Started";

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
            var sb = new StringBuilder(@"
SELECT Id
  FROM ").Append(MiniProfilersTable).Append(@"
");
            if (finish != null)
            {
                sb.AppendLine("WHERE Started < @finish");
            }
            if (start != null)
            {
                sb.AppendLine(finish != null
                    ? "  AND Started > @start"
                    : "WHERE Started > @start");
            }
            sb.Append("ORDER BY ").Append(orderBy == ListResultsOrder.Descending ? "Started DESC" : "Started ASC");
            sb.Append(" LIMIT @maxResults");

            return sb.ToString();
        }

        /// <summary>
        /// Returns a connection to MySQL Server.
        /// </summary>
        protected override DbConnection GetConnection() => new MySqlConnection(ConnectionString);

        /// <summary>
        /// SQL statements to create the MySQL tables.
        /// </summary>
        protected override IEnumerable<string> GetTableCreationScripts()
        {
            yield return $@"
CREATE TABLE {MiniProfilersTable}
(
    RowId                                integer not null auto_increment primary key,
    Id                                   char(36) not null collate ascii_general_ci,
    RootTimingId                         char(36) null collate ascii_general_ci,
    Name                                 varchar(200) null,
    Started                              datetime not null,
    DurationMilliseconds                 decimal(15,1) not null,
    User                                 varchar(100) null,
    HasUserViewed                        bool not null,
    MachineName                          varchar(100) null,
    CustomLinksJson                      longtext,
    ClientTimingsRedirectCount           int null,
    UNIQUE INDEX IX_{MiniProfilersTable}_Id (Id), -- displaying results selects everything based on the main MiniProfilers.Id column
    INDEX IX_{MiniProfilersTable}_User_HasUserViewed (User, HasUserViewed) -- speeds up a query that is called on every .Stop()
) engine=InnoDB collate utf8mb4_bin;";
            yield return $@"
CREATE TABLE {MiniProfilerTimingsTable}
(
    RowId                               integer not null auto_increment primary key,
    Id                                  char(36) not null collate ascii_general_ci,
    MiniProfilerId                      char(36) not null collate ascii_general_ci,
    ParentTimingId                      char(36) null collate ascii_general_ci,
    Name                                varchar(200) not null,
    DurationMilliseconds                decimal(15,3) not null,
    StartMilliseconds                   decimal(15,3) not null,
    IsRoot                              bool not null,
    Depth                               smallint not null,
    CustomTimingsJson                   longtext null,
    UNIQUE INDEX IX_{MiniProfilerTimingsTable}_Id (Id),
    INDEX IX_{MiniProfilerTimingsTable}_MiniProfilerId (MiniProfilerId)
) engine=InnoDB collate utf8mb4_bin;";
            yield return $@"
CREATE TABLE {MiniProfilerClientTimingsTable}
(
    RowId                               integer not null auto_increment primary key,
    Id                                  char(36) not null collate ascii_general_ci,
    MiniProfilerId                      char(36) not null collate ascii_general_ci,
    Name                                varchar(200) not null,
    Start                               decimal(9, 3) not null,
    Duration                            decimal(9, 3) not null,
    UNIQUE INDEX IX_{MiniProfilerClientTimingsTable}_Id (Id),
    INDEX IX_{MiniProfilerClientTimingsTable}_MiniProfilerId (MiniProfilerId)
) engine=InnoDB collate utf8mb4_bin;";
        }
    }
}
