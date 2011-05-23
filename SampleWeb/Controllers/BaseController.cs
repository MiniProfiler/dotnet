using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Profiling;

namespace SampleWeb.Controllers
{
    public abstract class BaseController : Controller
    {

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
    }
}
