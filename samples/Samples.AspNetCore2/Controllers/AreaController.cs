using Microsoft.AspNetCore.Mvc;

namespace Samples.AspNetCore.Controllers
{
    [Area("MySpace")]
    public class AreaController : Controller
    {
        public IActionResult Simple() => Content("Simple");

        public IActionResult Index() => View();
    }
}
