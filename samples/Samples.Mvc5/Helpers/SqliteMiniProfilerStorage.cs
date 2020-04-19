﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dapper;
using StackExchange.Profiling.Storage;
using System.Data.SQLite;

namespace Samples.Mvc5.Helpers
{
    /// <summary>
    /// The SQLITE mini profiler storage.
    /// </summary>
    public class SqliteMiniProfilerStorage : SqlServerStorage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteMiniProfilerStorage"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public SqliteMiniProfilerStorage(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Get the Connection.
        /// </summary>
        /// <returns>The Abstracted Connection</returns>
        protected override System.Data.Common.DbConnection GetConnection() =>
            new SQLiteConnection(ConnectionString);

        /// <summary>
        /// Used for testing purposes - destroys and recreates the SQLITE file with needed tables.
        /// </summary>
        /// <param name="extraTablesToCreate">The Extra Tables To Create.</param>
        public SqliteMiniProfilerStorage RecreateDatabase(params string[] extraTablesToCreate)
        {
            using (var cnn = GetConnection())
            {
                // We need some tiny mods to allow SQLite support 
                foreach (var sql in TableCreationScripts.Union(extraTablesToCreate))
                {
                    cnn.Execute(sql);
                }
            }
            return this;
        }

        /// <summary>
        /// The list of results.
        /// </summary>
        /// <param name="maxResults">The max results.</param>
        /// <param name="start">The start</param>
        /// <param name="finish">The finish</param>
        /// <param name="orderBy">The order by.</param>
        /// <returns>The result set</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "This isn't C# 8, silly IDE.")]
        public override IEnumerable<Guid> List(
            int maxResults,
            DateTime? start = null,
            DateTime? finish = null,
            ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            var sb = new StringBuilder(@"
Select Id
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
            sb.Append("Order By ").AppendLine(orderBy == ListResultsOrder.Descending ? "Started Desc" : "Started Asc");
            sb.Append("LIMIT(").Append(maxResults).AppendLine(")");

            using (var conn = GetConnection())
            {
                return conn.Query<Guid>(sb.ToString(), new { start, finish }).ToList();
            }
        }

        protected override IEnumerable<string> GetTableCreationScripts()
        {
            yield return @"CREATE TABLE MiniProfilers
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
            yield return @"CREATE TABLE MiniProfilerTimings
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
            yield return @"CREATE TABLE MiniProfilerClientTimings
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
