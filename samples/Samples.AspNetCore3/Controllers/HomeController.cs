using Microsoft.AspNetCore.Mvc;
using StackExchange.Profiling;

namespace Samples.AspNetCore.Controllers
{
    [ExampleActionFilter]
    public class HomeController : Controller
    {
        [ExampleLongActionFilter]
        //[ExampleActionFilter]
        [ExampleAsyncActionFilter]
        public IActionResult Index()
        {
            using (MiniProfiler.Current.Step("Example Step"))
            {
                using (MiniProfiler.Current.Step("Sub timing"))
                {
                    // Not trying to delay the page load here, only serve as an example
                }
                using (MiniProfiler.Current.Step("Sub timing 2"))
                {
                    // Not trying to delay the page load here, only serve as an example
                }
            }
            return View();
        }

        public IActionResult Error() => View();
    }
}
