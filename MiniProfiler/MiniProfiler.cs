using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Profiling
{
    /// <summary>
    /// A single MiniProfiler can be used to represent any number of steps/levels in a call-graph, via Step()
    /// </summary>
    public class MiniProfiler
    {
        public class Timing : IDisposable
        {
            private string name;
            public string Name { get { return name; } }
            private readonly MiniProfiler parent;
            private readonly long startTicks;
            private long endTicks;
            internal Timing(string name, MiniProfiler parent, int depth)
            {
                this.parent = parent;
                startTicks = parent.EllapsedTicks;
                this.depth = depth;
                this.name = name;
            }
            public void AddData(string key, string value)
            {
                name += "; " + key + "=" + value;
            }

            private readonly int depth;
            public int Depth { get { return depth; } }
            public decimal Duration
            {
                get
                {
                    long z = (10000 * (endTicks - startTicks));
                    decimal msTimesTen = (int)(z / Stopwatch.Frequency);
                    return msTimesTen / 10;
                }
            }
            void IDisposable.Dispose()
            {
                End();
            }
            public bool HasChildren { get { return children != null && children.Count > 0; } }
            private List<Timing> children;
            public List<Timing> Children { get { return (children ?? (children = new List<Timing>())); } }
            internal void AddChild(Timing timing)
            {
                Children.Add(timing);
            }
            public void End()
            {
                if (endTicks == 0L) endTicks = parent.EllapsedTicks;
                parent.EndTiming(this);
            }


        }

        private readonly Stopwatch watch;
        private readonly List<Timing> stack; // but not really a stack as we need more flexibility...
        private readonly Timing root;
        public Timing Root { get { return root; } }
        public string Path { get { return root.Name; } }
        public MiniProfiler(string path)
        {
            watch = Stopwatch.StartNew();
            root = new Timing(path, this, 0);
            stack = new List<Timing>();
            stack.Add(root);
        }
        private long EllapsedTicks { get { return watch.ElapsedTicks; } }
        private void EndTiming(Timing timing)
        {
            int index = stack.LastIndexOf(timing);
            if (index >= 0) stack.RemoveRange(index, stack.Count - index);
        }
        public void EndAllImpl()
        {
            Kill();
            if (stack.Count > 0)
            {
                var clone = stack.ToArray();
                stack.Clear();
                for (int i = 0; i < clone.Length; i++) clone[i].End();
            }
        }
        private Timing Head { get { return stack.Count == 0 ? root : stack[stack.Count - 1]; } }
        public IDisposable StepImpl(string name)
        {
            var newTiming = new Timing(name, this, Head.Depth + 1);
            Head.AddChild(newTiming);
            stack.Add(newTiming);
            return newTiming;
        }

        internal void AddDataImpl(string key, string value)
        {
            Head.AddData(key, value);
        }

        internal void Kill()
        {
            watch.Stop();
        }
    }
    public static class MiniProfilerExtensions
    {
        public static IDisposable Step(this MiniProfiler profiler, string name)
        {
            return profiler == null ? null : profiler.StepImpl(name);
        }

        public static void AddData(this MiniProfiler profiler, string key, string value)
        {
            if (profiler != null) profiler.AddDataImpl(key, value);
        }
        public static IHtmlString Render(this MiniProfiler profiler)
        {
            if (profiler == null) return MvcHtmlString.Empty;
            profiler.EndAllImpl();
            var text = new StringBuilder()
                .Append(HttpUtility.HtmlEncode(Environment.MachineName)).Append(" at ").Append(DateTime.UtcNow).AppendLine()
                .Append("Path: ").AppendLine(HttpUtility.HtmlEncode(profiler.Path));
            Stack<MiniProfiler.Timing> timings = new Stack<MiniProfiler.Timing>();
            timings.Push(profiler.Root);
            while (timings.Count > 0)
            {
                var timing = timings.Pop();
                string name = HttpUtility.HtmlEncode(timing.Name);
                text.AppendFormat("{2} {0} = {1:###,##0.##}ms", name, timing.Duration, new string('>', timing.Depth)).AppendLine();
                if (timing.HasChildren)
                {
                    IList<MiniProfiler.Timing> children = timing.Children;
                    for (int i = children.Count - 1; i >= 0; i--) timings.Push(children[i]);
                }
            }
            return MvcHtmlString.Create(text.ToString());
        }

        private static readonly string ContextKey = ":mini-prof";
        public static MiniProfiler GetProfiler(this HttpContextBase context)
        {
            return (MiniProfiler)context.Items[ContextKey];
        }
        public static void SetProfiler(this HttpContextBase context, MiniProfiler profiler)
        {
            context.Items[ContextKey] = profiler;
        }

    }
}