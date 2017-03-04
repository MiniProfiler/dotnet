using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;
using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Samples.AspNetCore.Controllers
{
    public class TestController : Controller
    {
        public string ConnectionString =>
            "Data Source = " + Path.Combine(_env.ContentRootPath, @"App_Data\TestMiniProfiler.sqlite");

        private readonly IHostingEnvironment _env;
        public TestController(IHostingEnvironment env)
        {
            _env = env;
        }

        public ActionResult EnableProfilingUI()
        {
            Program.DisableProfilingResults = false;
            return Redirect("/");
        }

        /// <summary>
        /// disable the profiling UI.
        /// </summary>
        /// <returns>disable profiling the UI</returns>
        public ActionResult DisableProfilingUI()
        {
            Program.DisableProfilingResults = true;
            return Redirect("/");
        }

        public IActionResult DuplicatedQueries()
        {
            using (var conn = GetConnection())
            {
                long total = 0;

                for (int i = 0; i < 20; i++)
                {
                    total += conn.QueryFirst<long>("select count(1) from RouteHits where HitCount = @i", new { i });
                }
                return Content(string.Format("Duplicated Queries (N+1) completed {0}", total));
            }
        }

        public async Task<IActionResult> DuplicatedQueriesAsync()
        {
            using (var conn = await GetConnectionAsync().ConfigureAwait(false))
            {
                long total = 0;

                for (int i = 0; i < 20; i++)
                {
                    total += await conn.QueryFirstAsync<long>("select count(1) from RouteHits where HitCount = @i", new { i }).ConfigureAwait(false);
                }
                return Content(string.Format("Duplicated Queries (N+1) completed {0}", total));
            }
        }

        public IActionResult MassiveNesting()
        {
            var i = 0;
            using (var conn = GetConnection())
            {
                RecursiveMethod(ref i, conn, MiniProfiler.Current);
            }
            return Content("Massive Nesting completed");
        }

        public IActionResult MassiveNesting2()
        {
            for (int i = 0; i < 6; i++)
            {
                MassiveNesting();
            }
            return Content("Massive Nesting 2 completed");
        }

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

        public IActionResult MinSaveMs()
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

        public IActionResult ParameterizedSqlWithEnums()
        {
            using (var conn = GetConnection())
            {
                var shouldBeOne = conn.Query<long>("select @OK = 200", new { System.Net.HttpStatusCode.OK }).Single();
                return Content("Parameterized SQL with Enums completed: " + shouldBeOne);
            }
        }

        public RedirectToActionResult MultipleRedirect() => RedirectToAction(nameof(MultipleRedirectChild));
        public RedirectToActionResult MultipleRedirectChild() => RedirectToAction(nameof(MultipleRedirectChildChild));
        public IActionResult MultipleRedirectChildChild() => Content("You should see 3 MiniProfilers from that.");

        public IActionResult ViewProfiling() => View("ForLoop");

        /// <summary>
        /// Returns an open connection that will have its queries profiled.
        /// </summary>
        /// <param name="profiler">The mini profiler.</param>
        /// <returns>the data connection abstraction.</returns>
        public DbConnection GetConnection(MiniProfiler profiler = null)
        {
            using (profiler.Step("GetOpenConnection"))
            {
                DbConnection cnn = new SqliteConnection(ConnectionString);

                // to get profiling times, we have to wrap whatever connection we're using in a ProfiledDbConnection
                // when MiniProfiler.Current is null, this connection will not record any database timings
                if (MiniProfiler.Current != null)
                {
                    cnn = new ProfiledDbConnection(cnn, MiniProfiler.Current);
                }

                cnn.Open();
                return cnn;
            }
        }

        /// <summary>
        /// Asynchronously returns an open connection that will have its queries profiled.
        /// </summary>
        /// <param name="profiler">The mini profiler.</param>
        /// <returns>the data connection abstraction.</returns>
        public async Task<DbConnection> GetConnectionAsync(MiniProfiler profiler = null)
        {
            using (profiler.Step("GetOpenConnectionAsync"))
            {
                DbConnection cnn = new SqliteConnection(ConnectionString);

                // to get profiling times, we have to wrap whatever connection we're using in a ProfiledDbConnection
                // when MiniProfiler.Current is null, this connection will not record any database timings
                if (MiniProfiler.Current != null)
                {
                    cnn = new ProfiledDbConnection(cnn, MiniProfiler.Current);
                }

                await cnn.OpenAsync().ConfigureAwait(false);
                return cnn;
            }
        }
    }
}
