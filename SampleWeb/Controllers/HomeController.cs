using System;
using System.Web.Mvc;
using Profiling;
using System.Threading;
namespace SampleWeb.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            var profiler = MiniProfiler.Current;

            using (profiler.Step("Set page title"))
            {
                ViewBag.Message = "Welcome to ASP.NET MVC!";
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

        public ActionResult AjaxMethod()
        {
            var profiler = MiniProfiler.Current;

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

            return Content("the time is now " + DateTime.Now);
        }
    }
}
