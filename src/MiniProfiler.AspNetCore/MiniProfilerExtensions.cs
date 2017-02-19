using Microsoft.AspNetCore.Html;
using StackExchange.Profiling.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Extension methods for MiniProfiler
    /// </summary>
    public static class MiniProfilerExtensions
    {
        /// <summary>
        /// Renders script tag found in "include.partial.html".
        /// </summary>
        /// <param name="profiler">The profiler to render a tag for.</param>
        /// <param name="position">The UI position to render the profiler in (defaults to <see cref="MiniProfiler.Settings.PopupRenderPosition"/>).</param>
        /// <param name="showTrivial">Whether to show trivial timings column initially or not (defaults to <see cref="MiniProfiler.Settings.PopupShowTrivial"/>).</param>
        /// <param name="showTimeWithChildren">Whether to show time with children column initially or not (defaults to <see cref="MiniProfiler.Settings.PopupShowTimeWithChildren"/>).</param>
        /// <param name="maxTracesToShow">The maximum number of profilers to show (before the oldest is removed - defaults to <see cref="MiniProfiler.Settings.PopupMaxTracesToShow"/>).</param>
        /// <param name="showControls">Whether to show the controls (defaults to <see cref="MiniProfiler.Settings.ShowControls"/>).</param>
        /// <param name="startHidden">Whether to start hidden (defaults to <see cref="MiniProfiler.Settings.PopupStartHidden"/>).</param>
        public static HtmlString RenderIncludes(
            this MiniProfiler profiler,
            RenderPosition? position = null,
            bool? showTrivial = null,
            bool? showTimeWithChildren = null,
            int? maxTracesToShow = null,
            bool? showControls = null,
            bool? startHidden = null)
        {
            if (profiler == null) return HtmlString.Empty;

            // TODO: Figure out auth
            var authorized = true; // MiniProfilerMiddleware.Current.Options.ResultsAuthorize?.Invoke(HttpContext.Current.Request) ?? true;

            // unviewed ids are added to this list during Storage.Save, but we know we haven't 
            // seen the current one yet, so go ahead and add it to the end 
            var ids = authorized ? MiniProfiler.Settings.Storage.GetUnviewedIds(profiler.User) : new List<Guid>();
            ids.Add(profiler.Id);

            if (!MiniProfilerMiddleware.Current.Embedded.TryGetResource("include.partial.html", out string format))
            {
                return new HtmlString("<!-- Could not find 'include.partial.html' -->");
            }

            Func<bool, string> toJs = b => b ? "true" : "false";

            var sb = new StringBuilder(format);
            sb.Replace("{path}", MiniProfilerMiddleware.Current.BasePath.Value.EnsureTrailingSlash())
              .Replace("{version}", MiniProfiler.Settings.VersionHash)
              .Replace("{currentId}", profiler.Id.ToString())
              .Replace("{ids}", string.Join(",", ids.Select(guid => guid.ToString())))
              .Replace("{position}", (position ?? MiniProfiler.Settings.PopupRenderPosition).ToString().ToLower())
              .Replace("{showTrivial}", toJs(showTrivial ?? MiniProfiler.Settings.PopupShowTrivial))
              .Replace("{showChildren}", toJs(showTimeWithChildren ?? MiniProfiler.Settings.PopupShowTimeWithChildren))
              .Replace("{maxTracesToShow}", (maxTracesToShow ?? MiniProfiler.Settings.PopupMaxTracesToShow).ToString())
              .Replace("{showControls}", toJs(showControls ?? MiniProfiler.Settings.ShowControls))
              .Replace("{authorized}", toJs(authorized))
              .Replace("{toggleShortcut}", MiniProfiler.Settings.PopupToggleKeyboardShortcut)
              .Replace("{startHidden}", toJs(startHidden ?? MiniProfiler.Settings.PopupStartHidden))
              .Replace("{trivialMilliseconds}", MiniProfiler.Settings.TrivialDurationThresholdMilliseconds.ToString());
            return new HtmlString(sb.ToString());
        }
    }
}
