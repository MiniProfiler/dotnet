namespace SampleWeb.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Dapper;

    using StackExchange.Profiling.Storage;

    /// <summary>
    /// The SQLITE mini profiler storage.
    /// </summary>
    public class SqliteMiniProfilerStorage : SqlServerStorage
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="SqliteMiniProfilerStorage"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public SqliteMiniProfilerStorage(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        /// get the connection.
        /// </summary>
        /// <returns>the abstracted connection</returns>
        protected override System.Data.Common.DbConnection GetConnection()
        {
            return new System.Data.SQLite.SQLiteConnection(ConnectionString);
        }

        /// <summary>
        /// sQLITE doesn't support multiple result sets in one query.
        /// </summary>
        public override bool EnableBatchSelects
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Used for testing purposes - destroys and recreates the SQLITE file with needed tables.
        /// </summary>
        /// <param name="extraTablesToCreate">The Extra Tables To Create.</param>
        public void RecreateDatabase(params string[] extraTablesToCreate)
        {
            var path = ConnectionString.Replace("Data Source = ", string.Empty); // hacky

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (var cnn = new System.Data.SQLite.SQLiteConnection(MvcApplication.ConnectionString))
            {
                cnn.Open();

                // we need some tiny mods to allow sqlite support 
                foreach (var sql in TableCreationSQL.Union(extraTablesToCreate))
                {
                    cnn.Execute(sql);
                }
            }
        }

        /// <summary>
        /// The list of results.
        /// </summary>
        /// <param name="maxResults">The max results.</param>
        /// <param name="start">The start.</param>
        /// <param name="finish">The finish.</param>
        /// <param name="orderBy">The order by.</param>
        /// <returns>The result set</returns>
        public override IEnumerable<Guid> List(
            int maxResults,
            DateTime? start = null,
            DateTime? finish = null,
            ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            var builder = new SqlBuilder();
            var t =
                builder.AddTemplate("select Id from MiniProfilers /**where**/ /**orderby**/ LIMIT(" + maxResults + ")");

            if (start != null)
            {
                builder.Where("Started > @start", new { start });
            }
            if (finish != null)
            {
                builder.Where("Started < @finish", new { finish });
            }

            builder.OrderBy(orderBy == ListResultsOrder.Descending ? "Started desc" : "Started asc");

            using (var conn = GetOpenConnection())
            {
                return conn.Query<Guid>(t.RawSql, t.Parameters).ToList();
            }
        }

        /// <summary>
        /// MiniProfiler will serialize its profiling data to these tables.
        /// </summary>
        private static readonly string[] TableCreationSQL = new[] 
        { 
            @"create table MiniProfilers
              (
                 Id                                   uniqueidentifier not null primary key,
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
              )", @"create table MiniProfilerTimings
              (
                 RowId                               integer primary key autoincrement, -- sqlite: replace identity with autoincrement
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
              )", @"create table MiniProfilerSqlTimings
              (
                 RowId                          integer primary key autoincrement, -- sqlite: replace identity with autoincrement
                 Id                             uniqueidentifier not null,
                 MiniProfilerId                 uniqueidentifier not null,
                 ParentTimingId                 uniqueidentifier not null,
                 ExecuteType                    tinyint not null,
                 StartMilliseconds              decimal(7, 1) not null,
                 DurationMilliseconds           decimal(7, 1) not null,
                 FirstFetchDurationMilliseconds decimal(7, 1) null,
                 IsDuplicate                    bit not null,
                 StackTraceSnippet              nvarchar(200) not null,
                 CommandString                  nvarchar not null -- sqlite: remove (max)
              )", @"create table MiniProfilerSqlTimingParameters
              (
                 MiniProfilerId    uniqueidentifier not null,
                 ParentSqlTimingId uniqueidentifier not null,
                 Name              varchar(130) not null,
                 DbType            varchar(50) null,
                 Size              int null,
                 Value             nvarchar null -- sqlite: remove (max)
              )", @"
            
            create table MiniProfilerClientTimings
            (
              MiniProfilerId    uniqueidentifier not null,
              Name varchar(130) not null,
              Start decimal(7, 1) not null,
              Duration decimal(7, 1) not null    
            )
            " 
        };
    }
}