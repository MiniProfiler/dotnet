using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MvcMiniProfiler;
using Dapper;
using SampleWeb.Controllers;
using System.IO;

namespace SampleWeb.Helpers
{
    public class SqliteMiniProfilerStorage : MvcMiniProfiler.Storage.SqlServerStorage
    {
        public SqliteMiniProfilerStorage(string connectionString)
            : base(connectionString)
        {
        }

        protected override System.Data.Common.DbConnection GetConnection()
        {
            return new System.Data.SQLite.SQLiteConnection(ConnectionString);
        }

        /// <summary>
        /// Sqlite doesn't support multiple result sets in one query.
        /// </summary>
        public override bool EnableBatchSelects
        {
            get { return false; }
        }

        /// <summary>
        /// Used for testing purposes - destroys and recreates the sqlite file with needed tables.
        /// </summary>
        public void RecreateDatabase(params string[] extraTablesToCreate)
        {
            var path = ConnectionString.Replace("Data Source = ", ""); // hacky

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
  )",

@"create table MiniProfilerTimings
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
  )",

@"create table MiniProfilerSqlTimings
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
  )",

@"create table MiniProfilerSqlTimingParameters
  (
     MiniProfilerId    uniqueidentifier not null,
     ParentSqlTimingId uniqueidentifier not null,
     Name              varchar(130) not null,
     DbType            varchar(50) null,
     Size              int null,
     Value             nvarchar null -- sqlite: remove (max)
  )"
        };
    }
}