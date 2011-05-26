using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;
using System.IO;

namespace Profiling.UI
{
    public class MiniProfilerController : Controller
    {
        public static bool IsProfilerPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            return path.Contains("/mini-profiler-includes.") || path.Contains("/mini-profiler-results");
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("", "mini-profiler-includes.{type}", new { controller = "MiniProfiler", action = "Includes", type = "" });
            routes.MapRoute("", "mini-profiler-results", new { controller = "MiniProfiler", action = "Results" });
        }

        public ActionResult Includes(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return NotFound();

            var filename = "Includes." + type;
            var contentType = "";

            switch (type)
            {
                case "js":
                    contentType = "application/javascript";
                    break;
                case "less":
                    contentType = "text/plain";
                    break;
                default:
                    return NotFound();
            }

            using (var stream = GetResource(filename))
            {
                stream.CopyTo(Response.OutputStream);
            }

            return Content(null, contentType);
        }

        public ActionResult Results(Guid id, string share)
        {
            var profiler = MiniProfiler.ShortTermCacheGetter(id);
            if (profiler == null) return NotFound();

            var model = new MiniProfilerResultsModel { MiniProfiler = profiler, ShowShareLink = !string.IsNullOrWhiteSpace(share) };

            EnsureResultsCompiled();
            var html = RazorEngine.Razor.Run(model, "MiniProfilerResults");
            return Content(html);
        }

        private static volatile bool _isResultsCompiled = false;

        private void EnsureResultsCompiled()
        {
            if (_isResultsCompiled) return;

            lock (typeof(MiniProfilerController))
            {
                if (_isResultsCompiled) return;

                string html = "";
                using (var reader = new StreamReader(GetResource("MiniProfilerResults.cshtml")))
                {
                    html = reader.ReadToEnd();
                    // HACK: RazorEngine doesn't like @model, but intellisense needs it
                    html = html.Replace("@model Profiling.UI.MiniProfilerResultsModel", "");
                }
                try
                {
                    RazorEngine.Razor.Compile(html, typeof(MiniProfilerResultsModel), "MiniProfilerResults");
                }
                catch (RazorEngine.Templating.TemplateCompilationException ex)
                {
                    var msg = "Razor compile error: " + string.Join("\n", ex.Errors.Select(e => e.ToString()));
                    throw new InvalidOperationException(msg);
                }
                _isResultsCompiled = true;
            }
        }

        private Stream GetResource(string filename)
        {
            // TODO: return string from here and cache it
            return typeof(MiniProfilerController).Assembly.GetManifestResourceStream("MiniProfiler.UI." + filename);
        }

        private ActionResult NotFound(string contentType = "text/plain")
        {
            Response.StatusCode = 404;
            return Content(null, contentType);
        }
    }
}
