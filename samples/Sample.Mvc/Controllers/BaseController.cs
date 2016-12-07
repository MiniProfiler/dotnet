using System;
using System.Data.Common;
using System.Web.Mvc;

using Dapper;
using StackExchange.Profiling;

namespace SampleWeb.Controllers
{
    /// <summary>
    /// The base controller.
    /// </summary>
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// keep track of the profiler to dispose it.
        /// </summary>
        private IDisposable _resultExecutingToExecuted;

        /// <summary>
        /// Returns an open connection that will have its queries profiled.
        /// </summary>
        /// <param name="profiler">The mini profiler.</param>
        /// <returns>the data connection abstraction.</returns>
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

        /// <summary>
        /// on action executing.
        /// demonstrate using a base controller to intercept actions as they are executed.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var profiler = MiniProfiler.Current;

            using (profiler.Step("OnActionExecuting"))
            {
                UpsertRouteHit(filterContext.ActionDescriptor, profiler);
                base.OnActionExecuting(filterContext);
            }
        }

        /// <summary>
        /// on result executing.
        /// demonstrate using a base controller to intercept actions as they are executed.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            _resultExecutingToExecuted = MiniProfiler.Current.Step("OnResultExecuting");

            base.OnResultExecuting(filterContext);
        }

        /// <summary>
        /// on result executed.
        /// demonstrate using a base controller to intercept actions as they are executed.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        protected override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (_resultExecutingToExecuted != null)
                _resultExecutingToExecuted.Dispose();

            base.OnResultExecuted(filterContext);
        }

        /// <summary>
        /// The UPSERT route hit.
        /// demonstrate using a base controller to intercept actions as they are executed.
        /// </summary>
        /// <param name="actionDesc">The action description.</param>
        /// <param name="profiler">The profiler.</param>
        private void UpsertRouteHit(ActionDescriptor actionDesc, MiniProfiler profiler)
        {
//            var routeName = actionDesc.ControllerDescriptor.ControllerName + "/" + actionDesc.ActionName;

//            using (var conn = GetConnection(profiler))
//            {
//                var param = new { routeName };

//                using (profiler.Step("Insert RouteHits"))
//                {
//                   conn.Execute("insert or ignore into RouteHits (RouteName, HitCount) values (@routeName, 0)", param);
//                }
//                using (profiler.Step("Update RouteHits"))
//                {
//                    // let's put some whitespace in this query to demonstrate formatting
//                    // i might have broken this with the tabs (jim - 2013-01-08)
//                    conn.Execute(
//                        @"update RouteHits
//                        set    HitCount = HitCount + 1
//                        where  RouteName = @routeName", 
//                        param);
//                }
//            }
        }

    }
}
