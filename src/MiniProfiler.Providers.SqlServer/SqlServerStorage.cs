using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

using Dapper;
using StackExchange.Profiling.Helpers;
using System.Threading.Tasks;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a MSSQL database.
    /// </summary>
    public class SqlServerStorage : DatabaseStorageBase
    {
        /// <summary>
        /// Load the SQL statements (using Dapper Multiple Results)
        /// </summary>
        protected readonly string SqlStatements = @"
SELECT * FROM MiniProfilers WHERE Id = @id;
SELECT * FROM MiniProfilerTimings WHERE MiniProfilerId = @id ORDER BY StartMilliseconds;
SELECT * FROM MiniProfilerClientTimings WHERE MiniProfilerId = @id ORDER BY Start;";

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerStorage"/> class with the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to use.</param>
        public SqlServerStorage(string connectionString) : base(connectionString) { /* base call */ }

        private const string _saveSql =
@"INSERT INTO MiniProfilers
            (Id,  RootTimingId,  Name,  Started,  DurationMilliseconds, [User], HasUserViewed,  MachineName,  CustomLinksJson,  ClientTimingsRedirectCount)
SELECT      @Id, @RootTimingId, @Name, @Started, @DurationMilliseconds, @User, @HasUserViewed, @MachineName, @CustomLinksJson, @ClientTimingsRedirectCount
WHERE NOT EXISTS (SELECT 1 FROM MiniProfilers WHERE Id = @Id)"; // this syntax works on both mssql and sqlite

        private const string _saveTimingsSql = @"
INSERT INTO MiniProfilerTimings
            (Id,  MiniProfilerId,  ParentTimingId,  Name,  DurationMilliseconds,  StartMilliseconds,  IsRoot,  Depth,  CustomTimingsJson)
SELECT      @Id, @MiniProfilerId, @ParentTimingId, @Name, @DurationMilliseconds, @StartMilliseconds, @IsRoot, @Depth, @CustomTimingsJson
WHERE NOT EXISTS (SELECT 1 FROM MiniProfilerTimings WHERE Id = @Id)";

        private const string _saveClientTimingsSql = @"
INSERT INTO MiniProfilerClientTimings
            (Id,  MiniProfilerId,  Name,  Start,  Duration)
SELECT      @Id, @MiniProfilerId, @Name, @Start, @Duration
WHERE NOT EXISTS (SELECT 1 FROM MiniProfilerClientTimings WHERE Id = @Id)";

        /// <summary>
        /// Stores to <c>dbo.MiniProfilers</c> under its <see cref="MiniProfiler.Id"/>;
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public override void Save(MiniProfiler profiler)
        {
            using (var conn = GetConnection())
            {
                conn.Execute(_saveSql, new
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

                conn.Execute(_saveTimingsSql, timings.Select(timing => new
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

                    conn.Execute(_saveClientTimingsSql, profiler.ClientTimings.Timings.Select(timing => new
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
                await conn.ExecuteAsync(_saveSql, new
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

                await conn.ExecuteAsync(_saveTimingsSql, timings.Select(timing => new
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
                    await conn.ExecuteAsync(_saveClientTimingsSql, profiler.ClientTimings.Timings.Select(timing => new
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
                // HACK: stored dates are utc, but are pulled out as local time
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
                // HACK: stored dates are utc, but are pulled out as local time
                result.Started = new DateTime(result.Started.Ticks, DateTimeKind.Utc);
            }
            return result;
        }

        /// <summary>
        /// Sets a particular profiler session so it is considered "un-viewed"  
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public override void SetUnviewed(string user, Guid id) => ToggleViewed(user, id, false);

        /// <summary>
        /// Asynchronously sets a particular profiler session so it is considered "un-viewed"  
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

        private const string _toggleViewedSql = @"
Update MiniProfilers 
   Set HasUserViewed = @hasUserVeiwed 
 Where Id = @id 
   And [User] = @user";

        private void ToggleViewed(string user, Guid id, bool hasUserVeiwed)
        {
            using (var conn = GetConnection())
            {
                conn.Execute(_toggleViewedSql, new { id, user, hasUserVeiwed });
            }
        }

        private async Task ToggleViewedAsync(string user, Guid id, bool hasUserVeiwed)
        {
            using (var conn = GetConnection())
            {
                await conn.ExecuteAsync(_toggleViewedSql, new { id, user, hasUserVeiwed }).ConfigureAwait(false);
            }
        }

        private const string _getUnviewedIdsSql = @"
  Select Id
    From MiniProfilers
   Where [User] = @user
     And HasUserViewed = 0
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
                return conn.Query<Guid>(_getUnviewedIdsSql, new { user }).AsList();
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
                return (await conn.QueryAsync<Guid>(_getUnviewedIdsSql, new { user }).ConfigureAwait(false)).AsList();
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
Select Top {=maxResults} Id
  From MiniProfilers
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
            sb.Append("Order By ").Append(orderBy == ListResultsOrder.Descending ? "Started Desc" : "Started Asc");

            return sb.ToString();
        }

        /// <summary>
        /// Load individual MiniProfiler
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="keyParameter">The id Parameter.</param>
        /// <returns>Related MiniProfiler object</returns>
        private MiniProfiler LoadProfilerRecord(DbConnection connection, object keyParameter)
        {
            using (var multi = connection.QueryMultiple(SqlStatements, keyParameter))
            {
                var profiler = multi.Read<MiniProfiler>().SingleOrDefault();
                var timings = multi.Read<Timing>().ToList();
                var clientTimings = multi.Read<ClientTiming>().ToList();

                ConnectTimings(profiler, timings, clientTimings);

                return profiler;
            }
        }

        /// <summary>
        /// Returns a connection to Sql Server.
        /// </summary>
        protected override DbConnection GetConnection() => new SqlConnection(ConnectionString);

        /// <summary>
        /// Creates needed tables. Run this once on your database.
        /// </summary>
        /// <remarks>
        /// Works in SQL server and <c>sqlite</c> (with documented removals).
        /// </remarks>
        public const string TableCreationScript =
                @"
                create table MiniProfilers
                  (
                     RowId                                integer not null identity constraint PK_MiniProfilers primary key clustered, -- Need a clustered primary key for SQL Azure
                     Id                                   uniqueidentifier not null, -- don't cluster on a guid
                     RootTimingId                         uniqueidentifier null,
                     Name                                 nvarchar(200) null,
                     Started                              datetime not null,
                     DurationMilliseconds                 decimal(7, 1) not null,
                     [User]                               nvarchar(100) null,
                     HasUserViewed                        bit not null,
                     MachineName                          nvarchar(100) null,
                     CustomLinksJson                      nvarchar(max),
                     ClientTimingsRedirectCount           int null
                  );
                
                -- displaying results selects everything based on the main MiniProfilers.Id column
                create unique nonclustered index IX_MiniProfilers_Id on MiniProfilers (Id);
                
                -- speeds up a query that is called on every .Stop()
                create nonclustered index IX_MiniProfilers_User_HasUserViewed_Includes on MiniProfilers ([User], HasUserViewed) include (Id, [Started]); 

                create table MiniProfilerTimings
                  (
                     RowId                               integer not null identity constraint PK_MiniProfilerTimings primary key clustered,
                     Id                                  uniqueidentifier not null,
                     MiniProfilerId                      uniqueidentifier not null,
                     ParentTimingId                      uniqueidentifier null,
                     Name                                nvarchar(200) not null,
                     DurationMilliseconds                decimal(9, 3) not null,
                     StartMilliseconds                   decimal(9, 3) not null,
                     IsRoot                              bit not null,
                     Depth                               smallint not null,
                     CustomTimingsJson                   nvarchar(max) null
                  );

                 create unique nonclustered index IX_MiniProfilerTimings_Id on MiniProfilerTimings (Id);
                 create nonclustered index IX_MiniProfilerTimings_MiniProfilerId on MiniProfilerTimings (MiniProfilerId);

                 create table MiniProfilerClientTimings
                  (
                     RowId                               integer not null identity constraint PK_MiniProfilerClientTimings primary key clustered,
                     Id                                  uniqueidentifier not null,
                     MiniProfilerId                      uniqueidentifier not null,
                     Name                                nvarchar(200) not null,
                     Start                               decimal(9, 3) not null,
                     Duration                            decimal(9, 3) not null
                  );

                 create unique nonclustered index IX_MiniProfilerClientTimings_Id on MiniProfilerClientTimings (Id);
                 create nonclustered index IX_MiniProfilerClientTimings_MiniProfilerId on MiniProfilerClientTimings (MiniProfilerId);             
                ";
    }
}
