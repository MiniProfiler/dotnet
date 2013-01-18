namespace StackExchange.Profiling
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Web;

    /// <summary>
    /// Contains helper methods that ease working with null <see cref="MiniProfiler"/>s.
    /// </summary>
    public static class MiniProfilerExtensions
    {
        /// <summary>
        /// Wraps <paramref name="selector"/> in a <see cref="Step"/> call and executes it, returning its result.
        /// </summary>
        /// <typeparam name="T">the type of result.</typeparam>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="selector">Method to execute and profile.</param>
        /// <param name="name">The <see cref="Timing"/> step name used to label the profiler results.</param>
        /// <returns>the profiled result.</returns>
        public static T Inline<T>(this MiniProfiler profiler, Func<T> selector, string name)
        {
            if (selector == null) throw new ArgumentNullException("selector");
            if (profiler == null) return selector();
            using (profiler.StepImpl(name))
            {
                return selector();
            }
        }

        /// <summary>
        /// Returns an <see cref="IDisposable"/> that will time the code between its creation and disposal.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="name">A descriptive name for the code that is encapsulated by the resulting IDisposable's lifetime.</param>
        /// <param name="level">This step's visibility level; allows filtering when <see cref="MiniProfiler.Start"/> is called.</param>
        /// <returns>the profile step</returns>
        public static IDisposable Step(this MiniProfiler profiler, string name, ProfileLevel level = ProfileLevel.Info)
        {
            return profiler == null ? null : profiler.StepImpl(name, level);
        }

        /// <summary>
        /// Adds <paramref name="externalProfiler"/>'s <see cref="Timing"/> hierarchy to this profiler's current Timing step,
        /// allowing other threads, remote calls, etc. to be profiled and joined into this profiling session.
        /// </summary>
        /// <param name="profiler">The profiler.</param>
        /// <param name="externalProfiler">The external Profiler.</param>
        public static void AddProfilerResults(this MiniProfiler profiler, MiniProfiler externalProfiler)
        {
            if (profiler == null || profiler.Head == null || externalProfiler == null) return;
            profiler.Head.AddChild(externalProfiler.Root);
            profiler.HasSqlTimings |= externalProfiler.HasSqlTimings;
            profiler.HasDuplicateSqlTimings |= externalProfiler.HasDuplicateSqlTimings;
        }

        /// <summary>
        /// Returns an html-encoded string with a text-representation of <paramref name="profiler"/>; returns "" when profiler is null.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <returns>a string containing the rendered result.</returns>
        public static IHtmlString Render(this MiniProfiler profiler)
        {
            return new HtmlString(RenderImpl(profiler, true));
        }

        /// <summary>
        /// Returns a plain-text representation of <paramref name="profiler"/>, suitable for viewing from 
        /// <see cref="Console"/>, log, or unit test output.
        /// </summary>
        /// <param name="profiler">A profiling session with child <see cref="Timing"/> instances.</param>
        /// <returns>a string containing the plain text.</returns>
        public static string RenderPlainText(this MiniProfiler profiler)
        {
            return RenderImpl(profiler, false);
        }

        /// <summary>
        /// The render implementation.
        /// </summary>
        /// <param name="profiler">The profiler.</param>
        /// <param name="htmlEncode">The html encode.</param>
        /// <returns>a string containing the render implementation</returns>
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

                if (timing.HasSqlTimings)
                {
                    text.AppendFormat(" ({0:###,##0.##}ms in {1} sql quer{2})", timing.SqlTimingsDurationMilliseconds, timing.SqlTimings.Count, timing.SqlTimings.Count == 1 ? "y" : "ies");
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
    }
}