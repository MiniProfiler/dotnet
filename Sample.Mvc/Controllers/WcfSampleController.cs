namespace SampleWeb.Controllers
{
    using System;
    using System.Threading;
    using System.Web.Mvc;
    using System.Web.UI;

    using SampleWeb.SampleService;

    using StackExchange.Profiling;

    /// <summary>
    /// This controller is essentially the same as the home controller,
    /// except all methods flow over WCF.
    /// </summary>
    [OutputCache(Location = OutputCacheLocation.None)]
    public class WcfSampleController : BaseController
    {
        /// <summary>
        /// default view.
        /// </summary>
        /// <returns>The <see cref="ActionResult"/>.</returns>
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

            return this.View();
        }

        /// <summary>
        /// about the WCF sample.
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        public ActionResult About()
        {
            // prevent this specific route from being profiled
            MiniProfiler.Stop(discardResults: true);

            using (MiniProfiler.Current.Step("WCF Call"))
            {
                this.MakeSampleServiceCall(proxy => proxy.ServiceMethodThatIsNotProfiled());
            }

            return this.View();
        }

        /// <summary>
        /// fetch the route hits.
        /// </summary>
        /// <returns>The <see cref="ActionResult"/>.</returns>
        public ActionResult FetchRouteHits()
        {
            var profiler = MiniProfiler.Current;

            using (profiler.Step("WCF Call"))
            {
                var result = MakeSampleServiceCall(proxy => proxy.FetchRouteHits());
                return this.Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// massive nesting view.
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        public ActionResult MassiveNesting()
        {
            this.MakeSampleServiceCall(proxy => proxy.MassiveNesting());

            return this.Content("MassiveNesting completed");
        }

        /// <summary>
        /// massive nesting 2.
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        public ActionResult MassiveNesting2()
        {
            this.MakeSampleServiceCall(proxy => proxy.MassiveNesting2());

            return this.Content("MassiveNesting2 completed");
        }

        /// <summary>
        /// duplicated queries.
        /// </summary>
        /// <returns>The <see cref="ActionResult"/>.</returns>
        public ActionResult Duplicated()
        {
            this.MakeSampleServiceCall(proxy => proxy.Duplicated());

            return this.Content("Duplicate queries completed");
        }

        /// <summary>
        /// The EF code first.
        /// </summary>
        /// <returns>The <see cref="ActionResult"/>.</returns>
        public ActionResult EFCodeFirst()
        {
            return this.Content("Not implemented in this sample");
        }

        /// <summary>
        /// Wrapper around our service call to ensure it is being correctly disposed
        /// </summary>
        /// <typeparam name="TResult">the service call type.</typeparam>
        /// <param name="serviceCall">the service call delegate</param>
        /// <returns>the result of the service call, with the error information thrown correctly otherwise.</returns>
        private TResult MakeSampleServiceCall<TResult>(Func<SampleServiceClient, TResult> serviceCall)
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