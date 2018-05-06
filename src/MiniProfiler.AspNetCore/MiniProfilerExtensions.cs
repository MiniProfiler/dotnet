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
        /// <param name="position">The UI position to render the profiler in (defaults to <see cref="MiniProfilerBaseOptions.PopupRenderPosition"/>).</param>
        /// <param name="showTrivial">Whether to show trivial timings column initially or not (defaults to <see cref="MiniProfilerBaseOptions.PopupShowTrivial"/>).</param>
        /// <param name="showTimeWithChildren">Whether to show time with children column initially or not (defaults to <see cref="MiniProfilerBaseOptions.PopupShowTimeWithChildren"/>).</param>
        /// <param name="maxTracesToShow">The maximum number of profilers to show (before the oldest is removed - defaults to <see cref="MiniProfilerBaseOptions.PopupMaxTracesToShow"/>).</param>
        /// <param name="showControls">Whether to show the controls (defaults to <see cref="MiniProfilerBaseOptions.ShowControls"/>).</param>
        /// <param name="startHidden">Whether to start hidden (defaults to <see cref="MiniProfilerBaseOptions.PopupStartHidden"/>).</param>
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

            // If we're not authroized, we're just rendering a <script> tag for no reason.
            if (state?.IsAuthorized == false) return HtmlString.Empty;

            var path = (profiler.Options as MiniProfilerOptions)?.RouteBasePath.Value.EnsureTrailingSlash();

            var result = Render.Includes(
                profiler,
                path: context.Request.PathBase + path,
                isAuthorized: state?.IsAuthorized ?? false,
                requestIDs: state?.RequestIDs,
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
