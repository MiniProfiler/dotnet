using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MvcMiniProfiler;
using Dapper;
using SampleWeb.Controllers;

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

        public override MiniProfiler LoadMiniProfiler(Guid id)
        {
            // sqlite can't execute multiple result sets at once, so we need to override and run three queries
            MiniProfiler result = null;

            using (var conn = GetOpenConnection())
            {
                var param = new { id = id };
                result = conn.Query<MiniProfiler>("select * from MiniProfilerS where Id = @id", param).SingleOrDefault();

                if (result != null)
                {
                    // HACK: stored dates are utc, but are pulled out as local time - sqlite doesn't have dedicated datetime types, though
                    result.Started = new DateTime(result.Started.Ticks, DateTimeKind.Utc);

                    var timings = conn.Query<Timing>("select * from MiniProfilerTimings where MiniProfilerId = @id order by RowId", param).ToList();
                    var sqlTimings = conn.Query<SqlTiming>("select * from MiniProfilerSqlTimings where MiniProfilerId = @id order by RowId", param).ToList();
                    var sqlParameters = conn.Query<SqlTimingParameter>("select * from MiniProfilerSqlTimingParameters where MiniProfilerId = @id", param).ToList();
                    MapTimings(result, timings, sqlTimings, sqlParameters);
                }
            }

            return result;
        }
    }
}