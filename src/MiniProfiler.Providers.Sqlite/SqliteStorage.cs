using Dapper;
using Microsoft.Data.Sqlite;
using StackExchange.Profiling.Internal;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a SQLite database.
    /// </summary>
    public class SqliteStorage : DatabaseStorageBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteStorage"/> class with the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to use.</param>
        public SqliteStorage(string connectionString) : base(connectionString) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteStorage"/> class with the specified connection string
        /// and the given table names to use.
        /// </summary>
        /// <param name="connectionString">The connection string to use.</param>
        /// <param name="profilersTable">The table name to use for MiniProfilers.</param>
        /// <param name="timingsTable">The table name to use for MiniProfiler Timings.</param>
        /// <param name="clientTimingsTable">The table name to use for MiniProfiler Client Timings.</param>
        public SqliteStorage(string connectionString, string profilersTable, string timingsTable, string clientTimingsTable)
            : base(connectionString, profilersTable, timingsTable, clientTimingsTable) { }

        private string _saveSql;
        private string SaveSql => _saveSql ?? (_saveSql = $@"
INSERT INTO {MiniProfilersTable}
            (Id, RootTimingId, Name, Started, DurationMilliseconds, [User], HasUserViewed, MachineName, CustomLinksJson, ClientTimingsRedirectCount)
SELECT      @Id, @RootTimingId, @Name, @Started, @DurationMilliseconds, @User, @HasUserViewed, @MachineName, @CustomLinksJson, @ClientTimingsRedirectCount
WHERE NOT EXISTS (SELECT 1 FROM {MiniProfilersTable} WHERE Id = @Id)");

        private string _saveTimingsSql;
        private string SaveTimingsSql => _saveTimingsSql ?? (_saveTimingsSql = $@"
INSERT INTO {MiniProfilerTimingsTable}
            (Id, MiniProfilerId, ParentTimingId, Name, DurationMilliseconds, StartMilliseconds, IsRoot, Depth, CustomTimingsJson)
SELECT      @Id, @MiniProfilerId, @ParentTimingId, @Name, @DurationMilliseconds, @StartMilliseconds, @IsRoot, @Depth, @CustomTimingsJson
WHERE NOT EXISTS (SELECT 1 FROM {MiniProfilerTimingsTable} WHERE Id = @Id)");

        private string _saveClientTimingsSql;
        private string SaveClientTimingsSql => _saveClientTimingsSql ?? (_saveClientTimingsSql = $@"
INSERT INTO {MiniProfilerClientTimingsTable}
            (Id, MiniProfilerId, Name, Start, Duration)
SELECT      @Id, @MiniProfilerId, @Name, @Start, @Duration
WHERE NOT EXISTS (SELECT 1 FROM {MiniProfilerClientTimingsTable} WHERE Id = @Id)");

        /// <summary>
        /// Stores to <c>dbo.MiniProfilers</c> under its <see cref="MiniProfiler.Id"/>;
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public override void Save(MiniProfiler profiler)
        {
            using (var conn = GetConnection())
            {
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
        private string LoadSql => _loadSql ?? (_loadSql = $@"
SELECT * FROM {MiniProfilersTable} WHERE Id = @id;
SELECT * FROM {MiniProfilerTimingsTable} WHERE MiniProfilerId = @id ORDER BY StartMilliseconds;
SELECT * FROM {MiniProfilerClientTimingsTable} WHERE MiniProfilerId = @id ORDER BY Start;");

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
        private string ToggleViewedSql => _toggleViewedSql ?? (_toggleViewedSql = $@"
Update {MiniProfilersTable} 
   Set HasUserViewed = @hasUserVeiwed 
 Where Id = @id 
   And [User] = @user");

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
        private string GetUnviewedIdsSql => _getUnviewedIdsSql ?? (_getUnviewedIdsSql = $@"
  Select Cast(Id as text) Id
    From {MiniProfilersTable}
   Where [User] = @user
     And HasUserViewed = 0
Order By Started");

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
Select Cast(Id as text) Id
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
            sb.Append("Order By ").AppendLine(orderBy == ListResultsOrder.Descending ? "Started Desc" : "Started Asc");
            sb.Append("LIMIT({=maxResults})");

            return sb.ToStringRecycle();
        }

        /// <summary>
        /// Returns a connection to Sql Server.
        /// </summary>
        protected override DbConnection GetConnection() => new SqliteConnection(ConnectionString);

        /// <summary>
        /// Creates the database schema from scratch, for initial spinup.
        /// </summary>
        /// <param name="additionalSqlStatements">(Optional) Extra SQL to run, e.g. additional tables to create.</param>
        public SqliteStorage WithSchemaCreation(params string[] additionalSqlStatements)
        {
            CreateSchema(ConnectionString, additionalSqlStatements);
            return this;
        }

        /// <summary>
        /// Creates the database schema from scratch, for initial spinup.
        /// </summary>
        /// <param name="connectionString">The connection string to use.</param>
        /// <param name="additionalSqlStatements">(Optional) Extra SQL to run, e.g. additional tables to create.</param>
        public void CreateSchema(string connectionString, params string[] additionalSqlStatements)
        {
            using (var cnn = new SqliteConnection(connectionString))
            {
                // We need some tiny mods to allow SQLite support 
                foreach (var sql in TableCreationScripts.Union(additionalSqlStatements))
                {
                    cnn.Execute(sql);
                }
            }
        }

        /// <summary>
        /// SQL statements to create the SQLite tables.
        /// </summary>
        protected override IEnumerable<string> GetTableCreationScripts()
        {
            yield return $@"Create Table {MiniProfilersTable}
                  (
                     RowId                                integer not null primary key,
                     Id                                   uniqueidentifier not null, 
                     RootTimingId                         uniqueidentifier null,
                     Name                                 nvarchar(200) not null,
                     Started                              datetime not null,
                     DurationMilliseconds                 decimal(9, 3) not null,
                     User                                 nvarchar(100) null,
                     HasUserViewed                        bit not null,
                     MachineName                          nvarchar(100) null,
                     CustomLinksJson                      text null,
                     ClientTimingsRedirectCount           int null
                  );";
            yield return $@"Create Table {MiniProfilerTimingsTable}
                  (
                     RowId                               integer not null primary key,
                     Id                                  uniqueidentifier not null,
                     MiniProfilerId                      uniqueidentifier not null,
                     ParentTimingId                      uniqueidentifier null,
                     Name                                nvarchar(200) not null,
                     DurationMilliseconds                decimal(9, 3) not null,
                     StartMilliseconds                   decimal(9, 3) not null,
                     IsRoot                              bit not null,
                     Depth                               smallint not null,
                     CustomTimingsJson                   text null
                  );";
            yield return $@"Create Table {MiniProfilerClientTimingsTable}
                  (
                     RowId                               integer not null primary key,
                     Id                                  uniqueidentifier not null,
                     MiniProfilerId                      uniqueidentifier not null,
                     Name                                nvarchar(200) not null,
                     Start                               decimal(9, 3) not null,
                     Duration                            decimal(9, 3) not null
                  );";
        }
    }
}
