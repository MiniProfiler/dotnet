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
    public static class MiniProfilerExtensions
    {
        /// <summary>
        /// Wraps <paramref name="selector"/> in a <see cref="Step(MiniProfiler, string)"/> call and executes it, returning its result.
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
        /// Wraps <paramref name="selector"/> in a <see cref="Step(MiniProfiler, string)"/> call and executes it, returning its result.
        /// </summary>
        /// <typeparam name="T">the type of result.</typeparam>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="selector">Method to execute and profile.</param>
        /// <param name="name">The <see cref="Timing"/> step name used to label the profiler results.</param>
        /// <param name="level">This step's visibility level; allows filtering when <see cref="MiniProfiler.Start(string)"/> is called.</param>
        /// <returns>the profiled result.</returns>
        [Obsolete("Please use the Inline(Func<T> selector, string name) overload instead of this one. ProfileLevel is going away.")]
        public static T Inline<T>(this MiniProfiler profiler, Func<T> selector, string name, ProfileLevel level)
        {
            return profiler.Inline(selector, name);
        }
        /// <summary>
        /// Returns an <see cref="IDisposable"/> that will time the code between its creation and disposal.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="name">A descriptive name for the code that is encapsulated by the resulting IDisposable's lifetime.</param>
        /// <returns>the profile step</returns>
        public static IDisposable Step(this MiniProfiler profiler, string name)
        {
            return profiler == null ? null : profiler.StepImpl(name);
        }

        /// <summary>
        /// Returns an <see cref="IDisposable"/> that will time the code between its creation and disposal.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="name">A descriptive name for the code that is encapsulated by the resulting IDisposable's lifetime.</param>
        /// <param name="level">This step's visibility level; allows filtering when <see cref="MiniProfiler.Start(string)"/> is called.</param>
        /// <returns>the profile step</returns>
        [Obsolete("Please use the Step(string name) overload instead of this one. ProfileLevel is going away.")]
        public static IDisposable Step(this MiniProfiler profiler, string name, ProfileLevel level)
        {
            return profiler == null ? null : profiler.StepImpl(name, level);
        }

        /// <summary>
        /// Returns a new <see cref="CustomTiming"/> that will automatically set its <see cref="Profiling.CustomTiming.StartMilliseconds"/>
        /// and <see cref="Profiling.CustomTiming.DurationMilliseconds"/>
        /// </summary>
        /// <remarks>
        /// Should be used like the <see cref="Step(MiniProfiler, string)"/> extension method:
        /// 
        /// <example>
        /// 
        /// </example>
        /// </remarks>
        public static CustomTiming CustomTiming(this MiniProfiler profiler, string category, string commandString, string executeType = null)
        {
            if (profiler == null || profiler.Head == null) return null;

            var result = new CustomTiming(profiler, commandString)
            {
                ExecuteType = executeType
            };
            
            // THREADING: revisit
            profiler.Head.AddCustomTiming(category, result);

            return result;
        }

        /// <summary>
        /// Returns an <see cref="IDisposable"/> that will ignore profiling between its creation and disposal.
        /// </summary>
        /// <remarks>
        /// This is mainly useful in situations where you want to ignore database profiling for known hot spots,
        /// but it is safe to use in a nested step such that you can ignore sub-sections of a profiled step.
        /// </remarks>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <returns>the profile step</returns>
        public static IDisposable Ignore(this MiniProfiler profiler)
        {
            return profiler == null ? null : profiler.IgnoreImpl();
        }
        
        /// <summary>
        /// Adds <paramref name="externalProfiler"/>'s <see cref="Timing"/> hierarchy to this profiler's current Timing step,
        /// allowing other threads, remote calls, etc. to be profiled and joined into this profiling session.
        /// </summary>
        public static void AddProfilerResults(this MiniProfiler profiler, MiniProfiler externalProfiler)
        {
            if (profiler == null || profiler.Head == null || externalProfiler == null) return;
            profiler.Head.AddChild(externalProfiler.Root);
        }

        /// <summary>
        /// Adds the <paramref name="text"/> and <paramref name="url"/> pair to <paramref name="profiler"/>'s 
        /// <see cref="MiniProfiler.CustomLinks"/> dictionary; will be displayed on the client in the bottom of the profiler popup.
        /// </summary>
        public static void AddCustomLink(this MiniProfiler profiler, string text, string url)
        {
            if (profiler == null) return;

            lock (profiler)
            {
                if (profiler.CustomLinks == null)
                {
                    profiler.CustomLinks = new Dictionary<string, string>();
                }
            }

            profiler.CustomLinks[text] = url;
        }

        /// <summary>
        /// Returns an html-encoded string with a text-representation of <paramref name="profiler"/>; returns "" when profiler is null.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        public static IHtmlString Render(this MiniProfiler profiler)
        {
            return new HtmlString(RenderImpl(profiler, true));
        }

        /// <summary>
        /// Returns a plain-text representation of <paramref name="profiler"/>, suitable for viewing from 
        /// <see cref="Console"/>, log, or unit test output.
        /// </summary>
        /// <param name="profiler">A profiling session with child <see cref="Timing"/> instances.</param>
        public static string RenderPlainText(this MiniProfiler profiler)
        {
            return RenderImpl(profiler, false);
        }

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
    }
}