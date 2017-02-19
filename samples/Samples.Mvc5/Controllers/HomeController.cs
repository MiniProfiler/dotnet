﻿using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using Dapper;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

using Samples.Mvc5.EfModelFirst;
using Samples.Mvc5.EFCodeFirst;
using Samples.Mvc5.Helpers;

namespace Samples.Mvc5.Controllers
{
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
        /// the default view, home page, top right orientation.
        /// </summary>
        public ActionResult Index() => HomeWithPosition(RenderPosition.Right);
        /// <summary>
        /// the default view, home page, top left orientation.
        /// </summary>
        public ActionResult TopLeft() => HomeWithPosition(RenderPosition.Left);
        /// <summary>
        /// the default view, home page, bottom left orientation.
        /// </summary>
        public ActionResult BottomLeft() => HomeWithPosition(RenderPosition.BottomLeft);
        /// <summary>
        /// the default view, home page, bottom right orientation.
        /// </summary>
        public ActionResult BottomRight() => HomeWithPosition(RenderPosition.BottomRight);

        private ActionResult HomeWithPosition(RenderPosition pos)
        {
            DefaultActions();
            ViewBag.Orientation = pos;
            return View("Index");
        }

        /// <summary>
        /// Runs the default actions used on all Index views (default, and bottom left/right)
        /// </summary>
        private void DefaultActions()
        {
            var profiler = MiniProfiler.Current;

            // test out using storage for this one request. Only store in SqlLite, not in httpCache
            profiler.Storage = new SqliteMiniProfilerStorage(MvcApplication.ConnectionString);

            using (profiler.Step("Set page title"))
            {
                ViewBag.Title = "Home Page";
            }

            using (profiler.Step("Doing complex stuff"))
            {
                using (profiler.Step("Step A"))
                {
                    // simulate fetching a url
                    using (profiler.CustomTiming("http", "GET http://google.com"))
                    {
                        Thread.Sleep(10);
                    }
                }
                using (profiler.Step("Step B"))
                {
                    // simulate fetching a url
                    using (profiler.CustomTiming("http", "GET http://stackoverflow.com"))
                    {
                        Thread.Sleep(20);
                    }

                    using (profiler.CustomTiming("redis", "SET \"mykey\" 10"))
                    {
                        Thread.Sleep(5);
                    }
                }
            }

            // now something that loops
            for (int i = 0; i < 15; i++)
            {
                using (profiler.CustomTiming("redis", "SET \"mykey\" 10"))
                {
                    Thread.Sleep(i);
                }
            }

            // let's also add a custom link to stack overflow!
            profiler.AddCustomLink("stack overflow", "http://stackoverflow.com");
        }

        /// <summary>
        /// about view.
        /// </summary>
        /// <returns>the about view (default)</returns>
        /// <remarks>this view is not profiled.</remarks>
        public ActionResult About()
        {
            // prevent this specific route from being profiled
            MiniProfiler.Stop(true);

            return View();
        }

        /// <summary>
        /// results authorization.
        /// </summary>
        /// <returns>The <see cref="ActionResult"/>.</returns>
        public ActionResult ResultsAuthorization() => View();

        /// <summary>
        /// fetch the route hits.
        /// </summary>
        /// <returns>the view of route hits.</returns>
        public ActionResult FetchRouteHits()
        {
            var profiler = MiniProfiler.Current;

            using (profiler.Step("Insert Route Row"))
            using (var conn = GetConnection(profiler))
            {
                conn.Execute("INSERT INTO RouteHits (RouteName, HitCount) VALUES (@RouteName, @HitCount)", new {RouteName = Request.Url.AbsoluteUri, HitCount = new Random().Next(100, 400)});
            }

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

        public ActionResult MinSaveMs()
        {
            var profiler = MiniProfiler.Current;

            using (profiler.StepIf("Should show up", 50))
            {
                Thread.Sleep(60);
            }
            using (profiler.StepIf("Should not show up", 50))
            {
                Thread.Sleep(10);
            }

            using (profiler.StepIf("Show show up with children", 10, true))
            {
                Thread.Sleep(5);
                using (profiler.Step("Step A"))
                {
                    Thread.Sleep(10);
                }
                using (profiler.Step("Step B"))
                {
                    Thread.Sleep(10);
                }
                using (profiler.StepIf("Should not show up", 15))
                {
                    Thread.Sleep(10);
                }
            }

            using (profiler.StepIf("Show Not show up with children", 10))
            {
                Thread.Sleep(5);
                using (profiler.Step("Step A"))
                {
                    Thread.Sleep(10);
                }
                using (profiler.Step("Step B"))
                {
                    Thread.Sleep(10);
                }
            }

            using (profiler.CustomTimingIf("redis", "should show up", 5))
            {
                Thread.Sleep(10);
            }

            using (profiler.CustomTimingIf("redis", "should not show up", 15))
            {
                Thread.Sleep(10);
            }
            return Content("All good");
        }

        /// <summary>
        /// The XHTML view.
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        public ActionResult Xhtml() => View();

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
                        var p = new ModelPerson { Name = "sam", Id = new Random().Next(10000)};
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
            int? newCount = null;

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

                    const string sql = "Select count(*) from People";
                    using (MiniProfiler.Current.Step("Get Count from SqlQuery Method - no sql recorded"))
                    {
                        newCount = context.Database.SqlQuery<int>(sql).Single();
                    }
                    using (MiniProfiler.Current.Step("Get Count using ProfiledConnection - sql recorded"))
                    {
                        using (var conn = new ProfiledDbConnection(context.Database.Connection, MiniProfiler.Current))
                        {
                            conn.Open();
                            newCount = conn.Query<int>(sql).Single();
                            conn.Close();
                        }
                    }
                }
                finally
                {
                    if (context != null)
                    {
                        context.Dispose();
                    }
                }
            }

            return Content(string.Format("EF Code First complete - count: {0}, sqlQuery count {1}", count, newCount));
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
                    where  DurationMilliseconds >= @duration
                            or Started > @yesterday",
                    new
                    {
                        name = "Home/Index",
                        duration = 100.5,
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
