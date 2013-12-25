using StackExchange.Profiling.Helpers.Dapper;

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
        /// Get the Connection.
        /// </summary>
        /// <returns>The Abstracted Connection</returns>
        protected override System.Data.Common.DbConnection GetConnection()
        {
            return new System.Data.SQLite.SQLiteConnection(ConnectionString);
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
                foreach (var sql in new List<string>{TableCreationScript}.Union(extraTablesToCreate))
                {
                    cnn.Execute(sql);
                }
            }
        }

        /// <summary>
        /// The list of results.
        /// </summary>
        /// <param name="maxResults">The max results.</param>
        /// <param name="start">The start</param>
        /// <param name="finish">The finish</param>
        /// <param name="orderBy">The order by.</param>
        /// <returns>The result set</returns>
        public override IEnumerable<Guid> List(
            int maxResults,
            DateTime? start = null,
            DateTime? finish = null,
            ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            var builder = new SqlBuilder();
            var t = builder.AddTemplate("select Id from MiniProfilers /**where**/ /**orderby**/ LIMIT(" + maxResults + ")");

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

        private new const string TableCreationScript = @"
                CREATE TABLE MiniProfilers
                  (
                     RowId                                integer not null primary key,
                     Id                                   uniqueidentifier not null, 
                     Started                              datetime not null,
                     DurationMilliseconds                 decimal(7, 1) not null,
                     User                                 nvarchar(100) null,
                     HasUserViewed                        bit not null,
                     Json                                 text
                  );";
    }
}