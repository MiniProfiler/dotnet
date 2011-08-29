using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcMiniProfiler;
using System.Threading;
using SampleWeb.SampleService;
using System.Data.Entity.Infrastructure;
using System.Web.UI;

namespace SampleWeb.Controllers
{
    /// <summary>
    /// This controller is essentially the same as the home controller,
    /// except all methods flow over WCF.
    /// </summary>
    [OutputCache(Location=OutputCacheLocation.None)]
    public class WcfSampleController : BaseController
    {
        
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

            return View();
        }

        public ActionResult About()
        {
            // prevent this specific route from being profiled
            MiniProfiler.Stop(discardResults: true);

            using (MiniProfiler.Current.Step("WCF Call"))
            {
                MakeSampleServiceCall(proxy => proxy.ServiceMethodThatIsNotProfiled());
            }

            return View();
        }

        public ActionResult FetchRouteHits()
        {
            var profiler = MiniProfiler.Current;

            using (profiler.Step("WCF Call"))
            {
                var result = MakeSampleServiceCall(proxy => proxy.FetchRouteHits());
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult MassiveNesting()
        {
            MakeSampleServiceCall(proxy => proxy.MassiveNesting());

            return Content("MassiveNesting completed");
        }

        public ActionResult MassiveNesting2()
        {
            MakeSampleServiceCall(proxy => proxy.MassiveNesting2());

            return Content("MassiveNesting2 completed");
        }

        public ActionResult Duplicated()
        {
            MakeSampleServiceCall(proxy => proxy.Duplicated());

            return Content("Duplicate queries completed");
        }

        public ActionResult EFCodeFirst()
        {
            return Content("Not implemented in this sample");
        }

        /// <summary>
        /// Wrapper around our service call to ensure it is being correctly disposed
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="serviceCall"></param>
        /// <returns></returns>
        private TResult MakeSampleServiceCall<TResult>(Func<SampleService.SampleServiceClient, TResult> serviceCall)
        {
            SampleServiceClient client = null;

            try
            {
                client = new SampleServiceClient();
                var result = serviceCall(client);

                client.Close();

                return result;
            }
            catch
            {
                if (client != null)
                {
                    client.Abort();
                }
                throw;
            }
        }
    }
}