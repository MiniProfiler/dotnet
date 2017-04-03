using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Extension methods for MiniProfiler
    /// </summary>
    public static class MiniProfilerExtensions
    {
        // don't allocate this every time...
        private static readonly HtmlString includeNotFound = new HtmlString("<!-- Could not find 'include.partial.html' -->");

        [ThreadStatic]
        private static StringBuilder _stringBuilderCache;

        private static StringBuilder GetStringBuilder() => _stringBuilderCache ?? CreateStringBuilder();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static StringBuilder CreateStringBuilder()
        {
            var sb = new StringBuilder();
            _stringBuilderCache = sb;
            return sb;
        }

        /// <summary>
        /// Renders script tag found in "include.partial.html".
        /// </summary>
        /// <param name="profiler">The profiler to render a tag for.</param>
        /// <param name="context">The <see cref="HttpContext"/> this tag is being rendered in.</param>
        /// <param name="position">The UI position to render the profiler in (defaults to <see cref="MiniProfiler.Settings.PopupRenderPosition"/>).</param>
        /// <param name="showTrivial">Whether to show trivial timings column initially or not (defaults to <see cref="MiniProfiler.Settings.PopupShowTrivial"/>).</param>
        /// <param name="showTimeWithChildren">Whether to show time with children column initially or not (defaults to <see cref="MiniProfiler.Settings.PopupShowTimeWithChildren"/>).</param>
        /// <param name="maxTracesToShow">The maximum number of profilers to show (before the oldest is removed - defaults to <see cref="MiniProfiler.Settings.PopupMaxTracesToShow"/>).</param>
        /// <param name="showControls">Whether to show the controls (defaults to <see cref="MiniProfiler.Settings.ShowControls"/>).</param>
        /// <param name="startHidden">Whether to start hidden (defaults to <see cref="MiniProfiler.Settings.PopupStartHidden"/>).</param>
        public static HtmlString RenderIncludes(
            this MiniProfiler profiler,
            HttpContext context,
            RenderPosition? position = null,
            bool? showTrivial = null,
            bool? showTimeWithChildren = null,
            int? maxTracesToShow = null,
            bool? showControls = null,
            bool? startHidden = null)
        {
            if (profiler == null) return HtmlString.Empty;

            // This is populated in Middleware by SetHeadersAndState
            var state = RequestState.Get(context);
            var path = MiniProfilerMiddleware.Current.BasePath.Value;
            var version = MiniProfiler.Settings.VersionHash;

            var sb = GetStringBuilder();

            sb.Append("<script async id=\"mini-profiler\" src=\"");
            sb.Append(path);
            sb.Append("includes.js?v=");
            sb.Append(version);
            sb.Append("\" data-version=\"");
            sb.Append(version);
            sb.Append("\" data-path=\"");
            sb.Append(path);
            sb.Append("\" data-current-id=\"");
            sb.Append(profiler.Id.ToString());

            sb.Append("\" data-ids=\"");
            var ids = state?.RequestIDs;
            if (ids != null)
            {
                var length = ids.Count;
                for (var i = 0; i < length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }
                    var id = ids[i];
                    sb.Append(id.ToString());
                }
            }

            sb.Append("\" data-position=\"");
            sb.Append((position ?? MiniProfiler.Settings.PopupRenderPosition).ToString().ToLower());

            sb.Append("\" data-trivial=\"");
            sb.Append((showTrivial ?? MiniProfiler.Settings.PopupShowTrivial) ? "true" : "false");

            sb.Append("\" data-children=\"");
            sb.Append((showTimeWithChildren ?? MiniProfiler.Settings.PopupShowTimeWithChildren) ? "true" : "false");

            sb.Append("\" data-max-traces=\"");
            sb.Append((maxTracesToShow ?? MiniProfiler.Settings.PopupMaxTracesToShow).ToString(CultureInfo.InvariantCulture));

            sb.Append("\" data-controls=\"");
            sb.Append((showControls ?? MiniProfiler.Settings.ShowControls) ? "true" : "false");

            sb.Append("\" data-authorized=\"");
            sb.Append((state?.IsAuthorized ?? false) ? "true" : "false");

            sb.Append("\" data-toggle-shortcut=\"");
            sb.Append(MiniProfiler.Settings.PopupToggleKeyboardShortcut);

            sb.Append("\" data-start-hidden=\"");
            sb.Append((startHidden ?? MiniProfiler.Settings.PopupStartHidden) ? "true" : "false");

            sb.Append("\" data-trivial-milliseconds=\"");
            sb.Append(MiniProfiler.Settings.TrivialDurationThresholdMilliseconds.ToString(CultureInfo.InvariantCulture));

            sb.Append("\"></script>");

            var htmlString = new HtmlString(sb.ToString());
            // Clear StringBuilder for next use
            sb.Length = 0;
            return htmlString;
        }
    }
}
