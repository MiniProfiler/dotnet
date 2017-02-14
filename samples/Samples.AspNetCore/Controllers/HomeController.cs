using Microsoft.AspNetCore.Mvc;
using StackExchange.Profiling;
using System.Threading;

namespace Samples.AspNetCore.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            using (MiniProfiler.Current.Step("Delay Step"))
            {
                Thread.Sleep(50);
                using (MiniProfiler.Current.Step("Sub timing"))
                {
                    Thread.Sleep(65);
                }
            }

            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";
            return View();
        }

        public IActionResult Error() => View();
    }
}
