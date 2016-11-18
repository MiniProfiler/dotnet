using System;
using System.Web.Mvc;
using SampleWeb.Data;
using StackExchange.Profiling;

namespace SampleWeb.Controllers
{
    public abstract class BaseController : Controller
    {
        private IDisposable _resultExecutingToExecuted;

        protected MongoDataRepository Repository { get; private set; }

        protected BaseController()
        {
            Repository = new MongoDataRepository("mongodb://localhost", "test");
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            MiniProfiler profiler = MiniProfiler.Current;

            using (profiler.Step("OnActionExecuting"))
            {
                base.OnActionExecuting(filterContext);
            }
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

    }
}