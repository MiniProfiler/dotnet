namespace StackExchange.Profiling.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using StackExchange.Profiling.Helpers;
    using StackExchange.Profiling.Helpers.Dapper;

    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a MSSQL database.
    /// </summary>
    public class SqlServerStorage : DatabaseStorageBase
    {
        /// <summary>
        /// load the SQL statements.
        /// </summary>
        private static readonly Dictionary<Type, string> LoadSqlStatements = new Dictionary<Type, string>
             {
                 { typeof(MiniProfiler), "select * from MiniProfilers where Id = @id" },
                 { typeof(Timing), "select * from MiniProfilerTimings where MiniProfilerId = @id order by RowId" },
                 { typeof(SqlTiming), "select * from MiniProfilerSqlTimings where MiniProfilerId = @id order by RowId" },
                 { typeof(SqlTimingParameter), "select * from MiniProfilerSqlTimingParameters where MiniProfilerId = @id" },
                 { typeof(ClientTimings.ClientTiming), "select * from MiniProfilerClientTimings where MiniProfilerId = @id" }
             };

        /// <summary>
        /// load the SQL batch.
        /// </summary>
        private static readonly string LoadSqlBatch = string.Join("\n", LoadSqlStatements.Select(pair => pair.Value));

        /// <summary>
        /// Initialises a new instance of the <see cref="SqlServerStorage"/> class. 
        /// Initializes a new instance of the <see cref="SqlServerStorage"/> class. 
        /// </summary>
        /// <param name="connectionString">
        /// The connection String.
        /// </param>
        public SqlServerStorage(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        /// Gets a value indicating whether or not to enable batch selects.
        /// A full install of sQL Server can return multiple result sets in one query, allowing the use of <see cref="SqlMapper.QueryMultiple"/>.
        /// However, sQL Server CE and <c>Sqlite</c> cannot do this, so inheritors for those providers can return false here.
        /// </summary>
        public virtual bool EnableBatchSelects
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Stores to <c>dbo.MiniProfilers</c> under its <see cref="MiniProfiler.Id"/>; 
        /// stores all child Timings and <c>SqlTimings</c> to their respective tables.
        /// </summary>
        /// <param name="profiler">the mini profiler</param>
        public override void Save(MiniProfiler profiler)
        {
            const string Sql =
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
                var insertCount = conn.Execute(
                    Sql,
                    new
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
        /// sets the session to un-viewed 
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="id">The id.</param>
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
        /// <param name="user">The user.</param>
        /// <param name="id">The id.</param>
        public override void SetViewed(string user, Guid id)
        {
            using (var conn = GetOpenConnection())
            {
                conn.Execute("update MiniProfilers set HasUserViewed = 1 where Id = @id", new { id });
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
        /// list the results.
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
        /// save the client timing.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="profiler">The profiler.</param>
        protected virtual void SaveClientTiming(DbConnection connection, MiniProfiler profiler)
        {
            if (profiler.ClientTimings == null || profiler.ClientTimings.Timings == null || profiler.ClientTimings.Timings.Count == 0) 
                return;

            connection.Execute(
                "insert into MiniProfilerClientTimings(MiniProfilerId,Name,Start,Duration) values (@Id, @Name, @Start, @Duration)",
                profiler.ClientTimings.Timings.Select(t => new { profiler.Id, t.Name, t.Start, t.Duration }));
        }

        /// <summary>
        /// Saves parameter Timing to the <c>dbo.MiniProfilerTimings</c> table.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="profiler">The profiler.</param>
        /// <param name="timing">The timing detail.</param>
        protected virtual void SaveTiming(DbConnection connection, MiniProfiler profiler, Timing timing)
        {
            const string Sql =
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

            connection.Execute(
                Sql,
                new
                    {
                        Id = timing.Id,
                        MiniProfilerId = profiler.Id,
                        ParentTimingId = timing.IsRoot ? (Guid?)null : timing.ParentTiming.Id,
                        Name = timing.Name.Truncate(200),
                        Depth = timing.Depth,
                        StartMilliseconds = timing.StartMilliseconds,
                        DurationMilliseconds = timing.DurationMilliseconds,
                        DurationWithoutChildrenMilliseconds = timing.DurationWithoutChildrenMilliseconds,
                        SqlTimingsDurationMilliseconds = timing.SqlTimingsDurationMilliseconds,
                        IsRoot = timing.IsRoot,
                        HasChildren = timing.HasChildren,
                        IsTrivial = timing.IsTrivial,
                        HasSqlTimings = timing.HasSqlTimings,
                        HasDuplicateSqlTimings = timing.HasDuplicateSqlTimings,
                        ExecutedReaders = timing.ExecutedReaders,
                        ExecutedScalars = timing.ExecutedScalars,
                        ExecutedNonQueries = timing.ExecutedNonQueries
                    });

            if (timing.HasSqlTimings)
            {
                foreach (var st in timing.SqlTimings)
                {
                    SaveSqlTiming(connection, profiler, st);
                }
            }

            if (timing.HasChildren)
            {
                foreach (var child in timing.Children)
                {
                    SaveTiming(connection, profiler, child);
                }
            }
        }

        /// <summary>
        /// Saves parameter <c>sqlTiming</c> to the <c>dbo.MiniProfilerSqlTimings</c> table.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="profiler">The profiler.</param>
        /// <param name="sqlTiming">The SQL Timing.</param>
        protected virtual void SaveSqlTiming(DbConnection connection, MiniProfiler profiler, SqlTiming sqlTiming)
        {
            const string Sql =
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

            connection.Execute(
                Sql,
                new
                    {
                        Id = sqlTiming.Id,
                        MiniProfilerId = profiler.Id,
                        ParentTimingId = sqlTiming.ParentTiming.Id,
                        ExecuteType = sqlTiming.ExecuteType,
                        StartMilliseconds = sqlTiming.StartMilliseconds,
                        DurationMilliseconds = sqlTiming.DurationMilliseconds,
                        FirstFetchDurationMilliseconds = sqlTiming.FirstFetchDurationMilliseconds,
                        IsDuplicate = sqlTiming.IsDuplicate,
                        StackTraceSnippet = sqlTiming.StackTraceSnippet.Truncate(200),
                        CommandString = sqlTiming.CommandString
                    });

            if (sqlTiming.Parameters != null && sqlTiming.Parameters.Count > 0)
            {
                SaveSqlTimingParameters(connection, profiler, sqlTiming);
            }
        }

        /// <summary>
        /// Saves any <c>SqlTimingParameters</c> used in the profiled <c>SqlTiming</c> to the <c>dbo.MiniProfilerSqlTimingParameters</c> table.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="profiler">The profiler.</param>
        /// <param name="sqlTiming">The SQL Timing.</param>
        protected virtual void SaveSqlTimingParameters(DbConnection connection, MiniProfiler profiler, SqlTiming sqlTiming)
        {
            const string Sql =
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

            foreach (var p in sqlTiming.Parameters)
            {
                connection.Execute(
                    Sql,
                    new
                        {
                            MiniProfilerId = profiler.Id,
                            ParentSqlTimingId = sqlTiming.Id,
                            Name = p.Name.Truncate(130),
                            DbType = p.DbType.Truncate(50),
                            p.Size,
                            p.Value
                        });
            }
        }
        
        /// <summary>
        /// load the profiler in a batch.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="keyParameter">The id parameter.</param>
        /// <returns>the mini profiler.</returns>
        private MiniProfiler LoadInBatch(DbConnection connection, object keyParameter)
        {
            MiniProfiler result;

            using (var multi = connection.QueryMultiple(LoadSqlBatch, keyParameter))
            {
                result = multi.Read<MiniProfiler>().SingleOrDefault();

                if (result != null)
                {
                    var timings = multi.Read<Timing>().ToList();
                    var sqlTimings = multi.Read<SqlTiming>().ToList();
                    var sqlParameters = multi.Read<SqlTimingParameter>().ToList();
                    var clientTimingList = multi.Read<ClientTimings.ClientTiming>().ToList();
                    ClientTimings clientTimings = null;
                    if (clientTimingList.Count > 0)
                    {
                        clientTimings = new ClientTimings { Timings = clientTimingList };
                    }

                    MapTimings(result, timings, sqlTimings, sqlParameters, clientTimings);
                }
            }

            return result;
        }

        /// <summary>
        /// load individually.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="keyParameter">The id Parameter.</param>
        /// <returns>the mini profiler.</returns>
        private MiniProfiler LoadIndividually(DbConnection connection, object keyParameter)
        {
            var result = LoadFor<MiniProfiler>(connection, keyParameter).SingleOrDefault();

            if (result != null)
            {
                var timings = LoadFor<Timing>(connection, keyParameter);
                var sqlTimings = LoadFor<SqlTiming>(connection, keyParameter);
                var sqlParameters = LoadFor<SqlTimingParameter>(connection, keyParameter);
                var clientTimingList = LoadFor<ClientTimings.ClientTiming>(connection, keyParameter).ToList();
                ClientTimings clientTimings = null;
                if (clientTimingList.Count > 0)
                {
                    clientTimings = new ClientTimings { Timings = clientTimingList };
                }

                MapTimings(result, timings, sqlTimings, sqlParameters, clientTimings);
            }

            return result;
        }

        /// <summary>
        /// load statements for the supplied key from the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="keyParameter">The id parameter.</param>
        /// <typeparam name="T">the type of timing (instance) to load</typeparam>
        /// <returns>the list of loaded timing instances.</returns>
        private List<T> LoadFor<T>(DbConnection connection, object keyParameter)
        {
            return connection.Query<T>(LoadSqlStatements[typeof(T)], keyParameter).ToList();
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
        /// TODO: add indexes
        /// </remarks>
        [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:ElementsMustBeOrderedByAccess", Justification = "Reviewed. Suppression is OK here.")]
        public const string TableCreationScript =
                @"
                create table MiniProfilers
                  (
                     RowId                                integer not null identity constraint PK_MiniProfilers primary key clustered, -- Need a clustered primary key for SQL Azure
                     Id                                   uniqueidentifier not null, -- don't cluster on a guid
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
                
                -- RowIds here are used to enforce an ordering and storage locality - really, the only id that matters for our querying is the MiniProfilerId
                
                create table MiniProfilerTimings
                  (
                     RowId                               integer not null identity constraint PK_MiniProfilerTimings primary key clustered,
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
                     RowId                          integer not null identity constraint PK_MiniProfilerSqlTimings primary key clustered,
                     Id                             uniqueidentifier not null,
                     MiniProfilerId                 uniqueidentifier not null,
                     ParentTimingId                 uniqueidentifier not null,
                     ExecuteType                    tinyint not null,
                     StartMilliseconds              decimal(7, 1) not null,
                     DurationMilliseconds           decimal(7, 1) not null,
                     FirstFetchDurationMilliseconds decimal(7, 1) null,
                     IsDuplicate                    bit not null,
                     StackTraceSnippet              nvarchar(200) not null,
                     CommandString                  nvarchar(max) not null
                  );
                
                create table MiniProfilerSqlTimingParameters
                  (
                	 RowId             integer not null identity constraint PK_MiniProfilerSqlTimingParameters primary key clustered,
                     MiniProfilerId    uniqueidentifier not null,
                     ParentSqlTimingId uniqueidentifier not null,
                     Name              nvarchar(130) not null,
                     DbType            nvarchar(50) null,
                     Size              int null,
                     Value             nvarchar(max) null
                  );
                
                create table MiniProfilerClientTimings
                (
                  RowId             integer not null identity constraint PK_MiniProfilerClientTimings primary key clustered,
                  MiniProfilerId    uniqueidentifier not null,
                  Name				nvarchar(200) not null,
                  Start				decimal(7,1),
                  Duration			decimal(7,1)    
                );
                
                -- displaying results selects everything based on the main MiniProfilers.Id column
                create unique nonclustered index IX_MiniProfilers_Id on MiniProfilers (Id)
                create nonclustered index IX_MiniProfilerTimings_MiniProfilerId on MiniProfilerTimings (MiniProfilerId)
                create nonclustered index IX_MiniProfilerSqlTimings_MiniProfilerId on MiniProfilerSqlTimings (MiniProfilerId)
                create nonclustered index IX_MiniProfilerSqlTimingParameters_MiniProfilerId on MiniProfilerSqlTimingParameters (MiniProfilerId)
                create nonclustered index IX_MiniProfilerClientTimings_MiniProfilerId on MiniProfilerClientTimings (MiniProfilerId)
                
                -- speeds up a query that is called on every .Stop()
                create nonclustered index IX_MiniProfilers_User_HasUserViewed_Includes on MiniProfilers ([User], HasUserViewed) include (Id, [Started])
                
                ";
    }
}
