using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Profiling;
using System.Data.Common;

using Dapper;

namespace SampleWeb.Controllers
{
    public abstract class BaseController : Controller
    {

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var profiler = MiniProfiler.Current;

            using (profiler.Step("OnActionExecuting"))
            {
                var actionDesc = filterContext.ActionDescriptor;
                var routeName = actionDesc.ControllerDescriptor.ControllerName + "/" + actionDesc.ActionName;

                using (var conn = GetOpenConnection(profiler))
                {
                    var param = new { routeName = routeName };

                    using (profiler.Step("Insert RouteHits"))
                    {
                        conn.Execute("insert or ignore into RouteHits (RouteName, HitCount) values (@routeName, 0)", param);
                    }
                    using (profiler.Step("Update RouteHits"))
                    {
                        conn.Execute("update RouteHits set HitCount = HitCount + 1 where RouteName = @routeName", param);
                    }
                }
                base.OnActionExecuting(filterContext);
            }
        }

        private IDisposable _viewRenderingStep;

        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            _viewRenderingStep = MiniProfiler.Current.Step("OnResultExecuting");

            base.OnResultExecuting(filterContext);
        }

        protected override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (_viewRenderingStep != null) _viewRenderingStep.Dispose();

            base.OnResultExecuted(filterContext);
        }

        /// <summary>
        /// 
        /// </summary>
        protected DbConnection GetOpenConnection(MiniProfiler profiler = null)
        {
            using (profiler.Step("GetOpenConnection"))
            {
                var dbPath = Server.MapPath("~/App_Data/TestMiniProfiler.sqlite");
                var wrapped = new System.Data.SQLite.SQLiteConnection("Data Source = " + dbPath);

                // to get profiling times, we have to wrap whatever connection we're using in this ProfiledDbConnection
                // when MiniProfiler.Current is null, this connection will not 
                var result = new Profiling.Data.ProfiledDbConnection(wrapped, MiniProfiler.Current);

                result.Open();

                return result;
            }
        }
    }
}
