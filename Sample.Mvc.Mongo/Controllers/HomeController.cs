using System;
using System.Threading;
using System.Web.Mvc;
using MongoDB.Driver.Builders;
using SampleWeb.Data;
using SampleWeb.Models;
using StackExchange.Profiling;

namespace SampleWeb.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult EnableProfilingUI()
        {
            MvcApplication.DisableProfilingResults = false;
            return Redirect("/");
        }

        public ActionResult DisableProfilingUI() 
        {
            MvcApplication.DisableProfilingResults = true;
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

            var model = new MongoDemoModel
            {
                FooCount = (int) Repository.FooCollection.Count(),
                FooCountQuery = (int) Repository.FooCollection.Count(Query.LT("r", 0.5))
            };

            return View(model);
        }

        public ActionResult FetchRouteHits()
        {
            var profiler = MiniProfiler.Current;

            using (profiler.Step("Do more complex stuff"))
            {
                Thread.Sleep(new Random().Next(100, 400));
            }

            //using (profiler.Step("FetchRouteHits"))
            //using (var conn = GetConnection(profiler))
            //{
            //    var result = conn.Query<RouteHit>("select RouteName, HitCount from RouteHits order by RouteName");
            //    return Json(result, JsonRequestBehavior.AllowGet);
            //}

            return Json(null);
        }
    }
}
