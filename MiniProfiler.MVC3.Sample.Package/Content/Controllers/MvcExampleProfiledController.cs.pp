using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcMiniProfiler;
using System.Threading;

namespace $rootnamespace$.Controllers
{
    public class MvcExampleProfiledController : Controller
    {
        public ActionResult Index()
        {
            var profiler = MiniProfiler.Current; // it's ok if this is null

            using (profiler.Step("Set page title"))
            {
                ViewBag.Title = "Home Page";
            }

            using (profiler.Step("Doing complex stuff"))
            {
                using (profiler.Step("Step A"))
                { // something more interesting here
                    Thread.Sleep(100);
                }
                using (profiler.Step("Step B"))
                { // and here
                    Thread.Sleep(250);
                }
            }

            using (profiler.Step("Set message"))
            {
                ViewBag.Message = "Welcome to ASP.NET MVC!";
            }

            return View();
        }

        public ActionResult About()
        {
            return View();
        }
    }
}