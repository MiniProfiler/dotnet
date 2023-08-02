using System.Threading;
using Microsoft.AspNetCore.Mvc;

namespace Samples.AspNetCore.Views.Shared
{
    public class ComponentTestViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            Thread.Sleep(5);
            return View();
        }
    }
}
