using System.Linq;
using System.Threading;
using System.Web.Mvc;
using StackExchange.Profiling;

using Samples.Mvc5.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using StackExchange.Profiling.Data;
using Dapper;

namespace Samples.Mvc5.Controllers
{
    public class HomeController : BaseController
    {
        /// <summary>
        /// the default view, home page, top right orientation.
        /// </summary>
        public ActionResult Index()
        {
            DefaultActions();
            return View("Index");
        }

        /// <summary>
        /// Runs the default actions used on all Index views (default, and bottom left/right)
        /// </summary>
        private void DefaultActions()
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
            MiniProfiler.Current?.Stop(true);

            return View();
        }

        /// <summary>
        /// The EF core.
        /// </summary>
        /// <returns>The entity framework core.</returns>
        public ActionResult EFCore()
        {
            int count;
            int? newCount = null;

            EFContext context = null;
            //using (var connection = new ProfiledDbConnection(new SqliteConnection("DataSource=:memory:"), MiniProfiler.Current))
            using (var connection = new SqliteConnection("DataSource=:memory:"))
            using (MiniProfiler.Current.Step("EF Core Stuff"))
            {
                try
                {
                    connection.Open();

                    var options = new DbContextOptionsBuilder<EFContext>()
                        .UseSqlite(connection)
                        .Options;

                    using (MiniProfiler.Current.Step("Create Context"))
                    {
                        context = new EFContext(options);
                    }

                    using (MiniProfiler.Current.Step("Create Schema"))
                    {
                        context.Database.EnsureCreated();
                    }

                    // this is not correct, as the count from this assignment is never actually used
                    using (MiniProfiler.Current.Step("First count"))
                    {
                        count = context.People.Count();
                    }

                    using (MiniProfiler.Current.Step("Insertion"))
                    {
                        var p = new Person { Name = "sam" };
                        context.People.Add(p);
                        context.SaveChanges();
                    }

                    // this count is actually used.
                    using (MiniProfiler.Current.Step("Second count"))
                    {
                        count = context.People.Count();
                    }

                    using (MiniProfiler.Current.Step("Get Count from SqlQuery Method - no sql recorded"))
                    {
                        newCount = context.People.FromSql("Select * from People").Count();
                    }
                    using (MiniProfiler.Current.Step("Get Count using ProfiledConnection - sql recorded"))
                    using (var conn = new ProfiledDbConnection(context.Database.GetDbConnection(), MiniProfiler.Current))
                    {
                        conn.Open();
                        newCount = conn.Query<int>("Select Count(*) from People").Single();
                        conn.Close();
                    }
                }
                finally
                {
                    context?.Dispose();
                }
            }

            return Content(string.Format("EF Code First complete - count: {0}, sqlQuery count {1}", count, newCount));
        }
    }
}
