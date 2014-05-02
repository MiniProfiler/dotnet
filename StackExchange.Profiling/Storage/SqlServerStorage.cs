namespace StackExchange.Profiling.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Helpers;
    using Helpers.Dapper;

    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a MSSQL database.
    /// </summary>
    public class SqlServerStorage : DatabaseStorageBase
    {
        /// <summary>
        /// Load the SQL statements (using Dapper Multiple Results)
        /// </summary>
        private const string SqlStatements = @"
SELECT * FROM MiniProfilers WHERE Id = @id;
SELECT * FROM MiniProfilerTimings WHERE MiniProfilerId = @id ORDER BY StartMilliseconds;
SELECT * FROM MiniProfilerClientTimings WHERE MiniProfilerId = @id ORDER BY Start;";

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerStorage"/> class with the specified connection string.
        /// </summary>
        /// <param name="connectionString">
        /// The connection string to use.
        /// </param>
        public SqlServerStorage(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerStorage"/> class with the specified connection string settings.
        /// </summary>
        /// <param name="connectionStringSettings">
        /// The connection string settings read from ConfigurationManager.ConnectionStrings["connection"]
        /// </param>
        public SqlServerStorage(ConnectionStringSettings connectionStringSettings)
            :base(connectionStringSettings.ConnectionString)
        {
        }
        
        /// <summary>
        /// Stores to <c>dbo.MiniProfilers</c> under its <see cref="MiniProfiler.Id"/>;
        /// </summary>
        /// <param name="profiler">The Mini Profiler</param>
        public override void Save(MiniProfiler profiler)
        {
            const string sql =
@"insert into MiniProfilers
            (Id,
             RootTimingId,
             Started,
             DurationMilliseconds,
             [User],
             HasUserViewed,
             MachineName,
             CustomLinksJson,
             ClientTimingsRedirectCount)
select       @Id,
             @RootTimingId,
             @Started,
             @DurationMilliseconds,
             @User,
             @HasUserViewed,
             @MachineName,
             @CustomLinksJson,
             @ClientTimingsRedirectCount
where not exists (select 1 from MiniProfilers where Id = @Id)"; // this syntax works on both mssql and sqlite

            using (var conn = GetOpenConnection())
            {
                conn.Execute(
                    sql,
                    new
                        {
                            profiler.Id,
                            profiler.Started,
                            User = profiler.User.Truncate(100),
                            RootTimingId = profiler.Root != null ? profiler.Root.Id : (Guid?)null,
                            profiler.DurationMilliseconds,
                            profiler.HasUserViewed,
                            MachineName = profiler.MachineName.Truncate(100),
                            profiler.CustomLinksJson,
                            ClientTimingsRedirectCount = profiler.ClientTimings != null ? profiler.ClientTimings.RedirectCount : (int?)null
                        });

                var timings = new List<Timing>();
                if (profiler.Root != null)
                {
                    profiler.Root.MiniProfilerId = profiler.Id;
                    FlattenTimings(profiler.Root, timings);
                }

                SaveTimings(timings, conn);
                if (profiler.ClientTimings != null && profiler.ClientTimings.Timings != null && profiler.ClientTimings.Timings.Any())
                {
                    // set the profilerId (isn't needed unless we are storing it)
                    profiler.ClientTimings.Timings.ForEach(x =>
                    {
                        x.MiniProfilerId = profiler.Id;
                        x.Id = Guid.NewGuid();
                    });
                    SaveClientTimings(profiler.ClientTimings.Timings, conn);
                }
            }
        }

        private void SaveTimings(List<Timing> timings, DbConnection conn)
        {
            const string sql = @"INSERT INTO MiniProfilerTimings
            (Id,
             MiniProfilerId,
             ParentTimingId,
             Name,
             DurationMilliseconds,
             StartMilliseconds,
             IsRoot,
             Depth,
             CustomTimingsJson)
SELECT       @Id,
             @MiniProfilerId,
             @ParentTimingId,
             @Name,
             @DurationMilliseconds,
             @StartMilliseconds,
             @IsRoot,
             @Depth,
             @CustomTimingsJson
WHERE NOT EXISTS (SELECT 1 FROM MiniProfilerTimings WHERE Id = @Id)";

            foreach (var timing in timings)
            {
                conn.Execute(
                    sql, 
                    new
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
                        });
            }
        }

        private void SaveClientTimings(List<ClientTimings.ClientTiming> timings, DbConnection conn)
        {
            const string sql = @"INSERT INTO MiniProfilerClientTimings
            ( Id,
              MiniProfilerId,
              Name,
              Start,
              Duration)
SELECT       @Id,
             @MiniProfilerId,
             @Name,
             @Start,
             @Duration
WHERE NOT EXISTS (SELECT 1 FROM MiniProfilerClientTimings WHERE Id = @Id)";

            foreach (var timing in timings)
            {
                conn.Execute(
                    sql,
                    new
                    {
                        timing.Id,
                        timing.MiniProfilerId,
                        Name = timing.Name.Truncate(200),
                        timing.Start,
                        timing.Duration
                    });
            }
        }

        private void FlattenTimings(Timing timing, List<Timing> timingsCollection)
        {
            timingsCollection.Add(timing);
            if (timing.HasChildren)
            {
                timing.Children.ForEach(x => FlattenTimings(x, timingsCollection));
            }
        }

        /// <summary>
        /// Loads the <c>MiniProfiler</c> identified by 'id' from the database.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>the mini profiler.</returns>
        public override MiniProfiler Load(Guid id)
        {
            using (var conn = GetOpenConnection())
            {
                var idParameter = new { id };
                var result = LoadProfilerRecord(conn, idParameter);

                if (result != null)
                {
                    // HACK: stored dates are utc, but are pulled out as local time
                    result.Started = new DateTime(result.Started.Ticks, DateTimeKind.Utc);
                }

                return result;
            }
        }

        /// <summary>
        /// Sets the session to un-viewed 
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="id">The id.</param>
        public override void SetUnviewed(string user, Guid id)
        {
            using (var conn = GetOpenConnection())
            {
                conn.Execute("UPDATE MiniProfilers SET HasUserViewed = 0 WHERE Id = @id AND [User] = @user", new { id, user });
            }
        }

        /// <summary>
        /// sets the session to viewed
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="id">The id.</param>
        public override void SetViewed(string user, Guid id)
        {
            using (var conn = GetOpenConnection())
            {
                conn.Execute("UPDATE MiniProfilers SET HasUserViewed = 1 WHERE Id = @id AND [User] = @user", new { id, user });
            }
        }

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current UserProvider"/&gt;.</param>
        /// <returns>the list of keys.</returns>
        public override List<Guid> GetUnviewedIds(string user)
        {
            const string Sql =
                @"select Id
                from   MiniProfilers
                where  [User] = @user
                and    HasUserViewed = 0
                order  by Started";

            using (var conn = GetOpenConnection())
            {
                return conn.Query<Guid>(Sql, new { user }).ToList();
            }
        }

        /// <summary>
        /// List the Results.
        /// </summary>
        /// <param name="maxResults">The max results.</param>
        /// <param name="start">The start.</param>
        /// <param name="finish">The finish.</param>
        /// <param name="orderBy">order by.</param>
        /// <returns>the list of key values.</returns>
        public override IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            var builder = new SqlBuilder();
            var t = builder.AddTemplate("select top " + maxResults + " Id from MiniProfilers /**where**/ /**orderby**/");

            if (finish != null)
            {
                builder.Where("Started < @finish", new { finish });
            }

            if (start != null)
            {
                builder.Where("Started > @start", new { start });
            }

            builder.OrderBy(orderBy == ListResultsOrder.Descending ? "Started desc" : "Started asc");

            using (var conn = GetOpenConnection())
            {
                return conn.Query<Guid>(t.RawSql, t.Parameters).ToList();
            }
        }
        
        /// <summary>
        /// Load individual MiniProfiler
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="keyParameter">The id Parameter.</param>
        /// <returns>Related MiniProfiler object</returns>
        private MiniProfiler LoadProfilerRecord(DbConnection connection, object keyParameter)
        {
            MiniProfiler profiler;
            using (var multi = connection.QueryMultiple(SqlStatements, keyParameter))
            {
                profiler = multi.Read<MiniProfiler>().SingleOrDefault();
                var timings = multi.Read<Timing>().ToList();
                var clientTimings = multi.Read<ClientTimings.ClientTiming>().ToList();

                if (profiler != null && profiler.RootTimingId.HasValue && timings.Any())
                {
                    var rootTiming = timings.SingleOrDefault(x => x.Id == profiler.RootTimingId.Value);
                    if (rootTiming != null)
                    {
                        profiler.Root = rootTiming;
                        timings.ForEach(x => x.Profiler = profiler);
                        timings.Remove(rootTiming);
                        var timingsLookupByParent = timings.ToLookup(x => x.ParentTimingId, x => x);
                        PopulateChildTimings(rootTiming, timingsLookupByParent);
                    }
                    if (clientTimings.Any() || profiler.ClientTimingsRedirectCount.HasValue)
                    {
                        profiler.ClientTimings = new ClientTimings
                        {
                            RedirectCount = profiler.ClientTimingsRedirectCount ?? 0, 
                            Timings = clientTimings
                        };
                    }
                }
            }
            return profiler;
        }

        /// <summary>
        /// Build the subtree of <see cref="Timing"/> objects with <paramref name="parent"/> at the top.
        /// Used recursively.
        /// </summary>
        /// <param name="parent">Parent <see cref="Timing"/> to be evaluated.</param>
        /// <param name="timingsLookupByParent">Key: parent timing Id; Value: collection of all <see cref="Timing"/> objects under the given parent.</param>
        private void PopulateChildTimings(Timing parent, ILookup<Guid, Timing> timingsLookupByParent)
        {
            if (timingsLookupByParent.Contains(parent.Id))
            {
                foreach (var timing in timingsLookupByParent[parent.Id].OrderBy(x => x.StartMilliseconds))
                {
                    parent.AddChild(timing);
                    PopulateChildTimings(timing, timingsLookupByParent);
                }
            }
        }

        /// <summary>
        /// Returns a connection to Sql Server.
        /// </summary>
        protected virtual DbConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        /// <summary>
        /// Returns a DbConnection already opened for execution.
        /// </summary>
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
        /// Works in SQL server and <c>sqlite</c> (with documented removals).
        /// </remarks>
        [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:ElementsMustBeOrderedByAccess", Justification = "Reviewed. Suppression is OK here.")]
        public static readonly string TableCreationScript =
                @"
                create table MiniProfilers
                  (
                     RowId                                integer not null identity constraint PK_MiniProfilers primary key clustered, -- Need a clustered primary key for SQL Azure
                     Id                                   uniqueidentifier not null, -- don't cluster on a guid
                     RootTimingId                         uniqueidentifier null,
                     Started                              datetime not null,
                     DurationMilliseconds                 decimal(7, 1) not null,
                     [User]                               nvarchar(100) null,
                     HasUserViewed                        bit not null,
                     MachineName                          nvarchar(100) null,
                     CustomLinksJson                      nvarchar(max),
                     ClientTimingsRedirectCount           int null
                  );
                
                -- displaying results selects everything based on the main MiniProfilers.Id column
                create unique nonclustered index IX_MiniProfilers_Id on MiniProfilers (Id)
                
                -- speeds up a query that is called on every .Stop()
                create nonclustered index IX_MiniProfilers_User_HasUserViewed_Includes on MiniProfilers ([User], HasUserViewed) include (Id, [Started])   

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
