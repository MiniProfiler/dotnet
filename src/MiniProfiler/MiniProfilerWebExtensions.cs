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
            return MiniProfilerHandler.RenderIncludes(
                profiler,
                position,
                showTrivial,
                showTimeWithChildren,
                maxTracesToShow,
                showControls,
                startHidden);
        }

        /// <summary>
        /// Returns an html-encoded string with a text-representation of <paramref name="profiler"/>; returns "" when profiler is null.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        public static IHtmlString Render(this MiniProfiler profiler) =>
            new HtmlString(RenderImpl(profiler, true));

        /// <summary>
        /// Returns a plain-text representation of <paramref name="profiler"/>, suitable for viewing from 
        /// <see cref="Console"/>, log, or unit test output.
        /// </summary>
        /// <param name="profiler">A profiling session with child <see cref="Timing"/> instances.</param>
        public static string RenderPlainText(this MiniProfiler profiler) =>
            RenderImpl(profiler, false);

        private static string RenderImpl(MiniProfiler profiler, bool htmlEncode)
        {
            if (profiler == null) return string.Empty;

            var text = new StringBuilder()
                .Append(htmlEncode ? HttpUtility.HtmlEncode(Environment.MachineName) : Environment.MachineName)
                .Append(" at ")
                .Append(DateTime.UtcNow)
                .AppendLine();

            var timings = new Stack<Timing>();
            timings.Push(profiler.Root);

            while (timings.Count > 0)
            {
                var timing = timings.Pop();
                var name = htmlEncode ? HttpUtility.HtmlEncode(timing.Name) : timing.Name;

                text.AppendFormat("{2} {0} = {1:###,##0.##}ms", name, timing.DurationMilliseconds, new string('>', timing.Depth));

                if (timing.HasCustomTimings)
                {
                    foreach (var pair in timing.CustomTimings)
                    {
                        var type = pair.Key;
                        var customTimings = pair.Value;

                        text.AppendFormat(" ({0} = {1:###,##0.##}ms in {2} cmd{3})", 
                            type, 
                            customTimings.Sum(ct => ct.DurationMilliseconds),
                            customTimings.Count,
                            customTimings.Count == 1 ? "" : "s");
                    }
                }

                text.AppendLine();
                
                if (timing.HasChildren)
                {
                    var children = timing.Children;
                    for (var i = children.Count - 1; i >= 0; i--) timings.Push(children[i]);
                }
            }

            return text.ToString();
        }
        
        /// <summary>
        /// Returns null if there is not client timing stuff
        /// </summary>
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