namespace StackExchange.Profiling.Storage
{
    using System;
    using System.Collections.Generic;
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
        /// Load the SQL statements.
        /// </summary>
        private const string SqlStatement = "select * from MiniProfilers where Id = @id";

        /// <summary>
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
        /// Stores to <c>dbo.MiniProfilers</c> under its <see cref="MiniProfiler.Id"/>;
        /// </summary>
        /// <param name="profiler">The Mini Profiler</param>
        public override void Save(MiniProfiler profiler)
        {
            const string Sql =
@"insert into MiniProfilers
            (Id,
             Started,
             DurationMilliseconds,
             [User],
             HasUserViewed,
             Json)
select       @Id,
             @Started,
             @DurationMilliseconds,
             @User,
             @HasUserViewed,
             @Json
where not exists (select 1 from MiniProfilers where Id = @Id)"; // this syntax works on both mssql and sqlite

            var wrapper = new DbTimingsWrapper {ClientTimings = profiler.ClientTimings, CustomLinks = profiler.CustomLinks, Root = profiler.Root};

            using (var conn = GetOpenConnection())
            {
                conn.Execute(
                    Sql,
                    new
                        {
                            profiler.Id,
                            profiler.Started,
                            User = profiler.User.Truncate(100),
                            RootTimingId = profiler.Root.Id,
                            profiler.DurationMilliseconds,
                            profiler.HasUserViewed,
                            Json = wrapper.ToJson()
                        });
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
                conn.Execute("update MiniProfilers set HasUserViewed = 0 where Id = @id AND [User] = @user", new { id, user });
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
                conn.Execute("update MiniProfilers set HasUserViewed = 1 where Id = @id AND [User] = @user", new { id, user });
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
            var result = connection.Query<MiniProfiler>(SqlStatement, keyParameter).SingleOrDefault();

            if (result != null)
            {
                if (result.Json.HasValue())
                {
                    var wrapper = result.Json.FromJson<DbTimingsWrapper>();
                    MapTimings(result, wrapper);
                }
            }

            return result;
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
                     Started                              datetime not null,
                     DurationMilliseconds                 decimal(7, 1) not null,
                     User                                 nvarchar(100) null,
                     HasUserViewed                        bit not null,
                     Json                                 nvarchar(max)
                  );
                
                -- displaying results selects everything based on the main MiniProfilers.Id column
                create unique nonclustered index IX_MiniProfilers_Id on MiniProfilers (Id)
                
                -- speeds up a query that is called on every .Stop()
                create nonclustered index IX_MiniProfilers_User_HasUserViewed_Includes on MiniProfilers ([User], HasUserViewed) include (Id, [Started])                
                ";

    }
}
