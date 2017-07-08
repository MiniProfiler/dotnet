using StackExchange.Profiling.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace StackExchange.Profiling.Internal
{
    /// <summary>
    /// Internal MiniProfiler extensions, not meant for consumption.
    /// This can and probably will break without warning. Don't use the .Internal namespace directly.
    /// </summary>
    public static class MiniProfilerExtensions
    {
        /// <summary>
        /// Renders script tag for including MiniProfiler.
        /// </summary>
        /// <param name="profiler">The profiler to render a tag for.</param>
        /// <param name="path">The root path that MiniProfiler is being served from.</param>
        /// <param name="isAuthorized">Whether the current user is authorized for MiniProfiler.</param>
        /// <param name="requestIDs">The request IDs to fetch for this render.</param>
        /// <param name="position">The UI position to render the profiler in (defaults to <see cref="MiniProfiler.Settings.PopupRenderPosition"/>).</param>
        /// <param name="showTrivial">Whether to show trivial timings column initially or not (defaults to <see cref="MiniProfiler.Settings.PopupShowTrivial"/>).</param>
        /// <param name="showTimeWithChildren">Whether to show time with children column initially or not (defaults to <see cref="MiniProfiler.Settings.PopupShowTimeWithChildren"/>).</param>
        /// <param name="maxTracesToShow">The maximum number of profilers to show (before the oldest is removed - defaults to <see cref="MiniProfiler.Settings.PopupMaxTracesToShow"/>).</param>
        /// <param name="showControls">Whether to show the controls (defaults to <see cref="MiniProfiler.Settings.ShowControls"/>).</param>
        /// <param name="startHidden">Whether to start hidden (defaults to <see cref="MiniProfiler.Settings.PopupStartHidden"/>).</param>
        public static string RenderIncludes(
            this MiniProfiler profiler,
            string path,
            bool isAuthorized,
            List<Guid> requestIDs = null,
            RenderPosition? position = null,
            bool? showTrivial = null,
            bool? showTimeWithChildren = null,
            int? maxTracesToShow = null,
            bool? showControls = null,
            bool? startHidden = null)
        {
            var sb = StringBuilderCache.Get();

            sb.Append("<script async id=\"mini-profiler\" src=\"");
            sb.Append(path);
            sb.Append("includes.js?v=");
            sb.Append(MiniProfiler.Settings.VersionHash);
            sb.Append("\" data-version=\"");
            sb.Append(MiniProfiler.Settings.VersionHash);
            sb.Append("\" data-path=\"");
            sb.Append(path);
            sb.Append("\" data-current-id=\"");
            sb.Append(profiler.Id.ToString());

            sb.Append("\" data-ids=\"");
            if (requestIDs != null)
            {
                var length = requestIDs.Count;
                for (var i = 0; i < length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }
                    var id = requestIDs[i];
                    sb.Append(id.ToString());
                }
            }

            sb.Append("\" data-position=\"");
            sb.Append((position ?? MiniProfiler.Settings.PopupRenderPosition).ToString().ToLower());
            sb.Append('"');

            if (isAuthorized)
            {
                sb.Append(" data-authorized=\"true\"");
            }
            if (showTrivial ?? MiniProfiler.Settings.PopupShowTrivial)
            {
                sb.Append(" data-trivial=\"true\"");
            }
            if (showTimeWithChildren ?? MiniProfiler.Settings.PopupShowTimeWithChildren)
            {
                sb.Append(" data-children=\"true\"");
            }
            if (showControls ?? MiniProfiler.Settings.ShowControls)
            {
                sb.Append(" data-controls=\"true\"");
            }
            if (startHidden ?? MiniProfiler.Settings.PopupStartHidden)
            {
                sb.Append(" data-start-hidden=\"true\"");
            }

            sb.Append(" data-max-traces=\"");
            sb.Append((maxTracesToShow ?? MiniProfiler.Settings.PopupMaxTracesToShow).ToString(CultureInfo.InvariantCulture));

            sb.Append("\" data-toggle-shortcut=\"");
            sb.Append(MiniProfiler.Settings.PopupToggleKeyboardShortcut);

            sb.Append("\" data-trivial-milliseconds=\"");
            sb.Append(MiniProfiler.Settings.TrivialDurationThresholdMilliseconds.ToString(CultureInfo.InvariantCulture));

            sb.Append("\"></script>");

            return sb.ToStringRecycle();
        }

        /// <summary>
        /// Renders a full HTML page for the share link in MiniProfiler.
        /// </summary>
        /// <param name="profiler">The profiler to render a tag for.</param>
        /// <param name="path">The root path that MiniProfiler is being served from.</param>
        /// <returns>A full HTML page for this MiniProfiler.</returns>
        public static string RenderResultsHtml(this MiniProfiler profiler, string path)
        {
            var sb = StringBuilderCache.Get();
            sb.Append("<html><head><title>");
            sb.Append(profiler.Name);
            sb.Append(" (");
            sb.Append(profiler.DurationMilliseconds.ToString(CultureInfo.InvariantCulture));
            sb.Append(" ms) - Profiling Results</title><script>var profiler = ");
            sb.Append(profiler.ToJson(htmlEscape: true));
            sb.Append(";</script>");
            sb.Append(RenderIncludes(profiler, path: path, isAuthorized: true));
            sb.Append(@"</head><body><div class=""profiler-result-full""></div></body></html>");
            return sb.ToString();
        }
    }
}
