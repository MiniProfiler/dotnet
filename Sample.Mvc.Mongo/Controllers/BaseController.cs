using System;
using System.Web.Mvc;
using StackExchange.Profiling;

namespace SampleWeb.Controllers
{
    public abstract class BaseController : Controller
    {
        private IDisposable _resultExecutingToExecuted;

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            MiniProfiler profiler = MiniProfiler.Current;

            using (profiler.Step("OnActionExecuting"))
            {
                UpsertRouteHit(filterContext.ActionDescriptor, profiler);
                base.OnActionExecuting(filterContext);
            }

            profiler.Head.AddCustomTiming("sample",
                new CustomTiming(profiler, "SOME COMMAND STRING")
                {
                    DurationMilliseconds = 123.45m,
                    StartMilliseconds = 0.07m,
                    ExecuteType = "COMMAND"
                });
        }

        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            _resultExecutingToExecuted = MiniProfiler.Current.Step("OnResultExecuting");

            base.OnResultExecuting(filterContext);
        }

        protected override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (_resultExecutingToExecuted != null)
                _resultExecutingToExecuted.Dispose();

            base.OnResultExecuted(filterContext);
        }

        private void UpsertRouteHit(ActionDescriptor actionDesc, MiniProfiler profiler)
        {
            string routeName = actionDesc.ControllerDescriptor.ControllerName + "/" + actionDesc.ActionName;

            //            using (var conn = GetConnection(profiler))
            //            {
            //                var param = new { routeName = routeName };

            //                using (profiler.Step("Insert RouteHits"))
            //                {
            //                   conn.Execute("insert or ignore into RouteHits (RouteName, HitCount) values (@routeName, 0)", param);
            //                }
            //                using (profiler.Step("Update RouteHits"))
            //                {
            //                    // let's put some whitespace in this query to demonstrate formatting
            //                    conn.Execute(
            //@"update RouteHits
            //set    HitCount = HitCount + 1
            //where  RouteName = @routeName", param);
            //                }
            //            }
        }
    }
}