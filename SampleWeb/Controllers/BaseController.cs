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
        public MiniProfiler GetProfiler()
        {
            // does a profiler already exist for this request?
            var profiler = HttpContext.GetProfiler();
            if (profiler != null) return profiler;

            // might want to decide here (or maybe inside the action) whether you want
            // to profile this request - for example, using an "IsSystemAdmin" flag against
            // the user, or similar; this could also all be done in action filters, but this
            // is simple and practical; just return null for most users. For our test, we'll
            // profiler only for local requests (seems reasonable)
            if (Request.IsLocal)
            {
                profiler = new MiniProfiler(Request.Url.OriginalString);
                HttpContext.SetProfiler(profiler);
            }
            return profiler;
        }

        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            // note  that the capturing will usually be terminated by the view anyway
            filterContext.HttpContext.GetProfiler().Step("OnResultExecuting");
            base.OnResultExecuting(filterContext);
        }
    }
}
