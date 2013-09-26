using SampleWeb.EfModelFirst;

namespace SampleWeb.Controllers
{
    using System;
    using System.Data.Common;
    using System.Linq;
    using System.Threading;
    using System.Web.Mvc;

    using Dapper;

    using SampleWeb.EFCodeFirst;

    using StackExchange.Profiling;

    /// <summary>
    /// The home controller.
    /// </summary>
    public class HomeController : BaseController
    {
        /// <summary>
        /// enable the profiling UI.
        /// </summary>
        /// <returns>enable profiling the UI</returns>
        public ActionResult EnableProfilingUI()
        {
            MvcApplication.DisableProfilingResults = false;
            return Redirect("/");
        }

        /// <summary>
        /// disable the profiling UI.
        /// </summary>
        /// <returns>disable profiling the UI</returns>
        public ActionResult DisableProfilingUI() 
        {
            MvcApplication.DisableProfilingResults = true;
            return Redirect("/");
        }

        /// <summary>
        /// the default view, home page.
        /// </summary>
        /// <returns>the home page view.</returns>
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

            // simulate fetching a url
            using (profiler.CustomTiming("http", "GET http://google.com"))
            {
                Thread.Sleep(50);
            }

            // now something that loops
            for (int i = 0; i < 15; i++)
            {
                using (profiler.CustomTiming("redis", "SET \"mykey\" 10"))
                {
                    Thread.Sleep(i);
                }
            }

            return View();
        }

        /// <summary>
        /// about view.
        /// </summary>
        /// <returns>the about view (default)</returns>
        /// <remarks>this view is not profiled.</remarks>
        public ActionResult About()
        {
            // prevent this specific route from being profiled
            MiniProfiler.Stop(discardResults: true);

            return View();
        }

        /// <summary>
        /// results authorization.
        /// </summary>
        /// <returns>The <see cref="ActionResult"/>.</returns>
        public ActionResult ResultsAuthorization()
        {
            return View();
        }

        /// <summary>
        /// fetch the route hits.
        /// </summary>
        /// <returns>the view of route hits.</returns>
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

        /// <summary>
        /// The XHTML view.
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        public ActionResult Xhtml()
        {
            return View();
        }

        public ActionResult EfModelFirst()
        {
            int count;

            SampleEfModelFirstEntities context = null;
            using (MiniProfiler.Current.Step("EF Model First Stuff"))
            {
                try
                {
                    using (MiniProfiler.Current.Step("Create Context"))
                        context = new SampleEfModelFirstEntities(); 

                    // this is not correct, as the count from this assignment is never actually used
                    using (MiniProfiler.Current.Step("First count"))
                        count = context.ModelPersons.Count();

                    using (MiniProfiler.Current.Step("Insertion"))
                    {
                        var p = new ModelPerson { Name = "sam" };
                        context.ModelPersons.Add(p);
                        context.SaveChanges();
                    }

                    // this count is actually used.
                    using (MiniProfiler.Current.Step("Second count"))
                        count = context.ModelPersons.Count();
                }
                finally
                {
                    if (context != null)
                    {
                        context.Dispose();
                    }
                }
            }

            return Content("EF Model First complete - count: " + count);
        }

        /// <summary>
        /// The EF code first.
        /// </summary>
        /// <returns>the entity framework code first view.</returns>
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

                    // this is not correct, as the count from this assignment is never actually used
                    using (MiniProfiler.Current.Step("First count"))
                        count = context.People.Count();

                    using (MiniProfiler.Current.Step("Insertion"))
                    {
                        var p = new Person { Name = "sam" };
                        context.People.Add(p);
                        context.SaveChanges();
                    }

                    // this count is actually used.
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

        /// <summary>
        /// duplicated queries.
        /// </summary>
        /// <returns>duplicated query demonstration</returns>
        public ActionResult DuplicatedQueries()
        {
            using (var conn = GetConnection())
            {
                long total = 0;

                for (int i = 0; i < 20; i++)
                {
                    total += conn.Query<long>("select count(1) from RouteHits where HitCount = @i", new { i }).First();
                }
                return Content(string.Format("Duplicated Queries (N+1) completed {0}", total));
            }
        }

        /// <summary>
        /// test a massive nesting.
        /// </summary>
        /// <returns>the result view of the massive nesting.</returns>
        public ActionResult MassiveNesting()
        {
            var i = 0;
            using (var conn = GetConnection())
            {
                RecursiveMethod(ref i, conn, MiniProfiler.Current);
            }
            return Content("Massive Nesting completed");
        }

        /// <summary>
        /// The second massive nesting.
        /// </summary>
        /// <returns>the second massive nesting view</returns>
        public ActionResult MassiveNesting2()
        {
            for (int i = 0; i < 6; i++)
            {
                MassiveNesting();
            }
            return Content("Massive Nesting 2 completed");
        }

        /// <summary>
        /// demonstrate a recursive method.
        /// </summary>
        /// <param name="depth">recursion depth</param>
        /// <param name="connection">the connection</param>
        /// <param name="profiler">The profiler.</param>
        private void RecursiveMethod(ref int depth, DbConnection connection, MiniProfiler profiler)
        {
            Thread.Sleep(5); // ensure we show up in the profiler

            if (depth >= 10) return;

            using (profiler.Step("Nested call " + depth))
            {
                // run some meaningless queries to illustrate formatting
                connection.Query(
                    @"select *
                    from   MiniProfilers
                    where  Name like @name
                            or Name = @name
                            or DurationMilliseconds >= @duration
                            or HasSqlTimings = @hasSqlTimings
                            or Started > @yesterday ",
                    new
                        {
                            name = "Home/Index",
                                     duration = 100.5,
                                     hasSqlTimings = true,
                                     yesterday = DateTime.UtcNow.AddDays(-1)
                                 });

                connection.Query(@"select RouteName, HitCount from RouteHits where HitCount < 100000000 or HitCount > 0 order by HitCount, RouteName -- this should hopefully wrap");

                // massive query to test if max-height is properly removed from <pre> stylings
                connection.Query(
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
                
                // need a long title to test max-width
                using (profiler.Step("Incrementing a reference parameter named i"))
                {
                    depth++;
                }
                RecursiveMethod(ref depth, connection, profiler);
            }
        }

        /// <summary>
        /// route hit.
        /// </summary>
        public class RouteHit
        {
            /// <summary>
            /// Gets or sets the route name.
            /// </summary>
            public string RouteName { get; set; }

            /// <summary>
            /// Gets or sets the hit count.
            /// </summary>
            public long HitCount { get; set; }
        }

        /// <summary>
        /// The parameterized SQL with enumerations.
        /// </summary>
        /// <returns>The <see cref="ActionResult"/>.</returns>
        public ActionResult ParameterizedSqlWithEnums()
        {
            using (var conn = GetConnection())
            {
                var shouldBeOne = conn.Query<long>("select @OK = 200", new { System.Net.HttpStatusCode.OK }).Single();
                return Content("Parameterized SQL with Enums completed: " + shouldBeOne);
            }
        }
    }
}
