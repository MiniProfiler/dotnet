using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using Dapper;
using SampleWeb.EFCodeFirst;
using StackExchange.Profiling;

namespace SampleWeb.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult EnableProfilingUI()
        {
            SampleWeb.MvcApplication.DisableProfilingResults = false;
            return Redirect("/");
        }

        public ActionResult DisableProfilingUI() 
        {
            SampleWeb.MvcApplication.DisableProfilingResults = true;
            return Redirect("/");
        }

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
            // prevent this specific route from being profiled
            MiniProfiler.Stop(discardResults: true);

            return View();
        }

        public ActionResult ResultsAuthorization()
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
            using (var conn = GetConnection(profiler))
            {
                var result = conn.Query<RouteHit>("select RouteName, HitCount from RouteHits order by RouteName");
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult XHTML()
        {
            return View();
        }

        public ActionResult EFCodeFirst()
        {
            int count;

            EFContext context = null;
            using (MiniProfiler.Current.Step("EF Stuff"))
            {
                try
                {
                    using (MiniProfiler.Current.Step("Create Context"))
                        context = new EFContext();

                    using (MiniProfiler.Current.Step("First count"))
                        count = context.People.Count();

                    using (MiniProfiler.Current.Step("Insertion"))
                    {
                        var p = new Person { Name = "sam" };
                        context.People.Add(p);
                        context.SaveChanges();
                    }

                    using (MiniProfiler.Current.Step("Second count"))
                        count = context.People.Count();
                }
                finally
                {
                    if (context != null)
                    {
                        context.Dispose();
                    }
                }
            }

            return Content("EF Code First complete - count: " + count);
        }

        public ActionResult DuplicatedQueries()
        {
            using (var conn = GetConnection())
            {
                long total = 0;

                for (int i = 0; i < 20; i++)
                {
                    total += conn.Query<long>("select count(1) from RouteHits where HitCount = @i", new { i }).First();
                }
                return Content("Duplicated Queries (N+1) completed");
            }
        }

        public ActionResult MassiveNesting()
        {
            var i = 0;
            using (var conn = GetConnection())
            {
                RecursiveMethod(ref i, conn, MiniProfiler.Current);
            }
            return Content("Massive Nesting completed");
        }

        public ActionResult MassiveNesting2()
        {
            for (int i = 0; i < 6; i++)
            {
                MassiveNesting();
            }
            return Content("Massive Nesting 2 completed");
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

        public class RouteHit
        {
            public string RouteName { get; set; }
            public Int64 HitCount { get; set; }
        }

        public ActionResult ParameterizedSqlWithEnums()
        {
            using (var conn = GetConnection())
            {
                var shouldBeOne = conn.Query<Int64>("select @OK = 200", new { System.Net.HttpStatusCode.OK }).Single();
                return Content("Parameterized SQL with Enums completed: " + shouldBeOne);
            }
        }
    }
}
