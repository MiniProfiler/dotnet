using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MvcMiniProfiler;
using Dapper;
using SampleWeb.Controllers;

namespace SampleWeb.Helpers
{
    public class SqliteMiniProfilerStorage : MvcMiniProfiler.Storage.IStorage
    {

        public void SaveMiniProfiler(Guid id, MiniProfiler profiler)
        {
            using (var conn = BaseController.GetOpenConnection())
            {
                // we use the insert to ignore syntax here, because MiniProfiler will call this method each time the full results are displayed
                conn.Execute("insert or ignore into MiniProfilerResults (Id, Results) values (@id, @results)", new { id = id, results = MiniProfiler.ToJson(profiler) });
            }
        }

        public MiniProfiler LoadMiniProfiler(Guid id)
        {
            using (var conn = BaseController.GetOpenConnection())
            {
                string json = conn.Query<string>("select Results from MiniProfilerResults where Id = @id", new { id = id }).SingleOrDefault();
                return MiniProfiler.FromJson(json);
            }
        }

    }
}