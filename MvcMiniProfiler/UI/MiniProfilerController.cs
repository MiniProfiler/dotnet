using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;
using System.IO;
using System.Web.UI;
using MvcMiniProfiler.Helpers;
using System.Diagnostics;

namespace MvcMiniProfiler.UI
{
    public class MiniProfilerController : Controller
    {
        public static bool IsProfilerPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            return path.Contains("/mini-profiler-includes.") || path.Contains("/mini-profiler-results");
        }

        internal static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("", "mini-profiler-includes.{type}", new { controller = "MiniProfiler", action = "Includes", type = "" });
            routes.MapRoute("", "mini-profiler-results", new { controller = "MiniProfiler", action = "Results" });
        }

        /// <summary>
        /// Includes files keyed by filename.
        /// </summary>
        private static readonly Dictionary<string, string> _IncludesCache = new Dictionary<string, string>();

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

            string fileContents = null;

            if (!_IncludesCache.TryGetValue(filename, out fileContents))
            {
                using (var stream = GetResource(filename))
                using (var reader = new StreamReader(stream))
                {
                    fileContents = reader.ReadToEnd();
                }

                _IncludesCache[filename] = fileContents;
            }

            var cache = Response.Cache;
            cache.SetCacheability(System.Web.HttpCacheability.Public);
            cache.SetExpires(DateTime.Now.AddDays(7));
            cache.SetValidUntilExpires(true);

            return Content(fileContents, contentType);
        }

        public ActionResult Results(Guid id, string popup)
        {
            MiniProfiler.Settings.EnsureCacheMethods();

            var isPopup = !string.IsNullOrWhiteSpace(popup);
            var profiler = MiniProfiler.Settings.ShortTermCacheGetter(id);

            if (profiler == null)
                profiler = MiniProfiler.Settings.LongTermCacheGetter(id);

            if (profiler == null)
                return isPopup ? NotFound() : NotFound("text/html", "No MiniProfiler results found with Id=" + id.ToString());

            // the first time we hit this route as a full results page, the prof won't be in long term cache, so put it there for sharing
            // each subsequent time the full page is hit, just save again, so we act as a sliding expiration
            if (!isPopup)
                MiniProfiler.Settings.LongTermCacheSetter(profiler);

            var model = new MiniProfilerResultsModel { MiniProfiler = profiler, IsPopup = isPopup };

            
            string html = "";            using (var reader = new StreamReader(GetResource("MiniProfilerResults.cshtml")))            {                html = reader.ReadToEnd();
            }

            return Content(RazorCompiler.Render(html, model));
        }


        private Stream GetResource(string filename)
        {
            return typeof(MiniProfilerController).Assembly.GetManifestResourceStream("MvcMiniProfiler.UI." + filename);
        }

        private ActionResult NotFound(string contentType = "text/plain", string message = null)
        {
            Response.StatusCode = 404;
            return Content(message, contentType);
        }
    }
}
