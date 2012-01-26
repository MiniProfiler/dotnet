using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StackExchange.Profiling;
using System.Data.Common;

using Dapper;

namespace SampleWeb.Controllers
{
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// Returns an open connection that will have its queries profiled.
        /// </summary>
        public static DbConnection GetConnection(MiniProfiler profiler = null)
        {
            using (profiler.Step("GetOpenConnection"))
            {
                DbConnection cnn = new System.Data.SQLite.SQLiteConnection(MvcApplication.ConnectionString);

                // to get profiling times, we have to wrap whatever connection we're using in a ProfiledDbConnection
                // when MiniProfiler.Current is null, this connection will not record any database timings
                if (MiniProfiler.Current != null)
                {
                    cnn = new StackExchange.Profiling.Data.ProfiledDbConnection(cnn, MiniProfiler.Current);
                }

                cnn.Open();
                return cnn;
            }
        }


        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var profiler = MiniProfiler.Current;

            using (profiler.Step("OnActionExecuting"))
            {
                UpsertRouteHit(filterContext.ActionDescriptor, profiler);
                base.OnActionExecuting(filterContext);
            }
        }

        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            _resultExecutingToExecuted = MiniProfiler.Current.Step("OnResultExecuting");

            base.OnResultExecuting(filterContext);
        }

        private IDisposable _resultExecutingToExecuted;

        protected override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (_resultExecutingToExecuted != null)
                _resultExecutingToExecuted.Dispose();

            base.OnResultExecuted(filterContext);
        }


        private void UpsertRouteHit(ActionDescriptor actionDesc, MiniProfiler profiler)
        {
            var routeName = actionDesc.ControllerDescriptor.ControllerName + "/" + actionDesc.ActionName;

            using (var conn = GetConnection(profiler))
            {
                var param = new { routeName = routeName };

                using (profiler.Step("Insert RouteHits"))
                {
                   conn.Execute("insert or ignore into RouteHits (RouteName, HitCount) values (@routeName, 0)", param);
                }
                using (profiler.Step("Update RouteHits"))
                {
                    // let's put some whitespace in this query to demonstrate formatting
                    conn.Execute(
@"update RouteHits
set    HitCount = HitCount + 1
where  RouteName = @routeName", param);
                }
            }
        }

    }
}
