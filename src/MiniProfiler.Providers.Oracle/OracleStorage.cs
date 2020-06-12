using Dapper;
using Oracle.ManagedDataAccess.Client;
using StackExchange.Profiling.Internal;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a Oracle Database database.
    /// </summary>
    public class OracleStorage : DatabaseStorageBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OracleStorage"/> class with the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to use.</param>
        public OracleStorage(string connectionString) : base(connectionString) { /* base call */ }

        /// <summary>
        /// Initializes a new instance of the <see cref="OracleStorage"/> class with the specified connection string
        /// and the given table names to use.
        /// </summary>
        /// <param name="connectionString">The connection string to use.</param>
        /// <param name="profilersTable">The table name to use for MiniProfilers.</param>
        /// <param name="timingsTable">The table name to use for MiniProfiler Timings.</param>
        /// <param name="clientTimingsTable">The table name to use for MiniProfiler Client Timings.</param>
        public OracleStorage(string connectionString, string profilersTable, string timingsTable, string clientTimingsTable)
            : base(connectionString, profilersTable, timingsTable, clientTimingsTable) { }

        private string _saveSql;
        private string SaveSql => _saveSql ?? (_saveSql = $@"
INSERT INTO {MiniProfilersTable}
            (""Id"", RootTimingId, ""Name"", Started, DurationMilliseconds, ""User"", HasUserViewed, MachineName, CustomLinksJson, ClientTimingsRedirectCount)
SELECT      :pId, :pRootTimingId, :pName, :pStarted, :pDurationMilliseconds, :pUser, :pHasUserViewed, :pMachineName, :pCustomLinksJson, :pClientTimingsRedirectCount
  FROM DUAL
 WHERE NOT EXISTS (SELECT 1 FROM {MiniProfilersTable} WHERE ""Id"" = :pId)");

        private string _saveTimingsSql;
        private string SaveTimingsSql => _saveTimingsSql ?? (_saveTimingsSql = $@"
INSERT INTO {MiniProfilerTimingsTable}
            (""Id"", MiniProfilerId, ParentTimingId, ""Name"", DurationMilliseconds, StartMilliseconds, IsRoot, ""Depth"", CustomTimingsJson)
SELECT      :pId, :pMiniProfilerId, :pParentTimingId, :pName, :pDurationMilliseconds, :pStartMilliseconds, :pIsRoot, :pDepth, :pCustomTimingsJson
  FROM DUAL
 WHERE NOT EXISTS (SELECT 1 FROM {MiniProfilerTimingsTable} WHERE ""Id"" = :pId)");

        private string _saveClientTimingsSql;
        private string SaveClientTimingsSql => _saveClientTimingsSql ?? (_saveClientTimingsSql = $@"
INSERT INTO {MiniProfilerClientTimingsTable}
            (""Id"", MiniProfilerId, ""Name"", ""Start"", ""Duration"")
SELECT      :pId, :pMiniProfilerId, :pName, :pStart, :pDuration
  FROM DUAL
 WHERE NOT EXISTS (SELECT 1 FROM {MiniProfilerClientTimingsTable} WHERE ""Id"" = :pId)");

        /// <summary>
        /// Stores to <c>dbo.MiniProfilers</c> under its <see cref="MiniProfiler.Id"/>;
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public override void Save(MiniProfiler profiler)
        {
            using (var conn = GetConnection())
            {
                conn.Execute(SaveSql, ToProfilerTiming(profiler));

                var timings = new List<Timing>();
                if (profiler.Root != null)
                {
                    profiler.Root.MiniProfilerId = profiler.Id;
                    FlattenTimings(profiler.Root, timings);
                }

                conn.Execute(SaveTimingsSql, timings.Select(timing => ToTiming(timing)));

                if (profiler.ClientTimings?.Timings?.Any() ?? false)
                {
                    // set the profilerId (isn't needed unless we are storing it)
                    foreach (var timing in profiler.ClientTimings.Timings)
                    {
                        timing.MiniProfilerId = profiler.Id;
                        timing.Id = Guid.NewGuid();
                    }

                    conn.Execute(SaveClientTimingsSql, profiler.ClientTimings.Timings.Select(timing => ToClientTiming(timing)));
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
                await conn.ExecuteAsync(SaveSql, ToProfilerTiming(profiler)).ConfigureAwait(false);

                var timings = new List<Timing>();
                if (profiler.Root != null)
                {
                    profiler.Root.MiniProfilerId = profiler.Id;
                    FlattenTimings(profiler.Root, timings);
                }

                await conn.ExecuteAsync(SaveTimingsSql, timings.Select(timing => ToTiming(timing))).ConfigureAwait(false);

                if (profiler.ClientTimings?.Timings?.Any() ?? false)
                {
                    // set the profilerId (isn't needed unless we are storing it)
                    foreach (var timing in profiler.ClientTimings.Timings)
                    {
                        timing.MiniProfilerId = profiler.Id;
                        timing.Id = Guid.NewGuid();
                    }
                    await conn.ExecuteAsync(SaveClientTimingsSql, profiler.ClientTimings.Timings.Select(timing => ToClientTiming(timing))).ConfigureAwait(false);
                }
            }
        }

        private object ToProfilerTiming(MiniProfiler profiler) => new
        {
            pId = profiler.Id.ToString(),
            pStarted = profiler.Started,
            pName = profiler.Name.Truncate(200),
            pUser = profiler.User.Truncate(100),
            pRootTimingId = profiler.Root?.Id.ToString(),
            pDurationMilliseconds = profiler.DurationMilliseconds,
            pHasUserViewed = profiler.HasUserViewed ? 1 : 0,
            pMachineName = profiler.MachineName.Truncate(100),
            pCustomLinksJson = profiler.CustomLinksJson,
            pClientTimingsRedirectCount = profiler.ClientTimings?.RedirectCount
        };

        private object ToTiming(Timing timing) => new
        {
            pId = timing.Id.ToString(),
            pMiniProfilerId = timing.MiniProfilerId.ToString(),
            pParentTimingId = timing.ParentTimingId.ToString(),
            pName = timing.Name.Truncate(200),
            pDurationMilliseconds = timing.DurationMilliseconds,
            pStartMilliseconds = timing.StartMilliseconds,
            pIsRoot = timing.IsRoot ? 1 : 0,
            pDepth = timing.Depth,
            pCustomTimingsJson = timing.CustomTimingsJson
        };

        private object ToClientTiming(ClientTiming timing) => new
        {
            pId = timing.Id.ToString(),
            pMiniProfilerId = timing.MiniProfilerId.ToString(),
            pName = timing.Name.Truncate(200),
            pStart = timing.Start,
            pDuration = timing.Duration
        };
        
        private string _loadSqlProfiler;
        private string _loadSqlTimings;
        private string _loadSqlClientTimings;
        
        private string LoadSqlProfiler => _loadSqlProfiler ?? (_loadSqlProfiler = $@"SELECT * FROM {MiniProfilersTable} WHERE ""Id"" = :pId");
        private string LoadSqlTimings => _loadSqlTimings ?? (_loadSqlTimings = $@"SELECT * FROM {MiniProfilerTimingsTable} WHERE MiniProfilerId = :pId ORDER BY StartMilliseconds");
        private string LoadSqlClientTimings => _loadSqlClientTimings ?? (_loadSqlClientTimings = $@"SELECT * FROM {MiniProfilerClientTimingsTable} WHERE MiniProfilerId = :pId ORDER BY ""Start""");
        
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
                try
                {
                    var dresult = conn.Query<dynamic>(LoadSqlProfiler, new { pId = id.ToString() }).FirstOrDefault();
                    result = 
                    
                    result = conn.Query<MiniProfiler>(LoadSqlProfiler, new { pId = id.ToString() }).FirstOrDefault();
                    var timings = conn.Query<Timing>(LoadSqlTimings,  new { pId = id.ToString() }).AsList();
                    var clientTimings = conn.Query<ClientTiming>(LoadSqlClientTimings, new { pId = id.ToString() }).AsList();

                    ConnectTimings(result, timings, clientTimings);
                }
                catch (Exception ex)
                {
                    throw;
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
                result = (await conn.QueryAsync<MiniProfiler>(LoadSqlProfiler, new { pId = id.ToString() }).ConfigureAwait(false)).FirstOrDefault();
                var timings = (await conn.QueryAsync<Timing>(LoadSqlTimings,  new { pId = id.ToString() }).ConfigureAwait(false)).AsList();
                var clientTimings = (await conn.QueryAsync<ClientTiming>(LoadSqlClientTimings, new { pId = id.ToString() }).ConfigureAwait(false)).AsList();

                ConnectTimings(result, timings, clientTimings);
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
   Set HasUserViewed = :hasUserVeiwed 
 Where ""Id"" = :pId 
   And ""User"" = :user");

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
  Select ""Id""
    From {MiniProfilersTable}
   Where ""User"" = :pUser
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
                var ids = conn.Query<string>(GetUnviewedIdsSql, new { pUser = user }).ToList();
                return ids.Select(id => new Guid(id)).AsList();
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
                var ids = await conn.QueryAsync<string>(GetUnviewedIdsSql, new { pUser = user }).ConfigureAwait(false);
                return ids.Select(id => new Guid(id)).AsList();
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
                var ids = conn.Query<string>(query, new { maxResults, start, finish });
                return ids.Select(id => new Guid(id));
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
                var ids = await conn.QueryAsync<string>(query, new { maxResults, start, finish }).ConfigureAwait(false);
                return ids.Select(id => new Guid(id));
            }
        }

        private string BuildListQuery(DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            var sb = StringBuilderCache.Get();
            sb.Append(@"
Select ""Id""
  From ").Append(MiniProfilersTable).Append(@"
 Where rownum <= {=maxResults}");
            if (finish != null)
            {
                sb.AppendLine("  And Started < :finish");
            }
            if (start != null)
            {
                sb.AppendLine("  And Started > :start");
            }
            sb.Append(" Order By ").Append(orderBy == ListResultsOrder.Descending ? "Started Desc" : "Started Asc");

            return sb.ToStringRecycle();
        }

        /// <summary>
        /// Returns a connection to Oracle Database.
        /// </summary>
        protected override DbConnection GetConnection() => new OracleConnection(ConnectionString);

        /// <summary>
        /// SQL statements to create the Oracle Database tables.
        /// </summary>
        protected override IEnumerable<string> GetTableCreationScripts()
        {
            yield return $@"
CREATE TABLE {MiniProfilersTable}
(
    ""RowId""                              INTEGER NOT NULL,
    ""Id""                                 VARCHAR2(36 CHAR) NOT NULL,
    RootTimingId                         VARCHAR2(36 CHAR) NULL,
    ""Name""                               VARCHAR2(200 CHAR) NULL,
    Started                              DATE NOT NULL,
    DurationMilliseconds                 NUMBER(15,1) NOT NULL,
    ""User""                               VARCHAR2(100 CHAR) NULL,
    HasUserViewed                        NUMBER(1, 0) NOT NULL,
    MachineName                          VARCHAR2(100 CHAR) NULL,
    CustomLinksJson                      VARCHAR2(4000 CHAR),
    ClientTimingsRedirectCount           INTEGER NULL
);
ALTER TABLE {MiniProfilersTable} ADD CONSTRAINT PK_{MiniProfilersTable} PRIMARY KEY (""RowId"");

-- displaying results selects everything based on the main {MiniProfilersTable}.Id column
CREATE UNIQUE INDEX IX_{MiniProfilersTable}_1 ON {MiniProfilersTable} (""Id"");

-- speeds up a query that is called on every .Stop()
CREATE INDEX IX_{MiniProfilersTable}_2 ON {MiniProfilersTable} (""User"", HasUserViewed); 

CREATE SEQUENCE {MiniProfilersTable}_SEQ MINVALUE 1 MAXVALUE 999999999999999999999999999 INCREMENT BY 1 START WITH 1 NOCACHE ORDER NOCYCLE;

CREATE OR REPLACE TRIGGER {MiniProfilersTable}_IDT
   BEFORE INSERT ON {MiniProfilersTable}
   REFERENCING NEW AS NEW OLD AS OLD
   FOR EACH ROW WHEN (NVL(NEW.""RowId"", 0) = 0)
Begin
   SELECT MiniProfilers_SEQ.NEXTVAL INTO :NEW.""RowId"" FROM DUAL;
End;
/

----------------------------------------------------------------------------------------------

CREATE TABLE {MiniProfilerTimingsTable}
(
    ""RowId""                             INTEGER NOT NULL,
    ""Id""                                VARCHAR2(36 CHAR) NOT NULL,
    MiniProfilerId                      VARCHAR2(36 CHAR) NOT NULL,
    ParentTimingId                      VARCHAR2(36 CHAR) NULL,
    ""Name""                              VARCHAR2(200 CHAR) NOT NULL,
    DurationMilliseconds                NUMBER(15,3) NOT NULL,
    StartMilliseconds                   NUMBER(15,3) NOT NULL,
    IsRoot                              NUMBER(1, 0) NOT NULL,
    ""Depth""                             SMALLINT NOT NULL,
    CustomTimingsJson                   VARCHAR2(4000 CHAR) NULL
);
ALTER TABLE {MiniProfilerTimingsTable} ADD CONSTRAINT PK_{MiniProfilerTimingsTable} PRIMARY KEY (""RowId"");

CREATE UNIQUE INDEX IX_{MiniProfilerTimingsTable}_1 ON {MiniProfilerTimingsTable} (""Id"");
CREATE INDEX IX_{MiniProfilerTimingsTable}_2 ON {MiniProfilerTimingsTable} (MiniProfilerId);

CREATE SEQUENCE {MiniProfilerTimingsTable}_SEQ MINVALUE 1 MAXVALUE 999999999999999999999999999 INCREMENT BY 1 START WITH 1 NOCACHE ORDER NOCYCLE;

CREATE OR REPLACE TRIGGER {MiniProfilerTimingsTable}_IDT
   BEFORE INSERT ON {MiniProfilerTimingsTable}
   REFERENCING NEW AS NEW OLD AS OLD
   FOR EACH ROW WHEN (NVL(NEW.""RowId"", 0) = 0)
Begin
   SELECT {MiniProfilerTimingsTable}_SEQ.NEXTVAL INTO :NEW.""RowId"" FROM DUAL;
End;
/

----------------------------------------------------------------------------------------------

CREATE TABLE {MiniProfilerClientTimingsTable}
(
    ""RowId""                             INTEGER NOT NULL,
    ""Id""                                VARCHAR2(36 CHAR) NOT NULL,
    MiniProfilerId                      VARCHAR2(36 CHAR) NOT NULL,
    ""Name""                              VARCHAR2(200 CHAR) NOT NULL,
    ""Start""                             NUMBER(9, 3) NOT NULL,
    ""Duration""                          NUMBER(9, 3) NOT NULL
);
ALTER TABLE {MiniProfilerClientTimingsTable} ADD CONSTRAINT PK_{MiniProfilerClientTimingsTable} PRIMARY KEY (""RowId"");

CREATE UNIQUE INDEX IX_{MiniProfilerClientTimingsTable}_1 on {MiniProfilerClientTimingsTable} (""Id"");
CREATE INDEX IX_{MiniProfilerClientTimingsTable}_2 on {MiniProfilerClientTimingsTable} (MiniProfilerId);             

CREATE SEQUENCE {MiniProfilerClientTimingsTable}_SEQ MINVALUE 1 MAXVALUE 999999999999999999999999999 INCREMENT BY 1 START WITH 1 NOCACHE ORDER NOCYCLE;

CREATE OR REPLACE TRIGGER {MiniProfilerClientTimingsTable}_IDT
   BEFORE INSERT ON {MiniProfilerClientTimingsTable}
   REFERENCING NEW AS NEW OLD AS OLD
   FOR EACH ROW WHEN (NVL(NEW.""RowId"", 0) = 0)
Begin
   SELECT {MiniProfilerClientTimingsTable}_SEQ.NEXTVAL INTO :NEW.""RowId"" FROM DUAL;
End;
";
        }
    }
}
