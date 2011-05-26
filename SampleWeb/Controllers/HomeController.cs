using System;
using System.Web.Mvc;
using Profiling;
using System.Threading;
using Dapper;
using System.Data.Common;
namespace SampleWeb.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            var profiler = MiniProfiler.Current;

            using (profiler.Step("Set page title"))
            {
                ViewBag.Title = "Home Page";
            }

            using (profiler.Step("Doing complex stuff"))
            {
                using (profiler.Step("Step A"))
                {
                    Thread.Sleep(100);
                }
                using (profiler.Step("Step B"))
                {
                    Thread.Sleep(250);
                }
            }

            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult FetchRouteHits()
        {
            var profiler = MiniProfiler.Current;

            using (profiler.Step("Do more complex stuff"))
            {
                Thread.Sleep(new Random().Next(100, 400));
            }

            using (profiler.Step("FetchRouteHits"))
            using (var conn = GetOpenConnection(profiler))
            {
                var result = conn.Query<RouteHit>("select RouteName, HitCount from RouteHits order by RouteName");
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult MassiveNesting()
        {
            var i = 0;
            using (var conn = GetOpenConnection())
            {
                RecursiveMethod(ref i, conn, MiniProfiler.Current);
            }
            return Content("MassiveNesting completed");
        }

        public ActionResult MassiveNesting2()
        {
            for (int i = 0; i < 6; i++)
            {
                MassiveNesting();
            }
            return Content("MassiveNesting2 completed");
        }

        private void RecursiveMethod(ref int i, DbConnection conn, MiniProfiler profiler)
        {
            Thread.Sleep(5); // ensure we show up in the profiler

            if (i >= 10) return;

            using (profiler.Step("Nested call " + i))
            {
                // let's run a few queries, too - the formatted ones can have their formatting restored by clicking on them in the results
                conn.Query("select * from RouteHits");

                conn.Query(
@"select RouteName,
       HitCount
from   RouteHits
where  HitCount > 10
order  by RouteName");

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
        where  HitCount between 20 and 30
        union all
        select RouteName,
               HitCount
        from   RouteHits
        where  HitCount > 30)
order  by RouteName");

                using (profiler.Step("Increment i"))
                {
                    i++;
                }
                RecursiveMethod(ref i, conn, profiler);
            }
        }

        public class RouteHit
        {
            public string RouteName { get; set; }
            public Int64 HitCount { get; set; }
        }
    }
}
