using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Linq;
using System.Runtime.Serialization;
using System.Web.Routing;

namespace Profiling
{
    /// <summary>
    /// A single MiniProfiler can be used to represent any number of steps/levels in a call-graph, via Step()
    /// </summary>
    /// <remarks>Totally baller.</remarks>
    [DataContract]
    public partial class MiniProfiler
    {

        /// <summary>
        /// Identifies this Profiler so it may be stored/cached.
        /// </summary>
        [DataMember(Order = 0)]
        public Guid Id { get; private set; }

        /// <summary>
        /// A display name for this profiling session.
        /// </summary>
        [DataMember(Order = 1)]
        public string Name { get; set; }

        /// <summary>
        /// When this profiler was instantiated.
        /// </summary>
        [DataMember(Order = 2)]
        public DateTime Started { get; private set; }

        /// <summary>
        /// Where this profiler was run.
        /// </summary>
        [DataMember(Order = 3)]
        public string MachineName { get; private set; }

        /// <summary>
        /// Allows filtering of <see cref="Timing"/> steps based on what <see cref="ProfileLevel"/> 
        /// the steps are created with.
        /// </summary>
        [DataMember(Order = 4)]
        public ProfileLevel Level { get; set; }


        private Timing _root;
        /// <summary>
        /// The first <see cref="Timing"/> that is created and started when this profiler is instantiated.
        /// All other <see cref="Timing"/>s will be children of this one.
        /// </summary>
        [DataMember(Order = 5)]
        public Timing Root
        {
            get { return _root; }
            set
            {
                _root = value;

                // when being deserialized, we need to go through and set all child timings' parents
                if (_root.HasChildren)
                {
                    var timings = new Stack<Timing>();

                    timings.Push(_root);

                    while (timings.Count > 0)
                    {
                        var timing = timings.Pop();

                        if (timing.HasChildren)
                        {
                            var children = timing.Children;

                            for (int i = children.Count - 1; i >= 0; i--)
                            {
                                children[i].Parent = timing;
                                timings.Push(children[i]); // FLORIDA!  TODO: refactor this and other stack creation methods into one 
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Contains information about queries executed during this profiling session.
        /// </summary>
        internal SqlProfiler SqlProfiler { get; private set; }

        /// <summary>
        /// Starts when this profiler is instantiated. Each <see cref="Timing"/> step will use this Stopwatch's current ticks as
        /// their starting time.
        /// </summary>
        private readonly Stopwatch _watch;

        /// <summary>
        /// Milliseconds, to one decimal place, that this MiniProfiler ran.
        /// </summary>
        public double DurationMilliseconds
        {
            get { return _root.DurationMilliseconds ?? GetRoundedMilliseconds(ElapsedTicks); }
        }

        /// <summary>
        /// Milliseconds, to one decimal place, that this MiniProfiler was executing sql.
        /// </summary>
        public double DurationMillisecondsInSql
        {
            get { return GetTimingHierarchy().Sum(t => t.HasSqlTimings ? t.SqlTimings.Sum(s => s.DurationMilliseconds) : 0); }
        }

        /// <summary>
        /// Returns true when we have profiled queries.
        /// </summary>
        public bool HasSqlTimings
        {
            get
            {
                foreach (var t in GetTimingHierarchy())
                {
                    if (t.HasSqlTimings)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Ticks since this MiniProfiler was started.
        /// </summary>
        internal long ElapsedTicks { get { return _watch.ElapsedTicks; } }

        /// <summary>
        /// Points to the currently executing Timing.
        /// </summary>
        internal Timing Head { get; set; }


        public MiniProfiler(string path, ProfileLevel level = ProfileLevel.Info)
        {
            Started = DateTime.UtcNow;
            _watch = Stopwatch.StartNew();
            Root = new Timing(this, parent: null, name: path);
            Id = Guid.NewGuid();
            Level = level;
            SqlProfiler = new SqlProfiler(this);
            MachineName = Environment.MachineName;
        }

        [Obsolete("Used for serialization")]
        public MiniProfiler()
        {
        }


        internal IDisposable StepImpl(string name, ProfileLevel level = ProfileLevel.Info)
        {
            if (level > this.Level) return null;

            return new Timing(this, Head, name);
        }

        internal bool StopImpl()
        {
            if (!_watch.IsRunning)
                return false;

            _watch.Stop();
            foreach (var timing in GetTimingHierarchy()) timing.Stop();

            return true;
        }

        internal void AddDataImpl(string key, string value)
        {
            Head.AddKeyValue(key, value);
        }

        internal void AddSqlTiming(SqlTiming stats)
        {
            Head.AddSqlTiming(stats);
        }

        /// <summary>
        /// Walks the <see cref="Timing"/> hierarchy contained in this profiler, starting with <see cref="Root"/>, and returns each Timing found.
        /// </summary>
        public IEnumerable<Timing> GetTimingHierarchy()
        {
            var timings = new Stack<Timing>();

            timings.Push(_root);

            while (timings.Count > 0)
            {
                var timing = timings.Pop();

                yield return timing;

                if (timing.HasChildren)
                {
                    var children = timing.Children;
                    for (int i = children.Count - 1; i >= 0; i--) timings.Push(children[i]);
                }
            }
        }

        public List<SqlTiming> GetSqlTimings()
        {
            return GetTimingHierarchy().Where(t => t.HasSqlTimings).SelectMany(t => t.SqlTimings).ToList();
        }

        /// <summary>
        /// Returns milliseconds based on Stopwatch's Frequency.
        /// </summary>
        internal static double GetRoundedMilliseconds(long stopwatchElapsedTicks)
        {
            long z = 10000 * stopwatchElapsedTicks;
            double msTimesTen = (int)(z / Stopwatch.Frequency);
            return msTimesTen / 10;
        }

        /// <summary>
        /// Hooks up MiniProfiler's controller actions needed to display results.
        /// </summary>
        public static void RegisterRoutes(RouteCollection routes)
        {
            UI.MiniProfilerController.RegisterRoutes(routes);
        }

        /// <summary>
        /// Starts a new MiniProfiler for the current Request. This new profiler can be accessed by
        /// <see cref="MiniProfiler.Current"/>
        /// </summary>
        public static MiniProfiler Start(ProfileLevel level = ProfileLevel.Info)
        {
            var context = HttpContext.Current;
            if (context == null) return null;

            var url = context.Request.Url;
            var path = url.AbsolutePath.ToLower();

            // don't profile our profiler routes!
            if (UI.MiniProfilerController.IsProfilerPath(path)) return null;

            // don't profile /content or /scripts, either - happens in web.dev
            foreach (var ignored in Settings.IgnoredRootPaths ?? new string[0])
            {
                if (path.StartsWith(ignored, StringComparison.OrdinalIgnoreCase))
                    return null;
            }

            var result = new MiniProfiler(url.OriginalString, level);
            Current = result;

            return result;
        }

        /// <summary>
        /// Ends the current profiling session, if one exists.
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/> for this HttpContext, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public static void Stop(bool discardResults = false)
        {
            var context = HttpContext.Current;
            if (context == null)
                return;

            var current = Current;
            if (current == null)
                return;

            // stop our timings - when this is false, we've already called .Stop before on this session
            if (!current.StopImpl())
                return;

            if (discardResults)
            {
                Current = null;
                return;
            }

            var request = context.Request;
            var response = context.Response;

            // because we fetch profiler results after the page loads, we have to put them somewhere in the meantime
            Settings.EnsureCacheMethods();
            Settings.ShortTermCacheSetter(current);

            try
            {
                // allow profiling of ajax requests
                response.AppendHeader("X-MiniProfiler-Id", current.Id.ToString());
            }
            catch { } // headers blew up

            // also set the profiler name to Controller/Action or /url
            if (string.IsNullOrWhiteSpace(current.Name))
            {
                var mvc = context.Handler as MvcHandler;

                if (mvc != null)
                {
                    var values = mvc.RequestContext.RouteData.Values;
                    current.Name = values["Controller"].ToString() + "/" + values["Action"].ToString();
                }

                if (string.IsNullOrWhiteSpace(current.Name))
                {
                    current.Name = request.Url.AbsolutePath ?? "";
                    if (current.Name.Length > 50)
                        current.Name = current.Name.Remove(50);
                }
            }

            // by default, we should be calling .Stop in HttpApplication.EndRequest
            if (Settings.WriteScriptsToResponseOnStop)
            {
                if (string.IsNullOrWhiteSpace(response.ContentType) || !response.ContentType.ToLower().Contains("text/html"))
                    return;

                if (!string.IsNullOrWhiteSpace(context.Request.Headers["X-Requested-With"]))
                    return;

                response.Write(RenderIncludes());
            }
        }

        /// <summary>
        /// Returns the css and javascript includes needed to display the MiniProfiler results UI.
        /// </summary>
        /// <returns>Script and link elements normally; an empty string when there is no active profiling session.</returns>
        public static IHtmlString RenderIncludes()
        {
            if (Current == null) return MvcHtmlString.Empty;

            return MvcHtmlString.Create(string.Format(
@"<link rel=""stylesheet/less"" type=""text/css"" href=""/mini-profiler-includes.less"">
<script type=""text/javascript"" src=""/mini-profiler-includes.js""></script>
<script type=""text/javascript""> jQuery(function() {{ MiniProfiler.init({{ id:'{0}', renderLeft:{1} }}); }} ); </script>", Current.Id, MiniProfiler.Settings.RenderPopupButtonOnLeft ? "true" : "false"));
        }

        /// <summary>
        /// Gets the currently running MiniProfiler for the current HttpContext; null if no MiniProfiler was <see cref="Start"/>ed.
        /// </summary>
        public static MiniProfiler Current
        {
            get
            {
                var context = HttpContext.Current;
                if (context == null) return null;

                return context.Items[CacheKey] as MiniProfiler;
            }
            private set
            {
                var context = HttpContext.Current;
                if (context == null) return;

                context.Items[CacheKey] = value;
            }
        }

        private const string CacheKey = ":mini-profiler:";
    }

    public enum ProfileLevel
    {
        Info = 0,
        Verbose = 1
    }

    public static class MiniProfilerExtensions
    {

        public static void SetName(this MiniProfiler profiler, string name)
        {
            if (profiler == null) return;
            profiler.Name = name;
        }

        public static T Inline<T>(this MiniProfiler profiler, Func<T> selector, string name)
        {
            if (selector == null) throw new ArgumentNullException("selector");
            if (profiler == null) return selector();
            using (profiler.StepImpl(name))
            {
                return selector();
            }
        }

        public static IDisposable Step(this MiniProfiler profiler, string name, ProfileLevel level = ProfileLevel.Info)
        {
            return profiler == null ? null : profiler.StepImpl(name, level);
        }

        // TODO: get this working in the UI
        //public static void AddData(this MiniProfiler profiler, string key, string value)
        //{
        //    if (profiler != null) profiler.AddDataImpl(key, value);
        //}

        /// <summary>
        /// Adds <paramref name="externalProfiler"/>'s <see cref="Timing"/> hierarchy to this profiler's current Timing step,
        /// allowing other threads, remote calls, etc. to be profiled and joined into this profiling session.
        /// </summary>
        public static void AddProfilerResults(this MiniProfiler profiler, MiniProfiler externalProfiler)
        {
            if (profiler == null || externalProfiler == null) return;
            profiler.Head.AddChild(externalProfiler.Root);
        }

        public static IHtmlString Render(this MiniProfiler profiler)
        {
            if (profiler == null) return MvcHtmlString.Empty;

            var text = new StringBuilder()
                .Append(HttpUtility.HtmlEncode(Environment.MachineName)).Append(" at ").Append(DateTime.UtcNow).AppendLine();

            Stack<Timing> timings = new Stack<Timing>();
            timings.Push(profiler.Root);
            while (timings.Count > 0)
            {
                var timing = timings.Pop();
                string name = HttpUtility.HtmlEncode(timing.Name);
                text.AppendFormat("{2} {0} = {1:###,##0.##}ms", name, timing.DurationMilliseconds, new string('>', timing.Depth)).AppendLine();
                if (timing.HasChildren)
                {
                    IList<Timing> children = timing.Children;
                    for (int i = children.Count - 1; i >= 0; i--) timings.Push(children[i]);
                }
            }
            return MvcHtmlString.Create(text.ToString());
        }

    }
}