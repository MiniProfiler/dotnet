using StackExchange.Profiling.Helpers;
using StackExchange.Profiling.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Contains helper methods that ease working with null <see cref="MiniProfiler"/>s.
    /// </summary>
    public static class MiniProfilerWebExtensions
    {
        private static readonly HtmlString _empty = new HtmlString(string.Empty);

        /// <summary>
        /// Returns the <c>css</c> and <c>javascript</c> includes needed to display the MiniProfiler results UI.
        /// </summary>
        /// <param name="profiler">The profiler this extension method is called on</param>
        /// <param name="position">Which side of the page the profiler popup button should be displayed on (defaults to left)</param>
        /// <param name="showTrivial">Whether to show trivial timings by default (defaults to false)</param>
        /// <param name="showTimeWithChildren">Whether to show time the time with children column by default (defaults to false)</param>
        /// <param name="maxTracesToShow">The maximum number of trace popups to show before removing the oldest (defaults to 15)</param>
        /// <param name="showControls">when true, shows buttons to minimize and clear MiniProfiler results</param>
        /// <param name="startHidden">Should the profiler start as hidden. Default to null.</param>
        /// <returns>Script and link elements normally; an empty string when there is no active profiling session.</returns>
        public static IHtmlString RenderIncludes(
            this MiniProfiler profiler,
            RenderPosition? position = null,
            bool? showTrivial = null,
            bool? showTimeWithChildren = null,
            int? maxTracesToShow = null,
            bool? showControls = null,
            bool? startHidden = null)
        {
            if (profiler == null) return _empty;
            var settings = profiler.Options as MiniProfilerOptions;
            if (settings == null) return _empty;

            var authorized = settings.ResultsAuthorize?.Invoke(HttpContext.Current.Request) ?? true;
            // If we're not authroized, we're just rendering a <script> tag for no reason.
            if (!authorized) return _empty;

            // unviewed ids are added to this list during Storage.Save, but we know we haven't 
            // seen the current one yet, so go ahead and add it to the end 
            var ids = authorized ? settings.Storage.GetUnviewedIds(profiler.User) : new List<Guid>();
            ids.Add(profiler.Id);

            var path = VirtualPathUtility.ToAbsolute(settings.RouteBasePath).EnsureTrailingSlash();

            var result = profiler.RenderIncludes(
                path: path,
                isAuthorized: authorized,
                requestIDs: ids,
                position: position,
                showTrivial: showTrivial,
                showTimeWithChildren: showTimeWithChildren,
                maxTracesToShow: maxTracesToShow,
                showControls: showControls,
                startHidden: startHidden);

            return new HtmlString(result);
        }

        /// <summary>
        /// Returns an HTML-encoded string with a text-representation of <paramref name="profiler"/>; returns "" when profiler is null.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        public static IHtmlString Render(this MiniProfiler profiler) =>
            new HtmlString(profiler.RenderPlainText(true));

        /// <summary>
        /// Returns null if there is not client timing stuff
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> to get client timings from.</param>
        public static ClientTimings GetClientTimings(this HttpRequest request)
        {
            var dict = new Dictionary<string, string>();
            var form = request.Form;
            foreach (var k in form.AllKeys)
            {
                dict.Add(k, form[k]);
            }
            return ClientTimings.FromForm(dict);
        }
    }
}
