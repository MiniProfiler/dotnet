using System;
using System.Web.Mvc;
using Profiling;
using System.Threading;
using Dapper;
namespace SampleWeb.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            var profiler = MiniProfiler.Current;

            using (profiler.Step("Set page title"))
            {
                ViewBag.Message = "See the profiler in the top left corner!";
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
            using (var conn = GetOpenConnection())
            {
                var result = conn.Query<RouteHit>("select RouteName, HitCount from RouteHits order by RouteName");
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        public class RouteHit
        {
            public string RouteName { get; set; }
            public Int64 HitCount { get; set; }
        }
    }
}
