using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Extension methods for MiniProfiler
    /// </summary>
    public static class MiniProfilerExtensions
    {
        /// <summary>
        /// Renders script tag for including MiniProfiler.
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

            var result = profiler.RenderIncludes(
                path: path,
                requestIDs: state?.RequestIDs,
                isAuthorized: state?.IsAuthorized ?? false,
                position: position,
                showTrivial: showTrivial,
                showTimeWithChildren: showTimeWithChildren,
                maxTracesToShow: maxTracesToShow,
                showControls: showControls,
                startHidden: startHidden);

            return new HtmlString(result);
        }
    }
}
