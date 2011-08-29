using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using MvcMiniProfiler.Wcf;
using System.Data.Common;
using MvcMiniProfiler;
using System.Threading;
using Dapper;

namespace Sample.Wcf
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    public class SampleService : ISampleService
    {


        /// <summary>
        /// Returns an open connection that will have its queries profiled.
        /// </summary>
        public static DbConnection GetConnection(MiniProfiler profiler = null)
        {
            using (profiler.Step("GetOpenConnection"))
            {
                DbConnection cnn = new System.Data.SQLite.SQLiteConnection(WcfCommon.ConnectionString);

                // to get profiling times, we have to wrap whatever connection we're using in a ProfiledDbConnection
                // when MiniProfiler.Current is null, this connection will not record any database timings
                if (MiniProfiler.Current != null)
                {
                    cnn = new MvcMiniProfiler.Data.ProfiledDbConnection(cnn, MiniProfiler.Current);
                }

                cnn.Open();
                return cnn;
            }
        }

        public IEnumerable<RouteHit> FetchRouteHits()
        {
            var profiler = MiniProfiler.Current;
            using (profiler.Step("Do more complex stuff"))
            {
                Thread.Sleep(new Random().Next(100, 400));
            }

            using (profiler.Step("FetchRouteHits"))
            using (var conn = GetConnection(profiler))
            {
                var result = conn.Query<RouteHit>("select RouteName, HitCount from RouteHits order by RouteName");
                return result.ToList();
            }
        }

        public string ServiceMethodThatIsNotProfiled()
        {
            MiniProfiler.Stop(true);

            return "Result";
        }
        public string MassiveNesting()
        {
            var i = 0;
            using (var conn = GetConnection())
            {
                RecursiveMethod(ref i, conn, MiniProfiler.Current);
            }
            return "MassiveNesting completed";
        }

        public string MassiveNesting2()
        {
            for (int i = 0; i < 6; i++)
            {
                MassiveNesting();
            }
            return "MassiveNesting2 completed";
        }

        public string Duplicated()
        {
            using (var conn = GetConnection())
            {
                long total = 0;

                for (int i = 0; i < 20; i++)
                {
                    total += conn.Query<long>("select count(1) from RouteHits where HitCount = @i", new { i }).First();
                }
                return "Duplicate queries completed";
            }
        }

        private void RecursiveMethod(ref int i, DbConnection conn, MiniProfiler profiler)
        {
            Thread.Sleep(5); // ensure we show up in the profiler

            if (i >= 10) return;

            using (profiler.Step("Nested call " + i))
            {
                // run some meaningless queries to illustrate formatting
                conn.Query(
@"select *
from   MiniProfilers
where  Name like @name
        or Name = @name
        or DurationMilliseconds >= @duration
        or HasSqlTimings = @hasSqlTimings
        or Started > @yesterday ", new
                                 {
                                     name = "Home/Index",
                                     duration = 100.5,
                                     hasSqlTimings = true,
                                     yesterday = DateTime.UtcNow.AddDays(-1)
                                 });

                conn.Query(@"select RouteName, HitCount from RouteHits where HitCount < 100000000 or HitCount > 0 order by HitCount, RouteName -- this should hopefully wrap");

                // massive query to test if max-height is properly removed from <pre> stylings
                conn.Query(
@"select *
from   (select RouteName,
               HitCount
        from   RouteHits
        where  HitCount between 0 and 9
        union all
        select RouteName,
               HitCount
        from   RouteHits
        where  HitCount between 10 and 19
        union all
        select RouteName,
               HitCount
        from   RouteHits
        where  HitCount between 20 and 29
        union all
        select RouteName,
               HitCount
        from   RouteHits
        where  HitCount between 30 and 39
        union all
        select RouteName,
               HitCount
        from   RouteHits
        where  HitCount between 40 and 49
        union all
        select RouteName,
               HitCount
        from   RouteHits
        where  HitCount between 50 and 59
        union all
        select RouteName,
               HitCount
        from   RouteHits
        where  HitCount between 60 and 69
        union all
        select RouteName,
               HitCount
        from   RouteHits
        where  HitCount between 70 and 79
        union all
        select RouteName,
               HitCount
        from   RouteHits
        where  HitCount between 80 and 89
        union all
        select RouteName,
               HitCount
        from   RouteHits
        where  HitCount between 90 and 99
        union all
        select RouteName,
               HitCount
        from   RouteHits
        where  HitCount > 100)
order  by RouteName");

                using (profiler.Step("Incrementing a reference parameter named i")) // need a long title to test max-width
                {
                    i++;
                }
                RecursiveMethod(ref i, conn, profiler);
            }
        }
    }
}
